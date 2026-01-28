using System.Globalization;
using System.Text.RegularExpressions;
using Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Conversion;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;
using Logic.Domain.SpikeChunsoftManagement.Contract.DataClasses.Script;

namespace Logic.Business.SpikeChunsoftScriptManagement.Conversion;

internal class FsbCodeUnitConverter : IFsbCodeUnitConverter
{
    private readonly Regex _subPattern = new("^sub[0-9]+$", RegexOptions.Compiled);
    private int _labelCounter;
    private readonly Stack<LoopEmissionContext> _loopContextStack = new();

    public Sir0Function[] CreateScriptFile(CodeUnitSyntax tree)
    {
        Sir0Function[] functions = CreateFunctions(tree.MethodDeclarations);

        return functions;
    }

    private Sir0Function[] CreateFunctions(IReadOnlyList<MethodDeclarationSyntax> methods)
    {
        var functions = new List<Sir0Function>();

        foreach (MethodDeclarationSyntax method in methods)
            functions.Add(CreateFunction(method));

        return [.. functions];
    }

    private Sir0Function CreateFunction(MethodDeclarationSyntax method)
    {
        Sir0Operation[] operations = CreateOperations(method);

        return new Sir0Function(GetStringLiteral(method.Name), operations);
    }

    private Sir0Operation[] CreateOperations(MethodDeclarationSyntax method)
    {
        var operations = new List<Sir0Operation>();

        AddInitOperation(operations);
        CreateOperations(operations, method.Body);

        return [.. operations];
    }

    private void CreateOperations(List<Sir0Operation> operations, BlockExpression block, string? leadingLabel = null)
    {
        string? jumpLabel = CreateOperationsInternal(operations, block, leadingLabel);
        
        if (jumpLabel is not null)
            BackPropagateJumpLabel(operations, jumpLabel);
    }

    private string? CreateOperationsInternal(List<Sir0Operation> operations, BlockExpression block, string? leadingLabel)
    {
        string? jumpLabel = leadingLabel;
        foreach (StatementSyntax statement in block.Statements)
        {
            string? nextLabel = null;
            switch (statement)
            {
                case GotoLabelStatementSyntax gotoLabelStatement:
                    if (jumpLabel is not null)
                        throw CreateException("Only one jump label is allowed per statement.", gotoLabelStatement.Location);

                    nextLabel = GetStringLiteral(gotoLabelStatement.Label);
                    break;

                case MethodInvocationStatementSyntax methodInvocation:
                    AddOperations(operations, methodInvocation, jumpLabel);
                    break;

                case AsyncBlockStatement asyncStatement:
                    AddAsyncOperations(operations, asyncStatement, jumpLabel);
                    break;

                case IfStatementSyntax ifStatement:
                    nextLabel = AddIfOperations(operations, ifStatement, jumpLabel);
                    break;

                case IfElseStatementSyntax ifElseStatement:
                    nextLabel = AddIfElseOperations(operations, ifElseStatement, jumpLabel);
                    break;

                case DoWhileStatementSyntax doWhileStatement:
                    nextLabel = AddDoWhileOperations(operations, doWhileStatement, jumpLabel);
                    break;

                case BreakStatementSyntax:
                    nextLabel = AddBreakOperation(operations, jumpLabel);
                    break;

                case ContinueStatementSyntax:
                    nextLabel = AddContinueOperation(operations, jumpLabel);
                    break;

                case ReturnStatementSyntax:
                    AddReturnOperation(operations, jumpLabel);
                    break;

                default:
                    throw CreateException($"Unknown statement {statement.GetType().Name}.", statement.Location);
            }

            jumpLabel = nextLabel;
        }

        return jumpLabel;
    }

    private static void AddInitOperation(List<Sir0Operation> operations)
    {
        operations.Add(new Sir0Operation(null, 0x25, []));
    }

    private static void AddReturnOperation(List<Sir0Operation> operations, string? jumpLabel)
    {
        operations.Add(new Sir0Operation(jumpLabel, 0x26, []));
    }

    private void AddOperations(List<Sir0Operation> operations, MethodInvocationStatementSyntax methodInvocation, string? jumpLabel)
    {
        byte operation = GetOperation(methodInvocation.Name);

        object[] arguments = [];
        if (methodInvocation.Parameters.ParameterList != null)
        {
            var literals = methodInvocation.Parameters.ParameterList.Elements;
            arguments = new object[literals.Count];

            for (var i = 0; i < literals.Count; i++)
                arguments[i] = GetArgument(literals[i]);
        }

        operations.Add(new Sir0Operation(jumpLabel, operation, arguments));
    }

