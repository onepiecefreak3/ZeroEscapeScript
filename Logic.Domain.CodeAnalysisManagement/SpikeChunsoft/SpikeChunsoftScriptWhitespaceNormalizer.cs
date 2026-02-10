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
        NormalizeNameDeclaration(codeUnit.NameDeclaration, ctx);

        for (var i = 0; i < codeUnit.Members.Count; i++)
        {
            DeclarationSyntax member = codeUnit.Members[i];

            ctx.IsFirstElement = codeUnit.Members[0] == member;
            ctx.ShouldLineBreak = codeUnit.Members[^1] != member;

            switch (member)
            {
                case GlobalVariableDeclarationSyntax globalVariable:
                    var isNextGlobal = i + 1 < codeUnit.Members.Count && codeUnit.Members[i + 1] is GlobalVariableDeclarationSyntax;
                    NormalizeGlobalVariableDeclaration(globalVariable, isNextGlobal);
                    break;

                case MethodDeclarationSyntax methodDeclaration:
                    NormalizeMethodDeclaration(methodDeclaration, ctx);
                    break;
            }
        }
    }

    private void NormalizeNameDeclaration(NameDeclarationSyntax nameDeclaration, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken nameKeyword = nameDeclaration.NameToken.WithLeadingTrivia(null).WithTrailingTrivia(" ");
        SyntaxToken newSemicolon = nameDeclaration.Semicolon.WithLeadingTrivia(null).WithTrailingTrivia("\r\n\r\n");

        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        ctx.IsFirstElement = true;
        NormalizeLiteralExpression(nameDeclaration.Name, ctx);

        nameDeclaration.SetNameToken(nameKeyword, false);
        nameDeclaration.SetSemicolon(newSemicolon, false);
    }

    private void NormalizeGlobalVariableDeclaration(GlobalVariableDeclarationSyntax globalVariable, bool isNextGlobal)
    {
        SyntaxToken globalKeyword = globalVariable.Global.WithLeadingTrivia(null).WithTrailingTrivia(" ");
        SyntaxToken identifier = globalVariable.Identifier.WithNoTrivia();
        SyntaxToken newSemicolon = globalVariable.Semicolon.WithLeadingTrivia(null);

        newSemicolon = newSemicolon.WithTrailingTrivia(isNextGlobal ? "\r\n" : "\r\n\r\n");

        globalVariable.SetGlobal(globalKeyword, false);
        globalVariable.SetIdentifier(identifier, false);
        globalVariable.SetSemicolon(newSemicolon, false);
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

            case PostfixStatementSyntax postfix:
                NormalizePostfixStatement(postfix, ctx);
                break;

            case ExportedGotoLabelStatementSyntax exportedGotoLabel:
                NormalizeExportedGotoLabelStatement(exportedGotoLabel, ctx);
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

            case SwitchStatementSyntax switchStatement:
                NormalizeSwitchStatement(switchStatement, ctx);
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

    private void NormalizePostfixStatement(PostfixStatementSyntax postfixStatement, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken newSemicolon = postfixStatement.Semicolon.WithNoTrivia();

        if (ctx.ShouldLineBreak)
            newSemicolon = newSemicolon.WithTrailingTrivia("\r\n");

        NormalizePostfixExpression(postfixStatement.Postfix, ctx);

        postfixStatement.SetSemicolon(newSemicolon, false);
    }

    private void NormalizeExportedGotoLabelStatement(ExportedGotoLabelStatementSyntax exportedGotoLabelStatement, WhitespaceNormalizeContext ctx)
    {
        string? leadingTrivia = null;
        if (ctx is { ShouldIndent: true, Indent: > 0 })
            leadingTrivia = new string('\t', ctx.Indent - 1);

        SyntaxToken newExport = exportedGotoLabelStatement.Export.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(" ");
        SyntaxToken newColon = exportedGotoLabelStatement.Colon.WithNoTrivia();

        if (ctx.ShouldLineBreak)
            newColon = newColon.WithTrailingTrivia("\r\n");

        exportedGotoLabelStatement.SetExport(newExport, false);
        exportedGotoLabelStatement.SetColon(newColon, false);

        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        ctx.IsFirstElement = true;
        NormalizeLiteralExpression(exportedGotoLabelStatement.Label, ctx);
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
        ctx.IsFirstElement = true;
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
        NormalizeExpression(returnStatement.Expression, ctx);
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

        ctx.ShouldIndent = true;
        NormalizeBlock(ifStatement.Body, ctx, "\r\n");

        ctx.IsFirstElement = true;
        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        NormalizeExpression(ifStatement.Condition, ctx);
    }

    private void NormalizeIfElseStatement(IfElseStatementSyntax ifElseStatement, WhitespaceNormalizeContext ctx)
    {
        string? ifLeadingTrivia = null;
        if (ctx is { ShouldIndent: true, Indent: > 0 })
            ifLeadingTrivia = new string('\t', ctx.Indent);

        string? elseLeadingTrivia = null;
        if (ctx is { Indent: > 0 })
            elseLeadingTrivia = new string('\t', ctx.Indent);

        SyntaxToken ifToken = ifElseStatement.If.WithLeadingTrivia(ifLeadingTrivia).WithTrailingTrivia(" ");
        SyntaxToken elseToken = ifElseStatement.Else.WithLeadingTrivia(elseLeadingTrivia).WithTrailingTrivia("\r\n");
        SyntaxToken parenOpen = ifElseStatement.ParenOpen.WithNoTrivia();
        SyntaxToken parenClose = ifElseStatement.ParenClose.WithNoTrivia().WithTrailingTrivia("\r\n");

        ifElseStatement.SetIf(ifToken, false);
        ifElseStatement.SetElse(elseToken, false);
        ifElseStatement.SetParenOpen(parenOpen, false);
        ifElseStatement.SetParenClose(parenClose, false);

        ctx.ShouldIndent = true;

        NormalizeBlock(ifElseStatement.Body, ctx, "\r\n");
        if (IsInlineBlock(ifElseStatement.ElseBody))
        {
            elseToken = elseToken.WithTrailingTrivia(" ");
            ifElseStatement.SetElse(elseToken, false);

            NormalizeInlineBlock(ifElseStatement.ElseBody, ctx);
        }
        else
        {
            NormalizeBlock(ifElseStatement.ElseBody, ctx, "\r\n");
        }

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

        ctx.ShouldIndent = true;
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

        string? elseLeadingTrivia = null;
        if (ctx is { Indent: > 0 })
            elseLeadingTrivia = new string('\t', ctx.Indent);

        SyntaxToken ifToken = ifNotElseStatement.If.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(" ");
        SyntaxToken notToken = ifNotElseStatement.Not.WithNoTrivia().WithTrailingTrivia(" ");
        SyntaxToken elseToken = ifNotElseStatement.Else.WithLeadingTrivia(elseLeadingTrivia).WithTrailingTrivia("\r\n");
        SyntaxToken parenOpen = ifNotElseStatement.ParenOpen.WithNoTrivia();
        SyntaxToken parenClose = ifNotElseStatement.ParenClose.WithNoTrivia().WithTrailingTrivia("\r\n");

        ifNotElseStatement.SetIf(ifToken, false);
        ifNotElseStatement.SetNot(notToken, false);
        ifNotElseStatement.SetElse(elseToken, false);
        ifNotElseStatement.SetParenOpen(parenOpen, false);
        ifNotElseStatement.SetParenClose(parenClose, false);

        ctx.ShouldIndent = true;

        NormalizeBlock(ifNotElseStatement.Body, ctx, "\r\n");
        if (IsInlineBlock(ifNotElseStatement.ElseBody))
        {
            elseToken = elseToken.WithTrailingTrivia(" ");
            ifNotElseStatement.SetElse(elseToken, false);

            NormalizeInlineBlock(ifNotElseStatement.ElseBody, ctx);
        }
        else
        {
            NormalizeBlock(ifNotElseStatement.ElseBody, ctx, "\r\n");
        }

        ctx.IsFirstElement = true;
        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        NormalizeExpression(ifNotElseStatement.Condition, ctx);
    }

    private void NormalizeSwitchStatement(SwitchStatementSyntax switchStatement, WhitespaceNormalizeContext ctx)
    {
        string? leadingTrivia = null;
        if (ctx is { ShouldIndent: true, Indent: > 0 })
            leadingTrivia = new string('\t', ctx.Indent);

        SyntaxToken switchToken = switchStatement.Switch.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(" ");
        SyntaxToken parenOpen = switchStatement.ParenOpen.WithNoTrivia();
        SyntaxToken parenClose = switchStatement.ParenClose.WithNoTrivia().WithTrailingTrivia("\r\n");
        SyntaxToken curlyOpen = switchStatement.CurlyOpen.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia("\r\n");
        SyntaxToken curlyClose = switchStatement.CurlyClose.WithLeadingTrivia(leadingTrivia);

        if (ctx.ShouldLineBreak)
            curlyClose = curlyClose.WithTrailingTrivia("\r\n");

        switchStatement.SetSwitch(switchToken, false);
        switchStatement.SetParenOpen(parenOpen, false);
        switchStatement.SetParenClose(parenClose, false);
        switchStatement.SetCurlyOpen(curlyOpen, false);
        switchStatement.SetCurlyClose(curlyClose, false);

        var caseCtx = ctx;
        caseCtx.Indent++;
        foreach (CaseStatementSyntax @case in switchStatement.Cases)
        {
            caseCtx.IsFirstElement = switchStatement.Cases[0] == @case;
            caseCtx.ShouldLineBreak = true;
            caseCtx.ShouldIndent = true;
            NormalizeCaseStatement(@case, caseCtx);
        }

        ctx.IsFirstElement = true;
        ctx.ShouldIndent = false;
        ctx.ShouldLineBreak = false;
        NormalizeExpression(switchStatement.Expression, ctx);
    }

    private void NormalizeCaseStatement(CaseStatementSyntax caseStatement, WhitespaceNormalizeContext ctx)
    {
        string? leadingTrivia = null;
        if (ctx is { ShouldIndent: true, Indent: > 0 })
            leadingTrivia = new string('\t', ctx.Indent);

        SyntaxToken caseToken = caseStatement.Case.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(" ");
        SyntaxToken colon = caseStatement.Colon.WithNoTrivia().WithTrailingTrivia("\r\n");

        caseStatement.SetCase(caseToken, false);
        caseStatement.SetColon(colon, false);

        var labelCtx = ctx;
        labelCtx.IsFirstElement = true;
        labelCtx.ShouldIndent = false;
        labelCtx.ShouldLineBreak = false;
        NormalizeExpression(caseStatement.Label, labelCtx);

        var bodyCtx = ctx;
        bodyCtx.Indent++;
        foreach (StatementSyntax statement in caseStatement.Statements)
        {
            bodyCtx.IsFirstElement = caseStatement.Statements[0] == statement;
            bodyCtx.ShouldLineBreak = true;
            bodyCtx.ShouldIndent = true;
            NormalizeStatement(statement, bodyCtx);
        }
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
        if (IsInlineBlock(methodDeclarationBody))
        {
            NormalizeInlineBlock(methodDeclarationBody, ctx);
            return;
        }

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

    private void NormalizeInlineBlock(BlockExpression blockExpression, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken curlyOpen = blockExpression.CurlyOpen.WithNoTrivia();
        SyntaxToken curlyClose = blockExpression.CurlyClose.WithNoTrivia();

        blockExpression.SetCurlyOpen(curlyOpen, false);
        blockExpression.SetCurlyClose(curlyClose, false);

        foreach (StatementSyntax expression in blockExpression.Statements)
        {
            ctx.IsFirstElement = blockExpression.Statements[0] == expression;
            ctx.ShouldLineBreak = false;
            ctx.ShouldIndent = false;

            NormalizeStatement(expression, ctx);
        }
    }

    private static bool IsInlineBlock(BlockExpression blockExpression)
    {
        return string.IsNullOrEmpty(blockExpression.CurlyOpen.Text) &&
               string.IsNullOrEmpty(blockExpression.CurlyClose.Text);
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
            case SimpleMemberAccessExpressionSyntax memberAccess:
                NormalizeSimpleMemberAccessExpression(memberAccess, ctx);
                break;

            case QualifiedMemberAccessExpressionSyntax memberAccess:
                NormalizeQualifiedMemberAccessExpression(memberAccess, ctx);
                break;

            case CompoundMemberAccessExpressionSyntax memberAccess:
                NormalizeCompoundMemberAccessExpression(memberAccess, ctx);
                break;

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

    private void NormalizeSimpleMemberAccessExpression(SimpleMemberAccessExpressionSyntax memberAccess, WhitespaceNormalizeContext ctx)
    {
        string? leadingTrivia = null;
        if (ctx is { ShouldIndent: true, Indent: > 0 })
            leadingTrivia = new string('\t', ctx.Indent);

        SyntaxToken identifier = memberAccess.Identifier.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(null);

        memberAccess.SetIdentifier(identifier, false);
    }

    private void NormalizeQualifiedMemberAccessExpression(QualifiedMemberAccessExpressionSyntax memberAccess, WhitespaceNormalizeContext ctx)
    {
        string? leadingTrivia = null;
        if (ctx is { ShouldIndent: true, Indent: > 0 })
            leadingTrivia = new string('\t', ctx.Indent);

        SyntaxToken nameSpace = memberAccess.NameSpace.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(null);
        SyntaxToken operatorToken = memberAccess.Operator.WithNoTrivia();
        SyntaxToken identifier = memberAccess.Identifier.WithNoTrivia();

        memberAccess.SetNameSpace(nameSpace, false);
        memberAccess.SetOperator(operatorToken, false);
        memberAccess.SetIdentifier(identifier, false);
    }

    private void NormalizeCompoundMemberAccessExpression(CompoundMemberAccessExpressionSyntax memberAccess, WhitespaceNormalizeContext ctx)
    {
        SyntaxToken operatorToken = memberAccess.Operator.WithNoTrivia();
        SyntaxToken identifier = memberAccess.Identifier.WithNoTrivia();

        memberAccess.SetOperator(operatorToken, false);
        memberAccess.SetIdentifier(identifier, false);

        NormalizeParenthesizedExpression(memberAccess.Eval, ctx);
    }

    private void NormalizeParenthesizedExpression(ParenthesizedExpressionSyntax parens, WhitespaceNormalizeContext ctx)
    {
        string? leadingTrivia = null;
        if (ctx is { ShouldIndent: true, Indent: > 0 })
            leadingTrivia = new string('\t', ctx.Indent);

        SyntaxToken parenOpen = parens.ParenOpen.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(null);
        SyntaxToken parenClose = parens.ParenClose.WithNoTrivia();

        parens.SetParenOpen(parenOpen, false);
        parens.SetParenClose(parenClose, false);

        ctx.ShouldIndent = false;
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
        string? leadingTrivia = null;
        if (!ctx.IsFirstElement)
            leadingTrivia += " ";

        SyntaxToken operation = unaryExpression.Operation.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(null);

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
        SyntaxToken newOperator = assignmentExpression.Operator.WithLeadingTrivia(" ").WithTrailingTrivia(" ");

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
        NormalizeExpression(invocation.Name, ctx);
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