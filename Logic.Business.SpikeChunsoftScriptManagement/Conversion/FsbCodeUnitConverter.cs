using Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Conversion;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;
using Logic.Domain.CodeAnalysisManagement.Contract.SpikeChunsoft;
using Logic.Domain.SpikeChunsoftManagement.Contract.DataClasses.Script;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Logic.Business.SpikeChunsoftScriptManagement.Conversion;

internal class FsbCodeUnitConverter(ISpikeChunsoftSyntaxFactory syntaxFactory) : IFsbCodeUnitConverter
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
        if (method.Body.Statements.Count <= 0 || method.Body.Statements[^1] is not ReturnStatementSyntax)
            method.Body.SetStatements(method.Body.Statements.Concat([CreateReturnStatement()]).ToList());

        var operations = new List<Sir0Operation>();

        AddOperation(operations, 0x25);
        CreateOperations(operations, method.Body);

        return [.. operations];
    }

    private ReturnStatementSyntax CreateReturnStatement()
    {
        SyntaxToken returnToken = syntaxFactory.Token(SyntaxTokenKind.ReturnKeyword);
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new ReturnStatementSyntax(returnToken, null, semicolon);
    }

    private void CreateOperations(List<Sir0Operation> operations, BlockExpression block, string? leadingLabel = null, bool isNested = false)
    {
        string? jumpLabel = CreateOperationsInternal(operations, block, leadingLabel, isNested);

        if (jumpLabel is not null)
            BackPropagateJumpLabel(operations, jumpLabel);
    }

    private string? CreateOperationsInternal(List<Sir0Operation> operations, BlockExpression block, string? leadingLabel, bool isNested)
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

                case NativeMethodInvocationStatementSyntax methodInvocation:
                    AddOperations(operations, methodInvocation, jumpLabel);
                    break;

                case AssignmentStatementSyntax assignment:
                    AddOperations(operations, assignment, jumpLabel);
                    break;

                case AsyncBlockStatement asyncStatement:
                    AddAsyncOperations(operations, asyncStatement, jumpLabel);
                    break;

                case IfStatementSyntax ifStatement:
                    nextLabel = AddIfOperations(operations, ifStatement, jumpLabel, isNested);
                    break;

                case IfElseStatementSyntax ifElseStatement:
                    nextLabel = AddIfElseOperations(operations, ifElseStatement, jumpLabel);
                    break;

                case IfNotStatementSyntax ifNotStatement:
                    nextLabel = AddIfNotOperations(operations, ifNotStatement, jumpLabel, isNested);
                    break;

                case IfNotElseStatementSyntax ifNotElseStatement:
                    nextLabel = AddIfNotElseOperations(operations, ifNotElseStatement, jumpLabel);
                    break;

                case DoWhileStatementSyntax doWhileStatement:
                    nextLabel = AddDoWhileOperations(operations, doWhileStatement, jumpLabel);
                    break;

                case DoWhileNotStatementSyntax doWhileNotStatement:
                    nextLabel = AddDoWhileNotOperations(operations, doWhileNotStatement, jumpLabel);
                    break;

                case BreakStatementSyntax:
                    nextLabel = AddBreakOperation(operations, jumpLabel);
                    break;

                case ContinueStatementSyntax:
                    nextLabel = AddContinueOperation(operations, jumpLabel);
                    break;

                case ReturnStatementSyntax:
                    AddOperation(operations, 0x26, jumpLabel);
                    break;

                default:
                    throw CreateException($"Unknown statement {statement.GetType().Name}.", statement.Location);
            }

            jumpLabel = nextLabel;
        }

        return jumpLabel;
    }

    private void AddOperations(List<Sir0Operation> operations, MethodInvocationStatementSyntax methodInvocation, string? jumpLabel)
    {
        byte operation = GetOperation(methodInvocation.Method.Name);

        object[] arguments = [];
        if (methodInvocation.Method.Parameters.ParameterList != null)
        {
            var literals = methodInvocation.Method.Parameters.ParameterList.Elements;
            arguments = new object[literals.Count];

            for (var i = 0; i < literals.Count; i++)
                arguments[i] = GetArgument(literals[i]);
        }

        operations.Add(new Sir0Operation(jumpLabel, operation, arguments));
    }

    private void AddOperations(List<Sir0Operation> operations, NativeMethodInvocationStatementSyntax methodInvocation, string? jumpLabel)
    {
        AddNativeMethodInvocation(operations, methodInvocation.Method, jumpLabel);
        AddOperation(operations, 0x27);
    }

    private void AddOperations(List<Sir0Operation> operations, AssignmentStatementSyntax assignment, string? jumpLabel)
    {
        AddAssignmentExpression(operations, assignment.Assignment, jumpLabel);
        AddOperation(operations, 0x27);
    }

    private void AddAsyncOperations(List<Sir0Operation> operations, AsyncBlockStatement asyncStatement, string? jumpLabel)
    {
        AddAsyncStartOperation(operations, jumpLabel);
        CreateOperations(operations, asyncStatement.Body);
        AddAsyncEndOperation(operations, null);
    }

    private void AddExpressionOperations(List<Sir0Operation> operations, ExpressionSyntax expression, string? jumpLabel)
    {
        switch (expression)
        {
            case BinaryExpressionSyntax binary:
                AddBinaryExpression(operations, binary, jumpLabel);
                break;

            case LogicalExpressionSyntax logical:
                AddLogicalExpression(operations, logical, jumpLabel);
                break;

            case UnaryExpressionSyntax unary:
                AddUnaryExpression(operations, unary, jumpLabel);
                break;

            case PostfixExpressionSyntax postfix:
                AddPostfixExpression(operations, postfix, jumpLabel);
                break;

            case ArrayIndexExpressionSyntax arrayIndex:
                AddArrayIndexExpression(operations, arrayIndex, jumpLabel);
                break;

            case LiteralExpressionSyntax literal:
                AddLiteralExpression(operations, literal, jumpLabel);
                break;

            case AssignmentExpressionSyntax assignment:
                AddAssignmentExpression(operations, assignment, jumpLabel);
                break;

            case NativeMethodInvocationExpressionSyntax invocation:
                AddNativeMethodInvocation(operations, invocation, jumpLabel);
                break;
        }
    }

    private void AddBinaryExpression(List<Sir0Operation> operations, BinaryExpressionSyntax expression, string? jumpLabel)
    {
        AddExpressionOperations(operations, expression.Left, jumpLabel);
        AddExpressionOperations(operations, expression.Right, null);

        switch (expression.Operation.RawKind)
        {
            case (int)SyntaxTokenKind.Ampersand:
                AddOperation(operations, 0x0E);
                break;

            case (int)SyntaxTokenKind.Caret:
                AddOperation(operations, 0x10);
                break;

            case (int)SyntaxTokenKind.Pipe:
                AddOperation(operations, 0x11);
                break;

            case (int)SyntaxTokenKind.ShiftRight:
                AddOperation(operations, 0x13);
                break;

            case (int)SyntaxTokenKind.ShiftLeft:
                AddOperation(operations, 0x14);
                break;

            case (int)SyntaxTokenKind.Plus:
                AddOperation(operations, 0x15);
                break;

            case (int)SyntaxTokenKind.Minus:
                AddOperation(operations, 0x16);
                break;

            case (int)SyntaxTokenKind.Asterisk:
                AddOperation(operations, 0x17);
                break;

            case (int)SyntaxTokenKind.Slash:
                AddOperation(operations, 0x18);
                break;

            case (int)SyntaxTokenKind.Percent:
                AddOperation(operations, 0x19);
                break;

            case (int)SyntaxTokenKind.EqualsEquals:
                AddOperation(operations, 0x1A);
                break;

            case (int)SyntaxTokenKind.NotEquals:
                AddOperation(operations, 0x1B);
                break;

            case (int)SyntaxTokenKind.SmallerEquals:
                AddOperation(operations, 0x1C);
                break;

            case (int)SyntaxTokenKind.GreaterEquals:
                AddOperation(operations, 0x1D);
                break;

            case (int)SyntaxTokenKind.SmallerThan:
                AddOperation(operations, 0x1E);
                break;

            case (int)SyntaxTokenKind.GreaterThan:
                AddOperation(operations, 0x1F);
                break;

            default:
                throw new InvalidOperationException($"Unknown binary expression {expression.Operation.RawKind}.");
        }
    }

    private void AddLogicalExpression(List<Sir0Operation> operations, LogicalExpressionSyntax expression, string? jumpLabel)
    {
        AddExpressionOperations(operations, expression.Left, jumpLabel);
        AddExpressionOperations(operations, expression.Right, null);

        switch (expression.Operation.RawKind)
        {
            case (int)SyntaxTokenKind.AndKeyword:
                AddOperation(operations, 0x0F);
                break;

            case (int)SyntaxTokenKind.OrKeyword:
                AddOperation(operations, 0x12);
                break;

            default:
                throw new InvalidOperationException($"Unknown logical expression {expression.Operation.RawKind}.");
        }
    }

    private void AddUnaryExpression(List<Sir0Operation> operations, UnaryExpressionSyntax expression, string? jumpLabel)
    {
        AddExpressionOperations(operations, expression.Expression, jumpLabel);

        switch (expression.Operation.RawKind)
        {
            case (int)SyntaxTokenKind.ExclamationPoint:
                AddOperation(operations, 0x07);
                break;

            case (int)SyntaxTokenKind.PlusPlus:
                AddOperation(operations, 0x08);
                break;

            case (int)SyntaxTokenKind.MinusMinus:
                AddOperation(operations, 0x09);
                break;

            default:
                throw new InvalidOperationException($"Unknown unary expression {expression.Operation.RawKind}.");
        }
    }

    private void AddPostfixExpression(List<Sir0Operation> operations, PostfixExpressionSyntax expression, string? jumpLabel)
    {
        AddExpressionOperations(operations, expression.Expression, jumpLabel);

        switch (expression.Operation.RawKind)
        {
            case (int)SyntaxTokenKind.PlusPlus:
                AddOperation(operations, 0x0A);
                break;

            case (int)SyntaxTokenKind.MinusMinus:
                AddOperation(operations, 0x0B);
                break;

            default:
                throw new InvalidOperationException($"Unknown postfix expression {expression.Operation.RawKind}.");
        }
    }

    private void AddArrayIndexExpression(List<Sir0Operation> operations, ArrayIndexExpressionSyntax expression, string? jumpLabel)
    {
        AddExpressionOperations(operations, expression.Value, jumpLabel);

        foreach (var indexer in expression.Indexer)
            AddArrayIndexerExpression(operations, indexer, null);
    }

    private void AddArrayIndexerExpression(List<Sir0Operation> operations, ArrayIndexerExpressionSyntax expression, string? jumpLabel)
    {
        AddExpressionOperations(operations, expression.Index, jumpLabel);
        AddOperation(operations, 0x0C);
    }

    private void AddLiteralExpression(List<Sir0Operation> operations, LiteralExpressionSyntax expression, string? jumpLabel)
    {
        switch (expression.Literal.RawKind)
        {
            case (int)SyntaxTokenKind.NumericLiteral:
                AddNumericLiteralOperation(operations, GetNumericLiteral(expression), jumpLabel);
                break;

            case (int)SyntaxTokenKind.FloatingNumericLiteral:
                AddNumericLiteralOperation(operations, GetFloatingNumericLiteral(expression), jumpLabel);
                break;

            case (int)SyntaxTokenKind.StringLiteral:
                AddStringLiteralOperation(operations, GetStringLiteral(expression), jumpLabel);
                break;
        }
    }

    private void AddAssignmentExpression(List<Sir0Operation> operations, AssignmentExpressionSyntax expression, string? jumpLabel)
    {
        AddExpressionOperations(operations, expression.Left, jumpLabel);
        AddExpressionOperations(operations, expression.Right, null);

        switch (expression.Operation.RawKind)
        {
            case (int)SyntaxTokenKind.Equals:
                AddOperation(operations, 0x20);
                break;

            case (int)SyntaxTokenKind.PlusEquals:
                AddOperation(operations, 0x21);
                break;

            case (int)SyntaxTokenKind.MinusEquals:
                AddOperation(operations, 0x22);
                break;

            default:
                throw new InvalidOperationException($"Unknown assignment expression {expression.Operation.RawKind}.");
        }
    }

    private void AddNativeMethodInvocation(List<Sir0Operation> operations, NativeMethodInvocationExpressionSyntax expression, string? jumpLabel)
    {
        AddLiteralExpression(operations, expression.Name, jumpLabel);
        AddOperation(operations, 0x23);

        if (expression.Parameters.ParameterList is not null)
            foreach (var parameter in expression.Parameters.ParameterList.Elements)
                AddExpressionOperations(operations, parameter, null);

        AddOperation(operations, 0x24);
    }

    private string AddIfOperations(List<Sir0Operation> operations, IfStatementSyntax ifStatement, string? jumpLabel, bool isNested)
    {
        string endLabel = CreateLabel();

        AddExpressionOperations(operations, ifStatement.Condition, jumpLabel);

        int conditionIndex = operations.Count;
        AddConditionalJumpOnFalse(operations, null, endLabel);

        string? danglingLabel = CreateOperationsInternal(operations, ifStatement.Body, null, true);

        if (isNested)
        {
            AddGotoOperation(operations, danglingLabel, endLabel);
        }
        else if (danglingLabel is not null)
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

        AddExpressionOperations(operations, ifElseStatement.Condition, jumpLabel);
        AddConditionalJumpOnFalse(operations, null, elseLabel);

        string? danglingThenLabel = CreateOperationsInternal(operations, ifElseStatement.Body, null, true);
        
        int gotoIndex = operations.Count;
        AddGotoOperation(operations, danglingThenLabel, endLabel);

        string? danglingElseLabel = CreateOperationsInternal(operations, ifElseStatement.ElseBody, elseLabel, true);
        if (danglingElseLabel is not null)
        {
            UpdateJumpTarget(operations, gotoIndex, danglingElseLabel);
            endLabel = danglingElseLabel;
        }

        return endLabel;
    }

    private string AddIfNotOperations(List<Sir0Operation> operations, IfNotStatementSyntax ifNotStatement, string? jumpLabel, bool isNested)
    {
        string endLabel = CreateLabel();

        AddExpressionOperations(operations, ifNotStatement.Condition, jumpLabel);

        int conditionIndex = operations.Count;
        AddConditionalJumpOnTrue(operations, null, endLabel);

        string? danglingLabel = CreateOperationsInternal(operations, ifNotStatement.Body, null, true);

        if (isNested)
        {
            AddGotoOperation(operations, danglingLabel, endLabel);
        }
        else if (danglingLabel is not null)
        {
            UpdateJumpTarget(operations, conditionIndex, danglingLabel);
            endLabel = danglingLabel;
        }

        return endLabel;
    }

    private string AddIfNotElseOperations(List<Sir0Operation> operations, IfNotElseStatementSyntax ifNotElseStatement, string? jumpLabel)
    {
        string elseLabel = CreateLabel();
        string endLabel = CreateLabel();

        AddExpressionOperations(operations, ifNotElseStatement.Condition, jumpLabel);
        AddConditionalJumpOnTrue(operations, null, elseLabel);

        string? danglingThenLabel = CreateOperationsInternal(operations, ifNotElseStatement.Body, null, true);
        
        int gotoIndex = operations.Count;
        AddGotoOperation(operations, danglingThenLabel, endLabel);

        string? danglingElseLabel = CreateOperationsInternal(operations, ifNotElseStatement.ElseBody, elseLabel, true);
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

        string? danglingLabel = CreateOperationsInternal(operations, doWhileStatement.Body, startLabel, true);

        // HINT: "while (false)" is emitted as no additional operations
        if (doWhileStatement.Condition is LiteralExpressionSyntax { Literal.RawKind: (int)SyntaxTokenKind.TrueKeyword })
            AddGotoOperation(operations, danglingLabel, startLabel);
        else if (doWhileStatement.Condition is not LiteralExpressionSyntax { Literal.RawKind: (int)SyntaxTokenKind.FalseKeyword })
            AddConditionalJumpOnTrue(operations, danglingLabel, startLabel);

        _loopContextStack.Pop();

        return loopContext.BreakUsed ? breakLabel : null;
    }

    private string? AddDoWhileNotOperations(List<Sir0Operation> operations, DoWhileNotStatementSyntax doWhileNotStatement, string? jumpLabel)
    {
        string startLabel = jumpLabel ?? CreateLabel();
        string breakLabel = CreateLabel();
        var loopContext = new LoopEmissionContext(startLabel, breakLabel);
        _loopContextStack.Push(loopContext);

        string? danglingLabel = CreateOperationsInternal(operations, doWhileNotStatement.Body, startLabel, true);

        // HINT: "while not (true)" is emitted as no additional operations
        if (doWhileNotStatement.Condition is LiteralExpressionSyntax { Literal.RawKind: (int)SyntaxTokenKind.FalseKeyword })
            AddGotoOperation(operations, danglingLabel, startLabel);
        else if (doWhileNotStatement.Condition is not LiteralExpressionSyntax { Literal.RawKind: (int)SyntaxTokenKind.TrueKeyword })
            AddConditionalJumpOnFalse(operations, danglingLabel, startLabel);

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

    private static void AddNumericLiteralOperation(List<Sir0Operation> operations, float number, string? jumpLabel)
    {
        operations.Add(new Sir0Operation(jumpLabel, 0xF0, [number]));
    }

    private static void AddStringLiteralOperation(List<Sir0Operation> operations, string text, string? jumpLabel)
    {
        if (text[0] is '?' or '@' or ':' or '~' or '^' or '$' or '&')
        {
            var splitIndex = text.IndexOf("::", StringComparison.OrdinalIgnoreCase);
            if (splitIndex >= 0)
            {
                operations.Add(new Sir0Operation(jumpLabel, 0xF4, [text[..splitIndex], text[(splitIndex + 2)..]]));
                return;
            }

            operations.Add(new Sir0Operation(jumpLabel, 0xF4, [text]));
            return;
        }

        operations.Add(new Sir0Operation(jumpLabel, 0xF1, [text]));
    }

    private static void AddOperation(List<Sir0Operation> operations, byte operation, string? jumpLabel = null)
    {
        operations.Add(new Sir0Operation(jumpLabel, operation, []));
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