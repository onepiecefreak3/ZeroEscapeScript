using Logic.Business.SpikeChunsoftScriptManagement.DataClasses.Conversion;
using Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Conversion;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;
using Logic.Domain.CodeAnalysisManagement.Contract.SpikeChunsoft;
using Logic.Domain.SpikeChunsoftManagement.Contract.DataClasses.Script;

namespace Logic.Business.SpikeChunsoftScriptManagement.Conversion;

internal class FsbScriptFileConverter(ISpikeChunsoftSyntaxFactory syntaxFactory, IBlockBuilder blockBuilder) : IFsbScriptFileConverter
{
    public CodeUnitSyntax CreateCodeUnit(Sir0Function[] functions)
    {
        IReadOnlyList<MethodDeclarationSyntax> methods = CreateMethodDeclarations(functions);

        return new CodeUnitSyntax(methods);
    }

    private IReadOnlyList<MethodDeclarationSyntax> CreateMethodDeclarations(Sir0Function[] functions)
    {
        var result = new List<MethodDeclarationSyntax>(functions.Length);

        foreach (Sir0Function function in functions)
            result.Add(CreateMethodDeclaration(function));

        return [.. result];
    }

    private MethodDeclarationSyntax CreateMethodDeclaration(Sir0Function function)
    {
        var name = CreateStringLiteralExpression(function.Name);
        var parameters = CreateMethodDeclarationParameters();
        var body = CreateMethodDeclarationBody(function);

        return new MethodDeclarationSyntax(name, parameters, body);
    }

    private MethodDeclarationParametersSyntax CreateMethodDeclarationParameters()
    {
        SyntaxToken parenOpen = syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        SyntaxToken parenClose = syntaxFactory.Token(SyntaxTokenKind.ParenClose);

        return new MethodDeclarationParametersSyntax(parenOpen, null, parenClose);
    }

    private BlockExpression CreateMethodDeclarationBody(Sir0Function function)
    {
        SyntaxToken curlyOpen = syntaxFactory.Token(SyntaxTokenKind.CurlyOpen);
        var expressions = CreateStatements(function);
        SyntaxToken curlyClose = syntaxFactory.Token(SyntaxTokenKind.CurlyClose);

        return new BlockExpression(curlyOpen, expressions, curlyClose);
    }

    private IReadOnlyList<StatementSyntax> CreateStatements(Sir0Function function)
    {
        return CreateStatements(function.Operations);
    }

    private IReadOnlyList<StatementSyntax> CreateStatements(Sir0Operation[] operations)
    {
        IReadOnlyList<StatementBlock> blocks = blockBuilder.CreateStatementBlocks(operations);
        var result = new List<StatementSyntax>();

        foreach (StatementBlock block in blocks)
        {
            Sir0Operation[] blockOperations = [.. block.Operations];

            for (var index = 0; index < blockOperations.Length;)
            {
                Sir0Operation operation = blockOperations[index];

                if (operation.Label is not null)
                    result.Add(CreateGotoLabelStatement(operation.Label));

                StatementSyntax? statement = CreateStatement(blockOperations, ref index);
                if (statement is not null)
                    result.Add(statement);

                if (operation.Label is not null && index < blockOperations.Length && blockOperations[index] == operation)
                    index++;
            }
        }

        return result;
    }

    private GotoLabelStatementSyntax CreateGotoLabelStatement(string label)
    {
        var labelLiteral = CreateStringLiteralExpression(label);
        SyntaxToken colonToken = syntaxFactory.Token(SyntaxTokenKind.Colon);

        return new GotoLabelStatementSyntax(labelLiteral, colonToken);
    }

    private StatementSyntax? CreateStatement(Sir0Operation[] operations, ref int index)
    {
        Sir0Operation operation = operations[index++];

        switch (operation.Command)
        {
            case 0x25:
                return null;

            case 0x26:
            case 0x30:
                return CreateReturnStatement();

            case 0x2B:
                return CreateAsyncBlockStatement(operations, ref index);

            default:
                return CreateMethodInvocationExpression(CreateName($"sub{operation.Command}"), operation);
        }
    }

