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

        string? jumpLabel = null;
        foreach (StatementSyntax statement in method.Body.Statements)
        {
            switch (statement)
            {
                case GotoLabelStatementSyntax gotoLabelStatement:
                    if (jumpLabel is not null)
                        throw CreateException("Only one jump label is allowed per statement.", gotoLabelStatement.Location);

                    jumpLabel = GetStringLiteral(gotoLabelStatement.Label);
                    continue;

                case MethodInvocationStatementSyntax methodInvocation:
                    AddOperations(operations, methodInvocation, jumpLabel);
                    break;

                case ReturnStatementSyntax:
                    AddReturnOperation(operations, jumpLabel);
                    break;

                default:
                    throw CreateException($"Unknown statement {statement.GetType().Name}.", statement.Location);
            }

            jumpLabel = null;
        }

        return [.. operations];
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