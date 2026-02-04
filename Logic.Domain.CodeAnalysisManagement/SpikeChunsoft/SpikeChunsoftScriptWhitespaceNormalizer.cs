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

        ctx.ShouldLineBreak = false;
        NormalizeMethodDeclarationParameters(methodDeclaration.Parameters, ctx);

        ctx.ShouldLineBreak = shouldLineBreak;
        NormalizeMethodDeclarationBody(methodDeclaration.Body, ctx);

        ctx.IsFirstElement = true;
        ctx.ShouldLineBreak = false;
        NormalizeLiteralExpression(methodDeclaration.Name, ctx);
    }

    private void NormalizeMethodDeclarationParameters(MethodDeclarationParametersSyntax methodDeclarationParameters, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newParenOpen = methodDeclarationParameters.ParenOpen.WithNoTrivia();
        SyntaxToken newParenClose = methodDeclarationParameters.ParenClose.WithNoTrivia();

        if (ctx.ShouldLineBreak)
            newParenClose = newParenClose.WithTrailingTrivia("\r\n");

        methodDeclarationParameters.SetParenOpen(newParenOpen, false);
        NormalizeMethodDeclarationParameterList(methodDeclarationParameters.Parameters, ctx);
        methodDeclarationParameters.SetParenClose(newParenClose, false);
    }

    private void NormalizeMethodDeclarationParameterList(CommaSeparatedSyntaxList<LiteralExpressionSyntax>? list, WhitespaceNormalizeContext ctx)
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
        SyntaxToken newCurlyOpen = methodDeclarationBody.CurlyOpen;
        SyntaxToken newCurlyClose = methodDeclarationBody.CurlyClose.WithNoTrivia();

        if (methodDeclarationBody.Statements.Count <= 0)
            newCurlyOpen = newCurlyOpen.WithLeadingTrivia(" ").WithTrailingTrivia(" ");
        else
            newCurlyOpen = newCurlyOpen.WithLeadingTrivia("\r\n").WithTrailingTrivia("\r\n");

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
            case AssignmentStatementSyntax assignment:
                NormalizeAssignmentStatement(assignment, ctx);
                break;

            case NativeMethodInvocationStatementSyntax invocation:
                NormalizeNativeMethodInvocationStatement(invocation, ctx);
                break;

            case GotoLabelStatementSyntax gotoLabel:
                NormalizeGotoLabelStatement(gotoLabel, ctx);
                break;

            case GotoStatementSyntax gotoStatement:
                NormalizeGotoStatement(gotoStatement, ctx);
                break;

            case AsyncBlockStatement asyncStatement:
                NormalizeAsyncBlockStatement(asyncStatement, ctx);
                break;

            case IfStatementSyntax ifStatement:
                NormalizeIfStatement(ifStatement, ctx);
                break;

            case IfElseStatementSyntax ifElseStatement:
                NormalizeIfElseStatement(ifElseStatement, ctx);
                break;

            case IfNotStatementSyntax ifNotStatement:
                NormalizeIfNotStatement(ifNotStatement, ctx);
                break;

            case IfNotElseStatementSyntax ifNotElseStatement:
                NormalizeIfNotElseStatement(ifNotElseStatement, ctx);
                break;

            case DoWhileStatementSyntax doWhileStatement:
                NormalizeDoWhileStatement(doWhileStatement, ctx);
                break;

            case DoWhileNotStatementSyntax doWhileNotStatement:
                NormalizeDoWhileNotStatement(doWhileNotStatement, ctx);
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

    private void NormalizeAssignmentStatement(AssignmentStatementSyntax assignmentStatement, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newSemicolon = assignmentStatement.Semicolon.WithNoTrivia();

        if (ctx.ShouldLineBreak)
            newSemicolon = newSemicolon.WithTrailingTrivia("\r\n");

        NormalizeAssignmentExpression(assignmentStatement.Assignment, ctx);

        assignmentStatement.SetSemicolon(newSemicolon, false);
    }

    private void NormalizeNativeMethodInvocationStatement(NativeMethodInvocationStatementSyntax invocationStatement, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newSemicolon = invocationStatement.Semicolon.WithNoTrivia();

        if (ctx.ShouldLineBreak)
            newSemicolon = newSemicolon.WithTrailingTrivia("\r\n");

        NormalizeNativeMethodInvocationExpression(invocationStatement.Method, ctx);

        invocationStatement.SetSemicolon(newSemicolon, false);
    }

    private void NormalizeGotoLabelStatement(GotoLabelStatementSyntax gotoLabelStatement, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newColon = gotoLabelStatement.Colon.WithNoTrivia();

        if (ctx.ShouldLineBreak)
            newColon = newColon.WithTrailingTrivia("\r\n");

        gotoLabelStatement.SetColon(newColon, false);

        ctx.Indent--;
        ctx.ShouldIndent = true;
        ctx.ShouldLineBreak = false;
        NormalizeLiteralExpression(gotoLabelStatement.Label, ctx);
    }

    private void NormalizeGotoStatement(GotoStatementSyntax gotoStatement, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newGotoKeyword = gotoStatement.Goto.WithTrailingTrivia(" ");
        SyntaxToken newSemicolon = gotoStatement.Semicolon.WithNoTrivia();

        if (ctx is { ShouldIndent: true, Indent: > 0 })
            newGotoKeyword = newGotoKeyword.WithLeadingTrivia(new string('\t', ctx.Indent));

        if (ctx.ShouldLineBreak)
            newSemicolon = newSemicolon.WithTrailingTrivia("\r\n");

        gotoStatement.SetGoto(newGotoKeyword, false);
        gotoStatement.SetSemicolon(newSemicolon, false);
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
        NormalizeExpression(ifStatement.Condition, ctx);
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
        NormalizeExpression(ifElseStatement.Condition, ctx);
    }

    private void NormalizeIfNotStatement(IfNotStatementSyntax ifNotStatement, WhitespaceNormalizeContext ctx)
    {
        string? leadingTrivia = null;
        if (ctx is { ShouldIndent: true, Indent: > 0 })
            leadingTrivia = new string('\t', ctx.Indent);

        SyntaxToken ifToken = ifNotStatement.If.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(" ");
        SyntaxToken notToken = ifNotStatement.Not.WithNoTrivia().WithTrailingTrivia(" ");
        SyntaxToken parenOpen = ifNotStatement.ParenOpen.WithNoTrivia();
        SyntaxToken parenClose = ifNotStatement.ParenClose.WithNoTrivia().WithTrailingTrivia("\r\n");

        ifNotStatement.SetIf(ifToken, false);
        ifNotStatement.SetNot(notToken, false);
        ifNotStatement.SetParenOpen(parenOpen, false);
        ifNotStatement.SetParenClose(parenClose, false);

        NormalizeBlock(ifNotStatement.Body, ctx, "\r\n");

        ctx.IsFirstElement = true;
        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        NormalizeExpression(ifNotStatement.Condition, ctx);
    }

    private void NormalizeIfNotElseStatement(IfNotElseStatementSyntax ifNotElseStatement, WhitespaceNormalizeContext ctx)
    {
        string? leadingTrivia = null;
        if (ctx is { ShouldIndent: true, Indent: > 0 })
            leadingTrivia = new string('\t', ctx.Indent);

        SyntaxToken ifToken = ifNotElseStatement.If.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(" ");
        SyntaxToken notToken = ifNotElseStatement.Not.WithNoTrivia().WithTrailingTrivia(" ");
        SyntaxToken elseToken = ifNotElseStatement.Else.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia("\r\n");
        SyntaxToken parenOpen = ifNotElseStatement.ParenOpen.WithNoTrivia();
        SyntaxToken parenClose = ifNotElseStatement.ParenClose.WithNoTrivia().WithTrailingTrivia("\r\n");

        ifNotElseStatement.SetIf(ifToken, false);
        ifNotElseStatement.SetNot(notToken, false);
        ifNotElseStatement.SetElse(elseToken, false);
        ifNotElseStatement.SetParenOpen(parenOpen, false);
        ifNotElseStatement.SetParenClose(parenClose, false);

        NormalizeBlock(ifNotElseStatement.Body, ctx, "\r\n");
        NormalizeBlock(ifNotElseStatement.ElseBody, ctx, "\r\n");

        ctx.IsFirstElement = true;
        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        NormalizeExpression(ifNotElseStatement.Condition, ctx);
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
        NormalizeExpression(doWhileStatement.Condition, ctx);
    }

    private void NormalizeDoWhileNotStatement(DoWhileNotStatementSyntax doWhileNotStatement, WhitespaceNormalizeContext ctx)
    {
        string? leadingTrivia = null;
        if (ctx is { ShouldIndent: true, Indent: > 0 })
            leadingTrivia = new string('\t', ctx.Indent);

        SyntaxToken doToken = doWhileNotStatement.Do.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia("\r\n");
        SyntaxToken whileToken = doWhileNotStatement.While.WithNoTrivia().WithTrailingTrivia(" ");
        SyntaxToken notToken = doWhileNotStatement.Not.WithNoTrivia().WithTrailingTrivia(" ");
        SyntaxToken parenOpen = doWhileNotStatement.ParenOpen.WithNoTrivia();
        SyntaxToken parenClose = doWhileNotStatement.ParenClose.WithNoTrivia();
        SyntaxToken semicolon = doWhileNotStatement.Semicolon.WithNoTrivia();

        if (ctx.ShouldLineBreak)
            semicolon = semicolon.WithTrailingTrivia("\r\n");

        doWhileNotStatement.SetWhile(whileToken, false);
        doWhileNotStatement.SetNot(notToken, false);
        doWhileNotStatement.SetParenOpen(parenOpen, false);
        doWhileNotStatement.SetParenClose(parenClose, false);
        doWhileNotStatement.SetDo(doToken, false);
        doWhileNotStatement.SetSemicolon(semicolon, false);

        NormalizeBlock(doWhileNotStatement.Body, ctx, " ");

        ctx.IsFirstElement = true;
        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        NormalizeExpression(doWhileNotStatement.Condition, ctx);
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

        invocation.SetSemicolon(newSemicolon, false);

        ctx.ShouldIndent = true;
        ctx.ShouldLineBreak = false;
        NormalizeMethodInvocationExpression(invocation.Method, ctx);
    }

    private void NormalizeMethodInvocationExpression(MethodInvocationExpressionSyntax invocation, WhitespaceNormalizeContext ctx)
    {
        NormalizeName(invocation.Name, ctx);
        NormalizeMethodInvocationParameters(invocation.Parameters, ctx);
    }

    private void NormalizeMethodInvocationParameters(MethodInvocationParametersSyntax invocationParameters, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken parenOpen = invocationParameters.ParenOpen.WithNoTrivia();
        SyntaxToken parenClose = invocationParameters.ParenClose.WithNoTrivia();

        invocationParameters.SetParenOpen(parenOpen, false);
        invocationParameters.SetParenClose(parenClose, false);

        ctx.ShouldIndent = false;
        NormalizeLiteralExpressions(invocationParameters.ParameterList, ctx);
    }

    private void NormalizeExpression(ExpressionSyntax expression, WhitespaceNormalizeContext ctx)
    {
        switch (expression)
        {
            case ParenthesizedExpressionSyntax parens:
                NormalizeParenthesizedExpression(parens, ctx);
                break;

            case BinaryExpressionSyntax binary:
                NormalizeBinaryExpression(binary, ctx);
                break;

            case LogicalExpressionSyntax logical:
                NormalizeLogicalExpression(logical, ctx);
                break;

            case UnaryExpressionSyntax unary:
                NormalizeUnaryExpression(unary, ctx);
                break;

            case LiteralExpressionSyntax literalExpression:
                NormalizeLiteralExpression(literalExpression, ctx);
                break;

            case ArrayIndexExpressionSyntax arrayIndex:
                NormalizeArrayIndexExpression(arrayIndex, ctx);
                break;

            case PostfixExpressionSyntax postfix:
                NormalizePostfixExpression(postfix, ctx);
                break;

            case AssignmentExpressionSyntax assignment:
                NormalizeAssignmentExpression(assignment, ctx);
                break;

            case NativeMethodInvocationExpressionSyntax invocation:
                NormalizeNativeMethodInvocationExpression(invocation, ctx);
                break;
        }
    }

    private void NormalizeParenthesizedExpression(ParenthesizedExpressionSyntax parens, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken parenOpen = parens.ParenOpen.WithNoTrivia();
        SyntaxToken parenClose = parens.ParenClose.WithNoTrivia();

        parens.SetParenOpen(parenOpen, false);
        parens.SetParenClose(parenClose, false);

        NormalizeExpression(parens.Expression, ctx);
    }

    private void NormalizeBinaryExpression(BinaryExpressionSyntax binary, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken operation = binary.Operation.WithLeadingTrivia(" ").WithTrailingTrivia(" ");

        binary.SetOperation(operation, false);

        NormalizeExpression(binary.Left, ctx);
        NormalizeExpression(binary.Right, ctx);
    }

    private void NormalizeLogicalExpression(LogicalExpressionSyntax logical, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken operation = logical.Operation.WithLeadingTrivia(" ").WithTrailingTrivia(" ");

        logical.SetOperation(operation, false);

        NormalizeExpression(logical.Left, ctx);
        NormalizeExpression(logical.Right, ctx);
    }

    private void NormalizeUnaryExpression(UnaryExpressionSyntax unaryExpression, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken operation = unaryExpression.Operation.WithNoTrivia();

        ctx.IsFirstElement = true;
        NormalizeExpression(unaryExpression.Expression, ctx);

        unaryExpression.SetOperation(operation, false);
    }

    private void NormalizeArrayIndexExpression(ArrayIndexExpressionSyntax arrayIndex, WhitespaceNormalizeContext ctx)
    {
        NormalizeExpression(arrayIndex.Value, ctx);
        foreach (var index in arrayIndex.Indexer)
            NormalizeArrayIndexExpression(index, ctx);
    }

    private void NormalizeArrayIndexExpression(ArrayIndexerExpressionSyntax index, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken bracketOpen = index.BracketOpen.WithNoTrivia();
        SyntaxToken bracketClose = index.BracketClose.WithNoTrivia();

        ctx.IsFirstElement = true;
        ctx.ShouldIndent = false;
        NormalizeExpression(index.Index, ctx);

        index.SetBracketOpen(bracketOpen, false);
        index.SetBracketClose(bracketClose, false);
    }

    private void NormalizePostfixExpression(PostfixExpressionSyntax postfixExpression, WhitespaceNormalizeContext ctx)
    {
        ctx.IsFirstElement = true;
        ctx.ShouldLineBreak = false;
        NormalizeExpression(postfixExpression.Expression, ctx);
    }

    private void NormalizeAssignmentExpression(AssignmentExpressionSyntax assignmentExpression, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newOperator = assignmentExpression.Operation.WithLeadingTrivia(" ").WithTrailingTrivia(" ");

        ctx.ShouldIndent = true;
        ctx.ShouldLineBreak = false;
        ctx.IsFirstElement = true;
        NormalizeExpression(assignmentExpression.Left, ctx);

        ctx.ShouldIndent = false;
        NormalizeExpression(assignmentExpression.Right, ctx);

        assignmentExpression.SetOperator(newOperator, false);
    }

    private void NormalizeNativeMethodInvocationExpression(NativeMethodInvocationExpressionSyntax invocation, WhitespaceNormalizeContext ctx)
    {
        NormalizeNativeMethodInvocationParameters(invocation.Parameters, ctx);

        ctx.IsFirstElement = true;
        ctx.ShouldLineBreak = false;
        NormalizeLiteralExpression(invocation.Name, ctx);
    }

    private void NormalizeNativeMethodInvocationParameters(NativeMethodInvocationParametersSyntax invocationParameters, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken parenOpen = invocationParameters.ParenOpen.WithNoTrivia();
        SyntaxToken parenClose = invocationParameters.ParenClose.WithNoTrivia();

        invocationParameters.SetParenOpen(parenOpen, false);
        invocationParameters.SetParenClose(parenClose, false);

        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        NormalizeExpressions(invocationParameters.ParameterList, ctx);
    }

    private void NormalizeExpressions(CommaSeparatedSyntaxList<ExpressionSyntax>? valueList, WhitespaceNormalizeContext ctx)
    {
        if (valueList == null)
            return;

        foreach (ExpressionSyntax value in valueList.Elements)
        {
            ctx.IsFirstElement = valueList.Elements[0] == value;
            NormalizeExpression(value, ctx);
        }
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