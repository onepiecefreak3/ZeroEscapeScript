using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;
using Logic.Domain.CodeAnalysisManagement.Contract.SpikeChunsoft;
using Logic.Domain.CodeAnalysisManagement.DataClasses.SpikeChunsoft;

namespace Logic.Domain.CodeAnalysisManagement.SpikeChunsoft;

internal class SpikeChunsoftScriptWhitespaceNormalizer : ISpikeChunsoftScriptWhitespaceNormalizer
{
    public void NormalizeCodeUnit(CodeUnitSyntax codeUnit)
    {
        var ctx = new WhitespaceNormalizeContext();
        NormalizeCodeUnit(codeUnit, ctx);

        codeUnit.Update();
    }

    private void NormalizeCodeUnit(CodeUnitSyntax codeUnit, WhitespaceNormalizeContext ctx)
    {
        foreach (MethodDeclarationSyntax methodDeclaration in codeUnit.MethodDeclarations)
        {
            ctx.IsFirstElement = codeUnit.MethodDeclarations[0] == methodDeclaration;
            ctx.ShouldLineBreak = codeUnit.MethodDeclarations[^1] != methodDeclaration;
            NormalizeMethodDeclaration(methodDeclaration, ctx);
        }
    }

    private void NormalizeMethodDeclaration(MethodDeclarationSyntax methodDeclaration, WhitespaceNormalizeContext ctx)
    {
        bool shouldLineBreak = ctx.ShouldLineBreak;

        ctx.ShouldLineBreak = true;
        NormalizeMethodDeclarationParameters(methodDeclaration.Parameters, ctx);

        ctx.ShouldLineBreak = shouldLineBreak;
        NormalizeMethodDeclarationBody(methodDeclaration.Body, ctx);

        ctx.IsFirstElement = true;
        ctx.ShouldLineBreak = false;
        NormalizeLiteralExpression(methodDeclaration.Name, ctx);
    }

    private void NormalizeMethodDeclarationParameters(MethodDeclarationParametersSyntax methodDeclarationParameters, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newParenOpen = methodDeclarationParameters.ParenOpen.WithLeadingTrivia(null).WithLeadingTrivia(null);
        SyntaxToken newParenClose = methodDeclarationParameters.ParenClose.WithLeadingTrivia(null).WithLeadingTrivia(null);

        if (ctx.ShouldLineBreak)
            newParenClose = newParenClose.WithTrailingTrivia("\r\n");

        methodDeclarationParameters.SetParenOpen(newParenOpen, false);
        NormalizeMethodDeclarationParameterList(methodDeclarationParameters.Parameters, ctx);
        methodDeclarationParameters.SetParenClose(newParenClose, false);
    }

    private void NormalizeMethodDeclarationParameterList(CommaSeparatedSyntaxList<LiteralExpressionSyntax>? list,
        WhitespaceNormalizeContext ctx)
    {
        if (list == null)
            return;

        foreach (LiteralExpressionSyntax value in list.Elements)
        {
            ctx.IsFirstElement = list.Elements[0] == value;
            ctx.ShouldLineBreak = false;
            NormalizeLiteralExpression(value, ctx);
        }
    }

    private void NormalizeMethodDeclarationBody(BlockExpression methodDeclarationBody, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newCurlyOpen = methodDeclarationBody.CurlyOpen.WithLeadingTrivia(null).WithTrailingTrivia("\r\n");
        SyntaxToken newCurlyClose = methodDeclarationBody.CurlyClose.WithNoTrivia();

        if (ctx.ShouldLineBreak)
            newCurlyClose = newCurlyClose.WithTrailingTrivia("\r\n\r\n");

        methodDeclarationBody.SetCurlyOpen(newCurlyOpen, false);
        methodDeclarationBody.SetCurlyClose(newCurlyClose, false);

        ctx.Indent++;
        foreach (StatementSyntax expression in methodDeclarationBody.Statements)
        {
            ctx.IsFirstElement = methodDeclarationBody.Statements[0] == expression;
            ctx.ShouldLineBreak = true;
            ctx.ShouldIndent = true;

            NormalizeStatement(expression, ctx);
        }
    }

