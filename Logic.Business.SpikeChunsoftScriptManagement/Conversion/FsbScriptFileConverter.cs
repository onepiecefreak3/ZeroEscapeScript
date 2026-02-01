using System.Diagnostics.CodeAnalysis;
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
        IReadOnlyList<StatementBlock> blocks = blockBuilder.Build(operations);
        return CreateStatements(blocks);
    }

    private StatementSyntax CreateStatement(Sir0Operation[] operations, ref int index)
    {
        Sir0Operation operation = operations[index++];

        switch (operation.Command)
        {
            case 0x30:
                return CreateReturnStatement();

            case 0x2B:
                return CreateAsyncBlockStatement(operations, ref index);

            default:
                return CreateMethodInvocationExpression(CreateName($"sub{operation.Command}"), operation);
        }
    }

    private GotoStatementSyntax CreateGotoStatement(string jumpLabel)
    {
        SyntaxToken gotoToken = syntaxFactory.Token(SyntaxTokenKind.GotoKeyword);
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        var label = CreateStringLiteralExpression(jumpLabel);

        return new GotoStatementSyntax(gotoToken, label, semicolon);
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

    private LiteralExpressionSyntax CreateTrueLiteralExpression()
    {
        return new LiteralExpressionSyntax(syntaxFactory.Token(SyntaxTokenKind.TrueKeyword));
    }

    private GotoLabelStatementSyntax CreateGotoLabelStatement(string jumpLabel)
    {
        var colon = syntaxFactory.Token(SyntaxTokenKind.Colon);

        return new GotoLabelStatementSyntax(CreateStringLiteralExpression(jumpLabel), colon);
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

    private IReadOnlyList<StatementSyntax> CreateStatements(IReadOnlyList<StatementBlock> blocks)
    {
        if (blocks.Count == 0)
            return [];

        Dictionary<string, int> labelLookup = CreateLabelLookup(blocks);
        Dictionary<int, LoopBound> loopBounds = CreateLoopBounds(blocks, labelLookup);

        return BuildStatementsRange(blocks, labelLookup, loopBounds, 0, blocks.Count);
    }

    private Dictionary<string, int> CreateLabelLookup(IReadOnlyList<StatementBlock> blocks)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < blocks.Count; i++)
        {
            foreach (string label in blocks[i].Labels)
            {
                if (!result.TryAdd(label, i))
                    throw new InvalidOperationException($"Duplicate jump label {label}.");
            }
        }

        return result;
    }

    private Dictionary<int, LoopBound> CreateLoopBounds(IReadOnlyList<StatementBlock> blocks, IReadOnlyDictionary<string, int> labelLookup)
    {
        var result = new Dictionary<int, LoopBound>();
        for (var i = 0; i < blocks.Count; i++)
        {
            StatementBlock block = blocks[i];
            if (block.TerminalCommand is not (0x35 or 0x36 or 0x37))
                continue;

            if (block.JumpLabel is null || !labelLookup.TryGetValue(block.JumpLabel, out int targetIndex))
                continue;

            if (targetIndex > i)
                continue;

            LoopConditionKind conditionKind = block.TerminalCommand switch
            {
                0x35 => LoopConditionKind.True,
                0x36 => LoopConditionKind.Not,
                0x37 => LoopConditionKind.Normal,
                _ => throw new InvalidOperationException($"Unknown terminal command 0x{block.TerminalCommand:X00}.")
            };

            _ = result.TryAdd(targetIndex, new LoopBound(i, conditionKind));
        }

        return result;
    }

    private IReadOnlyList<StatementSyntax> BuildStatementsRange(IReadOnlyList<StatementBlock> blocks,
        Dictionary<string, int> labelLookup, Dictionary<int, LoopBound> loopBounds, int startIndex, int endIndex,
        bool skipLoopStart = false, LoopContext? loopContext = null)
    {
        var result = new List<StatementSyntax>();
        for (var i = startIndex; i < endIndex;)
        {
            if (!skipLoopStart && loopBounds.TryGetValue(i, out LoopBound? loopBound) && loopBound.EndIndex < endIndex)
            {
                int exitIndex = Math.Min(loopBound.EndIndex + 1, blocks.Count);
                var nestedContext = new LoopContext(i, loopBound.EndIndex, exitIndex);
                IReadOnlyList<StatementSyntax> bodyStatements = BuildStatementsRange(blocks, labelLookup, loopBounds, i,
                    loopBound.EndIndex + 1, true,
                    nestedContext);
                result.Add(CreateLoopStatement(loopBound.ConditionKind, bodyStatements));
                i = loopBound.EndIndex + 1;
                continue;
            }

            skipLoopStart = false;

            if (TryBuildIfStatement(blocks, labelLookup, loopBounds, i, endIndex, loopContext, out StatementSyntax? ifStatement,
                    out int nextIndex))
            {
                result.AddRange(CreateStatementsFromBlock(blocks, labelLookup, loopContext, i, true));
                result.Add(ifStatement);
                i = nextIndex;
                continue;
            }

            result.AddRange(CreateStatementsFromBlock(blocks, labelLookup, loopContext, i, true));
            i++;
        }

        return result;
    }

    private bool TryBuildIfStatement(IReadOnlyList<StatementBlock> blocks, Dictionary<string, int> labelLookup,
        Dictionary<int, LoopBound> loopBounds, int index, int endIndex, LoopContext? loopContext,
        [NotNullWhen(true)] out StatementSyntax? statement, out int nextIndex)
    {
        statement = null;
        nextIndex = index + 1;

        StatementBlock block = blocks[index];
        if (block.TerminalCommand is not (0x36 or 0x37) || block.JumpLabel is null)
            return false;

        if (!labelLookup.TryGetValue(block.JumpLabel, out int targetIndex))
            return false;

        if (targetIndex <= index || targetIndex > endIndex)
            return false;

        if (block.TerminalCommand is 0x37)
        {
            int thenStart = index + 1;
            int thenEnd = targetIndex;
            if (TryBuildIfElseOnFalse(blocks, labelLookup, loopBounds, thenStart, thenEnd, targetIndex, loopContext, out statement,
                    out nextIndex))
                return true;

            IReadOnlyList<StatementSyntax> thenStatements = BuildStatementsRange(blocks, labelLookup, loopBounds, thenStart, thenEnd, false,
                loopContext);
            statement = CreateIfStatement(thenStatements);
            nextIndex = targetIndex;
            return true;
        }

        int elseStart = index + 1;
        int elseEnd = targetIndex;
        if (TryBuildIfElseOnTrue(blocks, labelLookup, loopBounds, elseStart, elseEnd, targetIndex, loopContext, out statement, out nextIndex))
            return true;

        IReadOnlyList<StatementSyntax> notStatements = BuildStatementsRange(blocks, labelLookup, loopBounds, elseStart, elseEnd, false,
            loopContext);
        statement = CreateIfNotStatement(notStatements);
        nextIndex = targetIndex;
        return true;
    }

    private bool TryBuildIfElseOnFalse(IReadOnlyList<StatementBlock> blocks, Dictionary<string, int> labelLookup,
        Dictionary<int, LoopBound> loopBounds, int thenStart, int thenEnd, int targetIndex, LoopContext? loopContext,
        [NotNullWhen(true)] out StatementSyntax? statement, out int nextIndex)
    {
        statement = null;
        nextIndex = targetIndex;

        if (thenStart >= thenEnd)
            return false;

        StatementBlock endThenBlock = blocks[thenEnd - 1];
        if (endThenBlock.TerminalCommand is not 0x35 || endThenBlock.JumpLabel is null)
            return false;

        if (!labelLookup.TryGetValue(endThenBlock.JumpLabel, out int endIndex) || endIndex <= targetIndex)
            return false;

        IReadOnlyList<StatementSyntax> thenStatements = BuildStatementsRange(blocks, labelLookup, loopBounds, thenStart, thenEnd, false,
            loopContext);
        IReadOnlyList<StatementSyntax> elseStatements = BuildStatementsRange(blocks, labelLookup, loopBounds, targetIndex, endIndex, false,
            loopContext);

        if (elseStatements.Count <= 0)
            return false;

        statement = CreateIfElseStatement(thenStatements, elseStatements);
        nextIndex = endIndex;
        return true;
    }

    private bool TryBuildIfElseOnTrue(IReadOnlyList<StatementBlock> blocks, Dictionary<string, int> labelLookup,
        Dictionary<int, LoopBound> loopBounds, int elseStart, int elseEnd, int targetIndex, LoopContext? loopContext,
        [NotNullWhen(true)] out StatementSyntax? statement, out int nextIndex)
    {
        statement = null;
        nextIndex = targetIndex;

        if (elseStart >= elseEnd)
            return false;

        StatementBlock endElseBlock = blocks[elseEnd - 1];
        if (endElseBlock.TerminalCommand is not 0x35 || endElseBlock.JumpLabel is null)
            return false;

        if (!labelLookup.TryGetValue(endElseBlock.JumpLabel, out int endIndex) || endIndex <= targetIndex)
            return false;

        IReadOnlyList<StatementSyntax> thenStatements = BuildStatementsRange(blocks, labelLookup, loopBounds, elseStart, elseEnd, false,
            loopContext);
        IReadOnlyList<StatementSyntax> elseStatements = BuildStatementsRange(blocks, labelLookup, loopBounds, targetIndex, endIndex, false,
            loopContext);

        if (elseStatements.Count <= 0)
            return false;

        statement = CreateIfNotElseStatement(thenStatements, elseStatements);
        nextIndex = endIndex;
        return true;
    }

    private IReadOnlyList<StatementSyntax> CreateStatementsFromBlock(IReadOnlyList<StatementBlock> blocks,
        IReadOnlyDictionary<string, int>? labelLookup, LoopContext? loopContext, int blockIndex, bool skipTerminalJump)
    {
        var result = new List<StatementSyntax>();
        StatementBlock block = blocks[blockIndex];
        Sir0Operation[] blockOperations = [.. block.Operations];
        int endIndex = blockOperations.Length;

        if (skipTerminalJump && block.TerminalCommand is 0x35 or 0x36 or 0x37 && endIndex > 0)
            endIndex--;

        if (loopContext is not null && labelLookup is not null && block.TerminalCommand is 0x35 or 0x36 or 0x37 &&
            block.JumpLabel is not null && labelLookup.TryGetValue(block.JumpLabel, out int targetIndex))
        {
            if (targetIndex == loopContext.StartIndex && blockIndex != loopContext.EndIndex)
            {
                result.AddRange(CreateStatementsFromBlock(blocks, labelLookup, null, blockIndex, true));
                AddLoopControlStatements(result, block.TerminalCommand.Value, LoopControlKind.Continue);
                return result;
            }

            if (targetIndex == loopContext.ExitIndex)
            {
                result.AddRange(CreateStatementsFromBlock(blocks, labelLookup, null, blockIndex, true));
                AddLoopControlStatements(result, block.TerminalCommand.Value, LoopControlKind.Break);
                return result;
            }
        }

        for (var index = 0; index < endIndex;)
        {
            if (blockOperations[index].Command is 0x25 or 0x26)
            {
                index++;
                continue;
            }

            result.Add(CreateStatement(blockOperations, ref index));
        }

        return result;
    }

    private IfStatementSyntax CreateIfStatement(IReadOnlyList<StatementSyntax> thenStatements)
    {
        SyntaxToken ifToken = syntaxFactory.Token(SyntaxTokenKind.IfKeyword);
        SyntaxToken parenOpen = syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        LiteralExpressionSyntax condition = CreateConditionExpression();
        SyntaxToken parenClose = syntaxFactory.Token(SyntaxTokenKind.ParenClose);
        BlockExpression body = CreateBlockExpression(thenStatements);

        return new IfStatementSyntax(ifToken, parenOpen, condition, parenClose, body);
    }

    private IfElseStatementSyntax CreateIfElseStatement(IReadOnlyList<StatementSyntax> thenStatements, IReadOnlyList<StatementSyntax> elseStatements)
    {
        SyntaxToken ifToken = syntaxFactory.Token(SyntaxTokenKind.IfKeyword);
        SyntaxToken parenOpen = syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        LiteralExpressionSyntax condition = CreateConditionExpression();
        SyntaxToken parenClose = syntaxFactory.Token(SyntaxTokenKind.ParenClose);
        BlockExpression body = CreateBlockExpression(thenStatements);
        SyntaxToken elseToken = syntaxFactory.Token(SyntaxTokenKind.ElseKeyword);
        BlockExpression elseBody = CreateBlockExpression(elseStatements);

        return new IfElseStatementSyntax(ifToken, parenOpen, condition, parenClose, body, elseToken, elseBody);
    }

    private IfNotStatementSyntax CreateIfNotStatement(IReadOnlyList<StatementSyntax> thenStatements)
    {
        SyntaxToken ifToken = syntaxFactory.Token(SyntaxTokenKind.IfKeyword);
        SyntaxToken notToken = syntaxFactory.Token(SyntaxTokenKind.NotKeyword);
        SyntaxToken parenOpen = syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        LiteralExpressionSyntax condition = CreateConditionExpression();
        SyntaxToken parenClose = syntaxFactory.Token(SyntaxTokenKind.ParenClose);
        BlockExpression body = CreateBlockExpression(thenStatements);

        return new IfNotStatementSyntax(ifToken, notToken, parenOpen, condition, parenClose, body);
    }

    private IfNotElseStatementSyntax CreateIfNotElseStatement(IReadOnlyList<StatementSyntax> thenStatements,
        IReadOnlyList<StatementSyntax> elseStatements)
    {
        SyntaxToken ifToken = syntaxFactory.Token(SyntaxTokenKind.IfKeyword);
        SyntaxToken notToken = syntaxFactory.Token(SyntaxTokenKind.NotKeyword);
        SyntaxToken parenOpen = syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        LiteralExpressionSyntax condition = CreateConditionExpression();
        SyntaxToken parenClose = syntaxFactory.Token(SyntaxTokenKind.ParenClose);
        BlockExpression body = CreateBlockExpression(thenStatements);
        SyntaxToken elseToken = syntaxFactory.Token(SyntaxTokenKind.ElseKeyword);
        BlockExpression elseBody = CreateBlockExpression(elseStatements);

        return new IfNotElseStatementSyntax(ifToken, notToken, parenOpen, condition, parenClose, body, elseToken, elseBody);
    }

    private DoWhileStatementSyntax CreateDoWhileStatement(IReadOnlyList<StatementSyntax> bodyStatements)
    {
        SyntaxToken doToken = syntaxFactory.Token(SyntaxTokenKind.DoKeyword);
        BlockExpression body = CreateBlockExpression(bodyStatements);
        SyntaxToken whileToken = syntaxFactory.Token(SyntaxTokenKind.WhileKeyword);
        SyntaxToken parenOpen = syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        LiteralExpressionSyntax condition = CreateConditionExpression();
        SyntaxToken parenClose = syntaxFactory.Token(SyntaxTokenKind.ParenClose);
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new DoWhileStatementSyntax(doToken, body, whileToken, parenOpen, condition, parenClose, semicolon);
    }

    private DoWhileStatementSyntax CreateDoWhileStatement(IReadOnlyList<StatementSyntax> bodyStatements, LiteralExpressionSyntax condition)
    {
        SyntaxToken doToken = syntaxFactory.Token(SyntaxTokenKind.DoKeyword);
        BlockExpression body = CreateBlockExpression(bodyStatements);
        SyntaxToken whileToken = syntaxFactory.Token(SyntaxTokenKind.WhileKeyword);
        SyntaxToken parenOpen = syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        SyntaxToken parenClose = syntaxFactory.Token(SyntaxTokenKind.ParenClose);
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new DoWhileStatementSyntax(doToken, body, whileToken, parenOpen, condition, parenClose, semicolon);
    }

    private DoWhileNotStatementSyntax CreateDoWhileNotStatement(IReadOnlyList<StatementSyntax> bodyStatements, LiteralExpressionSyntax condition)
    {
        SyntaxToken doToken = syntaxFactory.Token(SyntaxTokenKind.DoKeyword);
        BlockExpression body = CreateBlockExpression(bodyStatements);
        SyntaxToken whileToken = syntaxFactory.Token(SyntaxTokenKind.WhileKeyword);
        SyntaxToken notToken = syntaxFactory.Token(SyntaxTokenKind.NotKeyword);
        SyntaxToken parenOpen = syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        SyntaxToken parenClose = syntaxFactory.Token(SyntaxTokenKind.ParenClose);
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new DoWhileNotStatementSyntax(doToken, body, whileToken, notToken, parenOpen, condition, parenClose, semicolon);
    }

    private LiteralExpressionSyntax CreateConditionExpression()
    {
        return CreateNumericLiteralExpression(1);
    }

    private StatementSyntax CreateLoopStatement(LoopConditionKind conditionKind, IReadOnlyList<StatementSyntax> bodyStatements)
    {
        return conditionKind switch
        {
            LoopConditionKind.Not => CreateDoWhileNotStatement(bodyStatements, CreateConditionExpression()),
            LoopConditionKind.True => CreateDoWhileStatement(bodyStatements, CreateTrueLiteralExpression()),
            LoopConditionKind.Normal => CreateDoWhileStatement(bodyStatements),
            _ => throw new InvalidOperationException($"Unknown loop condition {conditionKind}.")
        };
    }

    private BlockExpression CreateBlockExpression(IReadOnlyList<StatementSyntax> statements)
    {
        SyntaxToken curlyOpen = syntaxFactory.Token(SyntaxTokenKind.CurlyOpen);
        SyntaxToken curlyClose = syntaxFactory.Token(SyntaxTokenKind.CurlyClose);

        return new BlockExpression(curlyOpen, statements, curlyClose);
    }

    private void AddLoopControlStatements(List<StatementSyntax> target, byte terminalCommand, LoopControlKind controlKind)
    {
        if (terminalCommand is 0x35)
        {
            target.Add(controlKind == LoopControlKind.Break ? CreateBreakStatement() : CreateContinueStatement());
            return;
        }

        if (terminalCommand is 0x36)
        {
            target.Add(CreateIfStatement([controlKind == LoopControlKind.Break ? CreateBreakStatement() : CreateContinueStatement()]));
            return;
        }

        if (terminalCommand is 0x37)
        {
            StatementSyntax elseStatement = controlKind == LoopControlKind.Break ? CreateBreakStatement() : CreateContinueStatement();
            target.Add(CreateIfElseStatement([], [elseStatement]));
        }
    }

    private BreakStatementSyntax CreateBreakStatement()
    {
        SyntaxToken breakToken = syntaxFactory.Token(SyntaxTokenKind.BreakKeyword);
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new BreakStatementSyntax(breakToken, semicolon);
    }

    private ContinueStatementSyntax CreateContinueStatement()
    {
        SyntaxToken continueToken = syntaxFactory.Token(SyntaxTokenKind.ContinueKeyword);
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new ContinueStatementSyntax(continueToken, semicolon);
    }

    private sealed record LoopContext(int StartIndex, int EndIndex, int ExitIndex);

    private sealed record LoopBound(int EndIndex, LoopConditionKind ConditionKind);

    private enum LoopConditionKind
    {
        Normal,
        Not,
        True
    }

    private enum LoopControlKind
    {
        Break,
        Continue
    }
}