    private void AddAsyncOperations(List<Sir0Operation> operations, AsyncBlockStatement asyncStatement, string? jumpLabel)
    {
        AddAsyncStartOperation(operations, jumpLabel);
        CreateOperations(operations, asyncStatement.Body);
        AddAsyncEndOperation(operations, null);
    }

    private string AddIfOperations(List<Sir0Operation> operations, IfStatementSyntax ifStatement, string? jumpLabel)
    {
        string endLabel = CreateLabel();

        int conditionIndex = operations.Count;
        AddConditionalJumpOnFalse(operations, jumpLabel, endLabel);

        string? danglingLabel = CreateOperationsInternal(operations, ifStatement.Body, null);
        if (danglingLabel is not null)
        {
            UpdateJumpTarget(operations, conditionIndex, danglingLabel);
            endLabel = danglingLabel;
        }

        return endLabel;
    }

    private string AddIfElseOperations(List<Sir0Operation> operations, IfElseStatementSyntax ifElseStatement, string? jumpLabel)
    {
        string elseLabel = CreateLabel();
        string endLabel = CreateLabel();

        AddConditionalJumpOnFalse(operations, jumpLabel, elseLabel);

        string? danglingThenLabel = CreateOperationsInternal(operations, ifElseStatement.Body, null);
        int gotoIndex = operations.Count;
        AddGotoOperation(operations, danglingThenLabel, endLabel);

        string? danglingElseLabel = CreateOperationsInternal(operations, ifElseStatement.ElseBody, elseLabel);
        if (danglingElseLabel is not null)
        {
            UpdateJumpTarget(operations, gotoIndex, danglingElseLabel);
            endLabel = danglingElseLabel;
        }

        return endLabel;
    }

    private string? AddDoWhileOperations(List<Sir0Operation> operations, DoWhileStatementSyntax doWhileStatement, string? jumpLabel)
    {
        string startLabel = jumpLabel ?? CreateLabel();
        string breakLabel = CreateLabel();
        var loopContext = new LoopEmissionContext(startLabel, breakLabel);
        _loopContextStack.Push(loopContext);

        string? danglingLabel = CreateOperationsInternal(operations, doWhileStatement.Body, startLabel);
        AddConditionalJumpOnTrue(operations, danglingLabel, startLabel);

        _loopContextStack.Pop();
        return loopContext.BreakUsed ? breakLabel : null;
    }

    private string? AddBreakOperation(List<Sir0Operation> operations, string? jumpLabel)
    {
        if (_loopContextStack.Count == 0)
            throw new InvalidOperationException("Break statement is only valid within a loop.");

        LoopEmissionContext loopContext = _loopContextStack.Peek();
        AddGotoOperation(operations, jumpLabel, loopContext.BreakLabel);
        loopContext.BreakUsed = true;

        return null;
    }

    private string? AddContinueOperation(List<Sir0Operation> operations, string? jumpLabel)
    {
        if (_loopContextStack.Count == 0)
            throw new InvalidOperationException("Continue statement is only valid within a loop.");

        LoopEmissionContext loopContext = _loopContextStack.Peek();
        AddGotoOperation(operations, jumpLabel, loopContext.StartLabel);

        return null;
    }

    private static void AddAsyncStartOperation(List<Sir0Operation> operations, string? jumpLabel)
    {
        operations.Add(new Sir0Operation(jumpLabel, 0x2B, [2]));
    }

    private static void AddAsyncEndOperation(List<Sir0Operation> operations, string? jumpLabel)
    {
        operations.Add(new Sir0Operation(jumpLabel, 0x2C, [2]));
    }

    private static void AddGotoOperation(List<Sir0Operation> operations, string? jumpLabel, string targetLabel)
    {
        operations.Add(new Sir0Operation(jumpLabel, 0x35, [targetLabel]));
    }

    private static void AddConditionalJumpOnTrue(List<Sir0Operation> operations, string? jumpLabel, string targetLabel)
    {
        operations.Add(new Sir0Operation(jumpLabel, 0x36, [targetLabel]));
    }

    private static void AddConditionalJumpOnFalse(List<Sir0Operation> operations, string? jumpLabel, string targetLabel)
    {
        operations.Add(new Sir0Operation(jumpLabel, 0x37, [targetLabel]));
    }