    private void NormalizeStatement(StatementSyntax statement, WhitespaceNormalizeContext ctx)
    {
        switch (statement)
        {
            case AsyncBlockStatement asyncStatement:
                NormalizeAsyncBlockStatement(asyncStatement, ctx);
                break;

            case IfStatementSyntax ifStatement:
                NormalizeIfStatement(ifStatement, ctx);
                break;

            case IfElseStatementSyntax ifElseStatement:
                NormalizeIfElseStatement(ifElseStatement, ctx);
                break;

            case DoWhileStatementSyntax doWhileStatement:
                NormalizeDoWhileStatement(doWhileStatement, ctx);
                break;

            case BreakStatementSyntax breakStatement:
                NormalizeBreakStatement(breakStatement, ctx);
                break;

            case ContinueStatementSyntax continueStatement:
                NormalizeContinueStatement(continueStatement, ctx);
                break;

            case ReturnStatementSyntax returnStatement:
                NormalizeReturnStatement(returnStatement, ctx);
                break;

            case MethodInvocationStatementSyntax methodInvocationStatement:
                NormalizeMethodInvocationStatement(methodInvocationStatement, ctx);
                break;
        }
    }

    private void NormalizeReturnStatement(ReturnStatementSyntax returnStatement, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newReturnKeyword = returnStatement.Return.WithNoTrivia();
        SyntaxToken newSemicolon = returnStatement.Semicolon.WithNoTrivia();

        if (returnStatement.Expression != null)
            newReturnKeyword = newReturnKeyword.WithTrailingTrivia(" ");

        if (ctx is { ShouldIndent: true, Indent: > 0 })
            newReturnKeyword = newReturnKeyword.WithLeadingTrivia(new string('\t', ctx.Indent));

        if (ctx.ShouldLineBreak)
            newSemicolon = newSemicolon.WithTrailingTrivia("\r\n");

        returnStatement.SetReturn(newReturnKeyword, false);
        returnStatement.SetSemicolon(newSemicolon, false);

        if (returnStatement.Expression == null)
            return;

        ctx.IsFirstElement = true;
        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        NormalizeLiteralExpression(returnStatement.Expression, ctx);
    }

    private void NormalizeAsyncBlockStatement(AsyncBlockStatement asyncBlock, WhitespaceNormalizeContext ctx)
    {
        string? leadingTrivia = null;
        if (ctx is { ShouldIndent: true, Indent: > 0 })
            leadingTrivia = new string('\t', ctx.Indent);

        SyntaxToken asyncKeyword = asyncBlock.Async.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia("\r\n");
        asyncBlock.SetAsync(asyncKeyword, false);

        NormalizeBlock(asyncBlock.Body, ctx, "\r\n");
    }

    private void NormalizeIfStatement(IfStatementSyntax ifStatement, WhitespaceNormalizeContext ctx)
    {
        string? leadingTrivia = null;
        if (ctx is { ShouldIndent: true, Indent: > 0 })
            leadingTrivia = new string('\t', ctx.Indent);

        SyntaxToken ifToken = ifStatement.If.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(" ");
        SyntaxToken parenOpen = ifStatement.ParenOpen.WithNoTrivia();
        SyntaxToken parenClose = ifStatement.ParenClose.WithNoTrivia().WithTrailingTrivia("\r\n");

        ifStatement.SetIf(ifToken, false);
        ifStatement.SetParenOpen(parenOpen, false);
        ifStatement.SetParenClose(parenClose, false);

        NormalizeBlock(ifStatement.Body, ctx, "\r\n");

        ctx.IsFirstElement = true;
        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        NormalizeLiteralExpression(ifStatement.Condition, ctx);
    }

