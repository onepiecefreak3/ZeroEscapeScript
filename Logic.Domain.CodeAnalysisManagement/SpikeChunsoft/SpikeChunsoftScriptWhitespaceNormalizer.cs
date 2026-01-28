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
            case GotoLabelStatementSyntax gotoStatement:
                NormalizeGotoLabelStatement(gotoStatement, ctx);
                break;

            case AsyncBlockStatement asyncStatement:
                NormalizeAsyncBlockStatement(asyncStatement, ctx);
                break;

            case ReturnStatementSyntax returnStatement:
                NormalizeReturnStatement(returnStatement, ctx);
                break;

            case MethodInvocationStatementSyntax methodInvocationStatement:
                NormalizeMethodInvocationStatement(methodInvocationStatement, ctx);
                break;
        }
    }

    private void NormalizeGotoLabelStatement(GotoLabelStatementSyntax gotoLabelStatement, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newLiteral = gotoLabelStatement.Label.Literal.WithNoTrivia();
        SyntaxToken newColon = gotoLabelStatement.Colon.WithNoTrivia();

        int indent = ctx.Indent - 1;
        if (ctx.ShouldIndent && indent > 0)
            newLiteral = newLiteral.WithLeadingTrivia(new string('\t', indent));

        if (ctx.ShouldLineBreak)
            newColon = newColon.WithTrailingTrivia("\r\n");

        gotoLabelStatement.Label.SetLiteral(newLiteral, false);
        gotoLabelStatement.SetColon(newColon, false);
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

        NormalizeAsyncBlockBody(asyncBlock.Body, ctx);
    }

    private void NormalizeAsyncBlockBody(BlockExpression methodDeclarationBody, WhitespaceNormalizeContext ctx)
    {
        string? leadingTrivia = null;
        if (ctx is { ShouldIndent: true, Indent: > 0 })
            leadingTrivia = new string('\t', ctx.Indent);

        SyntaxToken newCurlyOpen = methodDeclarationBody.CurlyOpen.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia("\r\n");
        SyntaxToken newCurlyClose = methodDeclarationBody.CurlyClose.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia("\r\n");

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