    private ReturnStatementSyntax CreateReturnStatement()
    {
        SyntaxToken returnToken = syntaxFactory.Token(SyntaxTokenKind.ReturnKeyword);
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new ReturnStatementSyntax(returnToken, null, semicolon);
    }

    private AsyncBlockStatement CreateAsyncBlockStatement(Sir0Operation[] operations, ref int index)
    {
        for (var i = index; i < operations.Length; i++)
        {
            if (operations[i].Command is not 0x2C)
                continue;

            SyntaxToken asyncToken = syntaxFactory.Token(SyntaxTokenKind.AsyncKeyword);
            var asyncStatements = CreateAsyncBlockBody(operations[index..i]);

            index = i + 1;

            return new AsyncBlockStatement(asyncToken, asyncStatements);
        }

        throw new InvalidOperationException("Incomplete async block.");
    }

    private BlockExpression CreateAsyncBlockBody(Sir0Operation[] operations)
    {
        SyntaxToken curlyOpen = syntaxFactory.Token(SyntaxTokenKind.CurlyOpen);
        var expressions = CreateStatements(operations);
        SyntaxToken curlyClose = syntaxFactory.Token(SyntaxTokenKind.CurlyClose);

        return new BlockExpression(curlyOpen, expressions, curlyClose);
    }

    private MethodInvocationStatementSyntax CreateMethodInvocationExpression(NameSyntax methodName, Sir0Operation operation)
    {
        var parameters = CreateMethodInvocationExpressionParameters(operation);
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new MethodInvocationStatementSyntax(methodName, parameters, semicolon);
    }

    private MethodInvocationParametersSyntax CreateMethodInvocationExpressionParameters(Sir0Operation operation)
    {
        SyntaxToken parenOpen = syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        var parameterList = CreateValueList(operation);
        SyntaxToken parenClose = syntaxFactory.Token(SyntaxTokenKind.ParenClose);

        return new MethodInvocationParametersSyntax(parenOpen, parameterList, parenClose);
    }

    private CommaSeparatedSyntaxList<LiteralExpressionSyntax>? CreateValueList(Sir0Operation operation)
    {
        if (operation.Arguments.Length <= 0)
            return null;

        var result = new List<LiteralExpressionSyntax>();

        foreach (object argument in operation.Arguments)
        {
            switch (argument)
            {
                case float floatValue:
                    result.Add(CreateFloatingNumericLiteralExpression(floatValue));
                    break;

                case int intValue:
                    result.Add(CreateNumericLiteralExpression(intValue));
                    break;

                case string stringValue:
                    result.Add(CreateStringLiteralExpression(stringValue));
                    break;

                default:
                    throw new InvalidOperationException($"Unknown value type for operation 0x{operation.Command:X2}.");
            }
        }

        return new CommaSeparatedSyntaxList<LiteralExpressionSyntax>(result);
    }

    private LiteralExpressionSyntax CreateNumericLiteralExpression(int value)
    {
        return new LiteralExpressionSyntax(syntaxFactory.NumericLiteral(value));
    }

    private LiteralExpressionSyntax CreateFloatingNumericLiteralExpression(float value)
    {
        return new LiteralExpressionSyntax(syntaxFactory.FloatingNumericLiteral(value));
    }

    private LiteralExpressionSyntax CreateStringLiteralExpression(string value)
    {
        return new LiteralExpressionSyntax(syntaxFactory.StringLiteral(value));
    }

    private NameSyntax CreateName(string name)
    {
        if (name.Contains('.'))
            return new SimpleNameSyntax(syntaxFactory.Identifier(name));

        NameSyntax? result = null;

        foreach (string part in name.Split('.').Reverse())
        {
            if (result is null)
                result = new SimpleNameSyntax(syntaxFactory.Identifier(part));
            else
                result = new QualifiedNameSyntax(new SimpleNameSyntax(syntaxFactory.Identifier(part)), syntaxFactory.Token(SyntaxTokenKind.Dot), result);
        }

        return result!;
    }
}