    private void NormalizeIfElseStatement(IfElseStatementSyntax ifElseStatement, WhitespaceNormalizeContext ctx)
    {
        string? leadingTrivia = null;
        if (ctx is { ShouldIndent: true, Indent: > 0 })
            leadingTrivia = new string('\t', ctx.Indent);

        SyntaxToken ifToken = ifElseStatement.If.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(" ");
        SyntaxToken elseToken = ifElseStatement.Else.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia("\r\n");
        SyntaxToken parenOpen = ifElseStatement.ParenOpen.WithNoTrivia();
        SyntaxToken parenClose = ifElseStatement.ParenClose.WithNoTrivia().WithTrailingTrivia("\r\n");

        ifElseStatement.SetIf(ifToken, false);
        ifElseStatement.SetElse(elseToken, false);
        ifElseStatement.SetParenOpen(parenOpen, false);
        ifElseStatement.SetParenClose(parenClose, false);

        NormalizeBlock(ifElseStatement.Body, ctx, "\r\n");
        NormalizeBlock(ifElseStatement.ElseBody, ctx, "\r\n");

        ctx.IsFirstElement = true;
        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        NormalizeLiteralExpression(ifElseStatement.Condition, ctx);
    }

    private void NormalizeDoWhileStatement(DoWhileStatementSyntax doWhileStatement, WhitespaceNormalizeContext ctx)
    {
        string? leadingTrivia = null;
        if (ctx is { ShouldIndent: true, Indent: > 0 })
            leadingTrivia = new string('\t', ctx.Indent);

        SyntaxToken doToken = doWhileStatement.Do.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia("\r\n");
        SyntaxToken whileToken = doWhileStatement.While.WithNoTrivia().WithTrailingTrivia(" ");
        SyntaxToken parenOpen = doWhileStatement.ParenOpen.WithNoTrivia();
        SyntaxToken parenClose = doWhileStatement.ParenClose.WithNoTrivia();
        SyntaxToken semicolon = doWhileStatement.Semicolon.WithNoTrivia();

        if (ctx.ShouldLineBreak)
            semicolon = semicolon.WithTrailingTrivia("\r\n");

        doWhileStatement.SetWhile(whileToken, false);
        doWhileStatement.SetParenOpen(parenOpen, false);
        doWhileStatement.SetParenClose(parenClose, false);
        doWhileStatement.SetDo(doToken, false);
        doWhileStatement.SetSemicolon(semicolon, false);

        NormalizeBlock(doWhileStatement.Body, ctx, " ");

        ctx.IsFirstElement = true;
        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        NormalizeLiteralExpression(doWhileStatement.Condition, ctx);
    }

    private void NormalizeBreakStatement(BreakStatementSyntax breakStatement, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken breakToken = breakStatement.Break.WithNoTrivia();
        SyntaxToken semicolon = breakStatement.Semicolon.WithNoTrivia();

        if (ctx is { ShouldIndent: true, Indent: > 0 })
            breakToken = breakToken.WithLeadingTrivia(new string('\t', ctx.Indent));

        if (ctx.ShouldLineBreak)
            semicolon = semicolon.WithTrailingTrivia("\r\n");

        breakStatement.SetBreak(breakToken, false);
        breakStatement.SetSemicolon(semicolon, false);
    }

    private void NormalizeContinueStatement(ContinueStatementSyntax continueStatement, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken continueToken = continueStatement.Continue.WithNoTrivia();
        SyntaxToken semicolon = continueStatement.Semicolon.WithNoTrivia();

        if (ctx is { ShouldIndent: true, Indent: > 0 })
            continueToken = continueToken.WithLeadingTrivia(new string('\t', ctx.Indent));

        if (ctx.ShouldLineBreak)
            semicolon = semicolon.WithTrailingTrivia("\r\n");

        continueStatement.SetContinue(continueToken, false);
        continueStatement.SetSemicolon(semicolon, false);
    }