    private static void BackPropagateJumpLabel(List<Sir0Operation> operations, string jumpLabel)
    {
        for (int i = operations.Count - 1; i >= 0; i--)
        {
            Sir0Operation operation = operations[i];
            if (operation.Label is not null)
                continue;

            operations[i] = operation with { Label = jumpLabel };
            return;
        }

        throw new InvalidOperationException($"Could not back propagate jump label {jumpLabel}.");
    }

    private static void UpdateJumpTarget(List<Sir0Operation> operations, int operationIndex, string targetLabel)
    {
        Sir0Operation operation = operations[operationIndex];
        operations[operationIndex] = operation with { Arguments = [targetLabel] };
    }

    private object GetArgument(LiteralExpressionSyntax literal)
    {
        switch (literal.Literal.RawKind)
        {
            case (int)SyntaxTokenKind.NumericLiteral:
                return GetNumericLiteral(literal);

            case (int)SyntaxTokenKind.FloatingNumericLiteral:
                return GetFloatingNumericLiteral(literal);

            case (int)SyntaxTokenKind.StringLiteral:
                return GetStringLiteral(literal);

            default:
                throw CreateException($"Invalid operation argument {(SyntaxTokenKind)literal.Literal.RawKind}.", literal.Location);
        }
    }

    private byte GetOperation(NameSyntax name)
    {
        string composedName = GetName(name);

        if (_subPattern.IsMatch(composedName))
            return (byte)GetNumberFromStringEnd(composedName);

        throw CreateException($"Could not determine operation from {composedName}.", name.Location);
    }

    private string GetName(NameSyntax name)
    {
        switch (name)
        {
            case SimpleNameSyntax simpleName:
                return simpleName.Identifier.Text;

            case QualifiedNameSyntax qualifiedName:
                return GetName(qualifiedName.Left) + "." + GetName(qualifiedName.Right);

            default:
                throw CreateException("Invalid name syntax.", name.Location);
        }
    }

    private int GetNumericLiteral(LiteralExpressionSyntax literal)
    {
        if (literal.Literal.RawKind != (int)SyntaxTokenKind.NumericLiteral)
            throw CreateException($"Invalid literal {(SyntaxTokenKind)literal.Literal.RawKind}.", literal.Location, SyntaxTokenKind.NumericLiteral);

        return literal.Literal.Text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ?
            int.Parse(literal.Literal.Text[2..], NumberStyles.HexNumber) :
            int.Parse(literal.Literal.Text);
    }

    private float GetFloatingNumericLiteral(LiteralExpressionSyntax literal)
    {
        if (literal.Literal.RawKind != (int)SyntaxTokenKind.FloatingNumericLiteral)
            throw CreateException($"Invalid literal {(SyntaxTokenKind)literal.Literal.RawKind}.", literal.Location, SyntaxTokenKind.FloatingNumericLiteral);

        return float.Parse(literal.Literal.Text[..^1], CultureInfo.GetCultureInfo("en-gb"));
    }

    private string GetStringLiteral(LiteralExpressionSyntax literal)
    {
        if (literal.Literal.RawKind != (int)SyntaxTokenKind.StringLiteral)
            throw CreateException($"Invalid literal {(SyntaxTokenKind)literal.Literal.RawKind}.", literal.Location, SyntaxTokenKind.StringLiteral);

        return literal.Literal.Text[1..^1].Replace("\\\"", "\"");
    }

    private int GetNumberFromStringEnd(string text)
    {
        int startIndex = text.Length;
        while (text[startIndex - 1] >= '0' && text[startIndex - 1] <= '9')
            startIndex--;

        return int.Parse(text[startIndex..]);
    }

    private string CreateLabel()
    {
        return $"@{_labelCounter++:000}@";
    }

    private sealed class LoopEmissionContext
    {
        public string StartLabel { get; }
        public string BreakLabel { get; }
        public bool BreakUsed { get; set; }

        public LoopEmissionContext(string startLabel, string breakLabel)
        {
            StartLabel = startLabel;
            BreakLabel = breakLabel;
        }
    }

    private Exception CreateException(string message, SyntaxLocation location, params SyntaxTokenKind[] expected)
    {
        message = $"{message} (Line {location.Line}, Column {location.Column})";

        if (expected.Length > 0)
        {
            message = expected.Length == 1 ?
                $"{message} (Expected {expected[0]})" :
                $"{message} (Expected any of {string.Join(", ", expected)})";
        }

        return new InvalidOperationException(message);
    }
}