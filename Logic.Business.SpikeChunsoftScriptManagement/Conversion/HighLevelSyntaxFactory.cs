using Logic.Business.SpikeChunsoftScriptManagement.Enums.Conversion;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;
using Logic.Domain.CodeAnalysisManagement.Contract.SpikeChunsoft;

namespace Logic.Business.SpikeChunsoftScriptManagement.Conversion;

internal class HighLevelSyntaxFactory(ISpikeChunsoftSyntaxFactory syntaxFactory)
{
    public IfStatementSyntax CreateIfStatement(IReadOnlyList<StatementSyntax> thenStatements, ExpressionSyntax condition)
    {
        SyntaxToken ifToken = syntaxFactory.Token(SyntaxTokenKind.IfKeyword);
        SyntaxToken parenOpen = syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        SyntaxToken parenClose = syntaxFactory.Token(SyntaxTokenKind.ParenClose);
        BlockExpression body = CreateBlockExpression(thenStatements);

        return new IfStatementSyntax(ifToken, parenOpen, condition, parenClose, body);
    }

    public IfElseStatementSyntax CreateIfElseStatement(IReadOnlyList<StatementSyntax> thenStatements, IReadOnlyList<StatementSyntax> elseStatements,
        ExpressionSyntax condition)
    {
        SyntaxToken ifToken = syntaxFactory.Token(SyntaxTokenKind.IfKeyword);
        SyntaxToken parenOpen = syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        SyntaxToken parenClose = syntaxFactory.Token(SyntaxTokenKind.ParenClose);
        BlockExpression body = CreateBlockExpression(thenStatements);
        SyntaxToken elseToken = syntaxFactory.Token(SyntaxTokenKind.ElseKeyword);
        BlockExpression elseBody = CreateElseBlockExpression(elseStatements);

        return new IfElseStatementSyntax(ifToken, parenOpen, condition, parenClose, body, elseToken, elseBody);
    }

    public IfNotStatementSyntax CreateIfNotStatement(IReadOnlyList<StatementSyntax> thenStatements, ExpressionSyntax condition)
    {
        SyntaxToken ifToken = syntaxFactory.Token(SyntaxTokenKind.IfKeyword);
        SyntaxToken notToken = syntaxFactory.Token(SyntaxTokenKind.NotKeyword);
        SyntaxToken parenOpen = syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        SyntaxToken parenClose = syntaxFactory.Token(SyntaxTokenKind.ParenClose);
        BlockExpression body = CreateBlockExpression(thenStatements);

        return new IfNotStatementSyntax(ifToken, notToken, parenOpen, condition, parenClose, body);
    }

    public IfNotElseStatementSyntax CreateIfNotElseStatement(IReadOnlyList<StatementSyntax> thenStatements,
        IReadOnlyList<StatementSyntax> elseStatements, ExpressionSyntax condition)
    {
        SyntaxToken ifToken = syntaxFactory.Token(SyntaxTokenKind.IfKeyword);
        SyntaxToken notToken = syntaxFactory.Token(SyntaxTokenKind.NotKeyword);
        SyntaxToken parenOpen = syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        SyntaxToken parenClose = syntaxFactory.Token(SyntaxTokenKind.ParenClose);
        BlockExpression body = CreateBlockExpression(thenStatements);
        SyntaxToken elseToken = syntaxFactory.Token(SyntaxTokenKind.ElseKeyword);
        BlockExpression elseBody = CreateElseBlockExpression(elseStatements);

        return new IfNotElseStatementSyntax(ifToken, notToken, parenOpen, condition, parenClose, body, elseToken, elseBody);
    }

    public SwitchStatementSyntax CreateSwitchStatement(ExpressionSyntax expression, IReadOnlyList<CaseStatementSyntax> cases)
    {
        SyntaxToken switchToken = syntaxFactory.Token(SyntaxTokenKind.SwitchKeyword);
        SyntaxToken parenOpen = syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        SyntaxToken parenClose = syntaxFactory.Token(SyntaxTokenKind.ParenClose);
        SyntaxToken curlyOpen = syntaxFactory.Token(SyntaxTokenKind.CurlyOpen);
        SyntaxToken curlyClose = syntaxFactory.Token(SyntaxTokenKind.CurlyClose);

        return new SwitchStatementSyntax(switchToken, parenOpen, expression, parenClose, curlyOpen, cases, curlyClose);
    }

    public CaseStatementSyntax CreateCaseStatement(ExpressionSyntax label, IReadOnlyList<StatementSyntax> statements)
    {
        SyntaxToken caseToken = syntaxFactory.Token(SyntaxTokenKind.CaseKeyword);
        SyntaxToken colon = syntaxFactory.Token(SyntaxTokenKind.Colon);

        return new CaseStatementSyntax(caseToken, label, colon, statements);
    }