    private void NormalizeBlock(BlockExpression methodDeclarationBody, WhitespaceNormalizeContext ctx, string? trailingTrivia)
    {
        string? leadingTrivia = null;
        if (ctx is { ShouldIndent: true, Indent: > 0 })
            leadingTrivia = new string('\t', ctx.Indent);

        SyntaxToken newCurlyOpen = methodDeclarationBody.CurlyOpen.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia("\r\n");
        SyntaxToken newCurlyClose = methodDeclarationBody.CurlyClose.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(trailingTrivia);

        methodDeclarationBody.SetCurlyOpen(newCurlyOpen, false);
        methodDeclarationBody.SetCurlyClose(newCurlyClose, false);

        ctx.Indent++;
        foreach (StatementSyntax expression in methodDeclarationBody.Statements)
        {
            ctx.IsFirstElement = methodDeclarationBody.Statements[0] == expression;
            ctx.ShouldLineBreak = true;
            ctx.ShouldIndent = true;

            NormalizeStatement(expression, ctx);
        }
    }

    private void NormalizeMethodInvocationStatement(MethodInvocationStatementSyntax invocation, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newSemicolon = invocation.Semicolon.WithNoTrivia();

        if (ctx.ShouldLineBreak)
            newSemicolon = newSemicolon.WithTrailingTrivia("\r\n");

        NormalizeName(invocation.Name, ctx);

        invocation.SetSemicolon(newSemicolon, false);

        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        NormalizeMethodInvocationParameters(invocation.Parameters, ctx);
    }

    private void NormalizeMethodInvocationParameters(MethodInvocationParametersSyntax invocationParameters, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken parenOpen = invocationParameters.ParenOpen.WithNoTrivia();
        SyntaxToken parenClose = invocationParameters.ParenClose.WithNoTrivia();

        invocationParameters.SetParenOpen(parenOpen, false);
        invocationParameters.SetParenClose(parenClose, false);

        NormalizeLiteralExpressions(invocationParameters.ParameterList, ctx);
    }

    private void NormalizeLiteralExpressions(CommaSeparatedSyntaxList<LiteralExpressionSyntax>? valueList, WhitespaceNormalizeContext ctx)
    {
        if (valueList == null)
            return;

        foreach (LiteralExpressionSyntax value in valueList.Elements)
        {
            ctx.IsFirstElement = valueList.Elements[0] == value;
            NormalizeLiteralExpression(value, ctx);
        }
    }

    private void NormalizeLiteralExpression(LiteralExpressionSyntax literal, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken literalToken = literal.Literal.WithNoTrivia();

        string? leadingTrivia = null;
        if (ctx is { ShouldIndent: true, Indent: > 0 })
            leadingTrivia = new string('\t', ctx.Indent);
        if (!ctx.IsFirstElement)
            leadingTrivia += " ";

        literalToken = literalToken.WithLeadingTrivia(leadingTrivia);
        if (ctx.ShouldLineBreak)
            literalToken = literalToken.WithTrailingTrivia("\r\n");

        literal.SetLiteral(literalToken, false);
    }

    private void NormalizeName(NameSyntax name, WhitespaceNormalizeContext ctx)
    {
        switch (name)
        {
            case SimpleNameSyntax simpleName:
                NormalizeSimpleName(simpleName, ctx);
                break;

            case QualifiedNameSyntax qualifiedName:
                NormalizeQualifiedName(qualifiedName, ctx);
                break;
        }
    }

    private void NormalizeSimpleName(SimpleNameSyntax name, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken identifierToken = name.Identifier.WithNoTrivia();

        string? leadingTrivia = null;
        if (ctx is { ShouldIndent: true, Indent: > 0 })
            leadingTrivia = new string('\t', ctx.Indent);

        identifierToken = identifierToken.WithLeadingTrivia(leadingTrivia);

        name.SetIdentifier(identifierToken, false);
    }

    private void NormalizeQualifiedName(QualifiedNameSyntax name, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken dotToken = name.Dot.WithNoTrivia();

        name.SetDot(dotToken, false);

        NormalizeName(name.Left, ctx);
        NormalizeName(name.Right, ctx);
    }
}