    public StatementSyntax CreateLoopStatement(LoopConditionKind conditionKind, IReadOnlyList<StatementSyntax> bodyStatements, ExpressionSyntax? condition)
    {
        if (conditionKind is LoopConditionKind.True)
            return CreateDoWhileStatement(bodyStatements, CreateTrueLiteralExpression());

        if (condition is null)
            throw new InvalidOperationException("No condition for loop.");

        return conditionKind switch
        {
            LoopConditionKind.Not => CreateDoWhileNotStatement(bodyStatements, condition),
            LoopConditionKind.Normal => CreateDoWhileStatement(bodyStatements, condition),
            _ => throw new InvalidOperationException($"Unknown loop condition {conditionKind}.")
        };
    }

    private DoWhileStatementSyntax CreateDoWhileStatement(IReadOnlyList<StatementSyntax> bodyStatements, ExpressionSyntax condition)
    {
        SyntaxToken doToken = syntaxFactory.Token(SyntaxTokenKind.DoKeyword);
        BlockExpression body = CreateBlockExpression(bodyStatements);
        SyntaxToken whileToken = syntaxFactory.Token(SyntaxTokenKind.WhileKeyword);
        SyntaxToken parenOpen = syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        SyntaxToken parenClose = syntaxFactory.Token(SyntaxTokenKind.ParenClose);
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new DoWhileStatementSyntax(doToken, body, whileToken, parenOpen, condition, parenClose, semicolon);
    }

    private DoWhileNotStatementSyntax CreateDoWhileNotStatement(IReadOnlyList<StatementSyntax> bodyStatements, ExpressionSyntax condition)
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

    public BreakStatementSyntax CreateBreakStatement()
    {
        SyntaxToken breakToken = syntaxFactory.Token(SyntaxTokenKind.BreakKeyword);
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new BreakStatementSyntax(breakToken, semicolon);
    }

    public ContinueStatementSyntax CreateContinueStatement()
    {
        SyntaxToken continueToken = syntaxFactory.Token(SyntaxTokenKind.ContinueKeyword);
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new ContinueStatementSyntax(continueToken, semicolon);
    }

    private BlockExpression CreateBlockExpression(IReadOnlyList<StatementSyntax> statements)
    {
        SyntaxToken curlyOpen = syntaxFactory.Token(SyntaxTokenKind.CurlyOpen);
        SyntaxToken curlyClose = syntaxFactory.Token(SyntaxTokenKind.CurlyClose);

        return new BlockExpression(curlyOpen, statements, curlyClose);
    }

    public void AddLoopControlStatements(List<StatementSyntax> target, byte terminalCommand, LoopControlKind controlKind, ExpressionSyntax? condition)
    {
        if (terminalCommand is 0x35)
        {
            target.Add(controlKind == LoopControlKind.Break ? CreateBreakStatement() : CreateContinueStatement());
            return;
        }

        if (condition is null)
            throw new InvalidOperationException("No condition for loop.");

        if (terminalCommand is 0x36)
        {
            target.Add(CreateIfStatement([controlKind == LoopControlKind.Break ? CreateBreakStatement() : CreateContinueStatement()], condition));
            return;
        }

        if (terminalCommand is 0x37)
        {
            StatementSyntax elseStatement = controlKind == LoopControlKind.Break ? CreateBreakStatement() : CreateContinueStatement();
            target.Add(CreateIfElseStatement([], [elseStatement], condition));
        }
    }

    private BlockExpression CreateElseBlockExpression(IReadOnlyList<StatementSyntax> statements)
    {
        if (IsElseIfCandidate(statements))
            return CreateInlineBlockExpression(statements);

        return CreateBlockExpression(statements);
    }

    private BlockExpression CreateInlineBlockExpression(IReadOnlyList<StatementSyntax> statements)
    {
        SyntaxToken curlyOpen = syntaxFactory.Create(string.Empty, (int)SyntaxTokenKind.CurlyOpen);
        SyntaxToken curlyClose = syntaxFactory.Create(string.Empty, (int)SyntaxTokenKind.CurlyClose);

        return new BlockExpression(curlyOpen, statements, curlyClose);
    }

    private static bool IsElseIfCandidate(IReadOnlyList<StatementSyntax> statements)
    {
        if (statements.Count != 1)
            return false;

        return statements[0] is IfStatementSyntax or IfNotStatementSyntax or IfElseStatementSyntax or IfNotElseStatementSyntax;
    }

    private LiteralExpressionSyntax CreateTrueLiteralExpression()
    {
        SyntaxToken trueKeyword = syntaxFactory.Token(SyntaxTokenKind.TrueKeyword);

        return new LiteralExpressionSyntax(trueKeyword);
    }
}
