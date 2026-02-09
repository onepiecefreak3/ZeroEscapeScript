using System.Text;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;
using Logic.Domain.CodeAnalysisManagement.Contract.SpikeChunsoft;

namespace Logic.Domain.CodeAnalysisManagement.SpikeChunsoft;

internal class SpikeChunsoftScriptComposer(ISpikeChunsoftSyntaxFactory syntaxFactory) : ISpikeChunsoftScriptComposer
{
    public string ComposeCodeUnit(CodeUnitSyntax codeUnit)
    {
        var sb = new StringBuilder();

        ComposeCodeUnit(codeUnit, sb);

        return sb.ToString();
    }

    private void ComposeCodeUnit(CodeUnitSyntax codeUnit, StringBuilder sb)
    {
        ComposeNameDeclaration(codeUnit.NameDeclaration, sb);

        foreach (MethodDeclarationSyntax methodDeclaration in codeUnit.MethodDeclarations)
            ComposeMethodDeclaration(methodDeclaration, sb);
    }

    private void ComposeNameDeclaration(NameDeclarationSyntax nameDeclaration, StringBuilder sb)
    {
        ComposeSyntaxToken(nameDeclaration.NameToken, sb);
        ComposeLiteralExpression(nameDeclaration.Name, sb);
        ComposeSyntaxToken(nameDeclaration.Semicolon, sb);
    }

    private void ComposeMethodDeclaration(MethodDeclarationSyntax methodDeclaration, StringBuilder sb)
    {
        ComposeLiteralExpression(methodDeclaration.Name, sb);
        ComposeMethodDeclarationParameters(methodDeclaration.Parameters, sb);
        ComposeMethodDeclarationBody(methodDeclaration.Body, sb);
    }

    private void ComposeMethodDeclarationParameters(MethodDeclarationParametersSyntax methodDeclarationParameters, StringBuilder sb)
    {
        ComposeSyntaxToken(methodDeclarationParameters.ParenOpen, sb);
        ComposeLiteralExpressions(methodDeclarationParameters.Parameters, sb);
        ComposeSyntaxToken(methodDeclarationParameters.ParenClose, sb);
    }

    private void ComposeLiteralExpressions(CommaSeparatedSyntaxList<LiteralExpressionSyntax>? valueList, StringBuilder sb)
    {
        if (valueList == null || valueList.Elements.Count <= 0)
            return;

        for (var i = 0; i < valueList.Elements.Count - 1; i++)
        {
            ComposeLiteralExpression(valueList.Elements[i], sb);
            ComposeSyntaxToken(syntaxFactory.Token(SyntaxTokenKind.Comma), sb);
        }

        ComposeLiteralExpression(valueList.Elements[^1], sb);
    }

    private void ComposeMethodDeclarationBody(BlockExpression methodDeclarationBody, StringBuilder sb)
    {
        ComposeSyntaxToken(methodDeclarationBody.CurlyOpen, sb);

        foreach (StatementSyntax expression in methodDeclarationBody.Statements)
            ComposeStatement(expression, sb);

        ComposeSyntaxToken(methodDeclarationBody.CurlyClose, sb);
    }

    private void ComposeStatement(StatementSyntax statement, StringBuilder sb)
    {
        switch (statement)
        {
            case AssignmentStatementSyntax assignment:
                ComposeAssignmentStatement(assignment, sb);
                break;

            case NativeMethodInvocationStatementSyntax invocation:
                ComposeNativeMethodInvocationStatement(invocation, sb);
                break;

            case PostfixStatementSyntax postfix:
                ComposePostfixStatement(postfix, sb);
                break;

            case ExportedGotoLabelStatementSyntax exportGotoLabel:
                ComposeExportedGotoLabelStatement(exportGotoLabel, sb);
                break;

            case GotoLabelStatementSyntax gotoLabel:
                ComposeGotoLabelStatement(gotoLabel, sb);
                break;

            case GotoStatementSyntax gotoStatement:
                ComposeGotoStatement(gotoStatement, sb);
                break;

            case AsyncBlockStatement asyncStatement:
                ComposeAsyncBlockStatement(asyncStatement, sb);
                break;

            case IfStatementSyntax ifStatement:
                ComposeIfStatement(ifStatement, sb);
                break;

            case IfElseStatementSyntax ifElseStatement:
                ComposeIfElseStatement(ifElseStatement, sb);
                break;

            case IfNotStatementSyntax ifNotStatement:
                ComposeIfNotStatement(ifNotStatement, sb);
                break;

            case IfNotElseStatementSyntax ifNotElseStatement:
                ComposeIfNotElseStatement(ifNotElseStatement, sb);
                break;

            case SwitchStatementSyntax switchStatement:
                ComposeSwitchStatement(switchStatement, sb);
                break;

            case DoWhileStatementSyntax doWhileStatement:
                ComposeDoWhileStatement(doWhileStatement, sb);
                break;

            case DoWhileNotStatementSyntax doWhileNotStatement:
                ComposeDoWhileNotStatement(doWhileNotStatement, sb);
                break;

            case BreakStatementSyntax breakStatement:
                ComposeBreakStatement(breakStatement, sb);
                break;

            case ContinueStatementSyntax continueStatement:
                ComposeContinueStatement(continueStatement, sb);
                break;

            case ReturnStatementSyntax returnStatement:
                ComposeReturnStatement(returnStatement, sb);
                break;

            case MethodInvocationStatementSyntax methodInvocationStatement:
                ComposeMethodInvocationStatement(methodInvocationStatement, sb);
                break;
        }
    }

    private void ComposeAssignmentStatement(AssignmentStatementSyntax assignment, StringBuilder sb)
    {
        ComposeAssignmentExpression(assignment.Assignment, sb);
        ComposeSyntaxToken(assignment.Semicolon, sb);
    }

    private void ComposeNativeMethodInvocationStatement(NativeMethodInvocationStatementSyntax invocation, StringBuilder sb)
    {
        ComposeNativeMethodInvocationExpression(invocation.Method, sb);
        ComposeSyntaxToken(invocation.Semicolon, sb);
    }

    private void ComposePostfixStatement(PostfixStatementSyntax postfix, StringBuilder sb)
    {
        ComposePostfixExpression(postfix.Postfix, sb);
        ComposeSyntaxToken(postfix.Semicolon, sb);
    }

    private void ComposeExportedGotoLabelStatement(ExportedGotoLabelStatementSyntax exportGotoLabelStatement, StringBuilder sb)
    {
        ComposeSyntaxToken(exportGotoLabelStatement.Export, sb);
        ComposeLiteralExpression(exportGotoLabelStatement.Label, sb);
        ComposeSyntaxToken(exportGotoLabelStatement.Colon, sb);
    }

    private void ComposeGotoLabelStatement(GotoLabelStatementSyntax gotoLabelStatement, StringBuilder sb)
    {
        ComposeLiteralExpression(gotoLabelStatement.Label, sb);
        ComposeSyntaxToken(gotoLabelStatement.Colon, sb);
    }

    private void ComposeGotoStatement(GotoStatementSyntax gotoStatement, StringBuilder sb)
    {
        ComposeSyntaxToken(gotoStatement.Goto, sb);
        ComposeLiteralExpression(gotoStatement.Label, sb);
        ComposeSyntaxToken(gotoStatement.Semicolon, sb);
    }

    private void ComposeAsyncBlockStatement(AsyncBlockStatement asyncStatement, StringBuilder sb)
    {
        ComposeSyntaxToken(asyncStatement.Async, sb);
        ComposeBlock(asyncStatement.Body, sb);
    }

    private void ComposeBlock(BlockExpression blockExpression, StringBuilder sb)
    {
        if (IsInlineBlock(blockExpression))
        {
            foreach (StatementSyntax expression in blockExpression.Statements)
                ComposeStatement(expression, sb);
            return;
        }

        ComposeSyntaxToken(blockExpression.CurlyOpen, sb);

        foreach (StatementSyntax expression in blockExpression.Statements)
            ComposeStatement(expression, sb);

        ComposeSyntaxToken(blockExpression.CurlyClose, sb);
    }

    private static bool IsInlineBlock(BlockExpression blockExpression)
    {
        return string.IsNullOrEmpty(blockExpression.CurlyOpen.Text) &&
               string.IsNullOrEmpty(blockExpression.CurlyClose.Text);
    }

    private void ComposeIfStatement(IfStatementSyntax ifStatement, StringBuilder sb)
    {
        ComposeSyntaxToken(ifStatement.If, sb);
        ComposeSyntaxToken(ifStatement.ParenOpen, sb);
        ComposeExpression(ifStatement.Condition, sb);
        ComposeSyntaxToken(ifStatement.ParenClose, sb);
        ComposeBlock(ifStatement.Body, sb);
    }

    private void ComposeIfElseStatement(IfElseStatementSyntax ifElseStatement, StringBuilder sb)
    {
        ComposeSyntaxToken(ifElseStatement.If, sb);
        ComposeSyntaxToken(ifElseStatement.ParenOpen, sb);
        ComposeExpression(ifElseStatement.Condition, sb);
        ComposeSyntaxToken(ifElseStatement.ParenClose, sb);
        ComposeBlock(ifElseStatement.Body, sb);
        ComposeSyntaxToken(ifElseStatement.Else, sb);
        ComposeBlock(ifElseStatement.ElseBody, sb);
    }

    private void ComposeIfNotStatement(IfNotStatementSyntax ifNotStatement, StringBuilder sb)
    {
        ComposeSyntaxToken(ifNotStatement.If, sb);
        ComposeSyntaxToken(ifNotStatement.Not, sb);
        ComposeSyntaxToken(ifNotStatement.ParenOpen, sb);
        ComposeExpression(ifNotStatement.Condition, sb);
        ComposeSyntaxToken(ifNotStatement.ParenClose, sb);
        ComposeBlock(ifNotStatement.Body, sb);
    }

    private void ComposeIfNotElseStatement(IfNotElseStatementSyntax ifNotElseStatement, StringBuilder sb)
    {
        ComposeSyntaxToken(ifNotElseStatement.If, sb);
        ComposeSyntaxToken(ifNotElseStatement.Not, sb);
        ComposeSyntaxToken(ifNotElseStatement.ParenOpen, sb);
        ComposeExpression(ifNotElseStatement.Condition, sb);
        ComposeSyntaxToken(ifNotElseStatement.ParenClose, sb);
        ComposeBlock(ifNotElseStatement.Body, sb);
        ComposeSyntaxToken(ifNotElseStatement.Else, sb);
        ComposeBlock(ifNotElseStatement.ElseBody, sb);
    }

    private void ComposeSwitchStatement(SwitchStatementSyntax switchStatement, StringBuilder sb)
    {
        ComposeSyntaxToken(switchStatement.Switch, sb);
        ComposeSyntaxToken(switchStatement.ParenOpen, sb);
        ComposeExpression(switchStatement.Expression, sb);
        ComposeSyntaxToken(switchStatement.ParenClose, sb);
        ComposeSyntaxToken(switchStatement.CurlyOpen, sb);
        foreach (CaseStatementSyntax @case in switchStatement.Cases)
            ComposeCaseStatement(@case, sb);
        ComposeSyntaxToken(switchStatement.CurlyClose, sb);
    }

    private void ComposeCaseStatement(CaseStatementSyntax caseStatement, StringBuilder sb)
    {
        ComposeSyntaxToken(caseStatement.Case, sb);
        ComposeExpression(caseStatement.Label, sb);
        ComposeSyntaxToken(caseStatement.Colon, sb);
        foreach (StatementSyntax statement in caseStatement.Statements)
            ComposeStatement(statement, sb);
    }

    private void ComposeDoWhileStatement(DoWhileStatementSyntax doWhileStatement, StringBuilder sb)
    {
        ComposeSyntaxToken(doWhileStatement.Do, sb);
        ComposeBlock(doWhileStatement.Body, sb);
        ComposeSyntaxToken(doWhileStatement.While, sb);
        ComposeSyntaxToken(doWhileStatement.ParenOpen, sb);
        ComposeExpression(doWhileStatement.Condition, sb);
        ComposeSyntaxToken(doWhileStatement.ParenClose, sb);
        ComposeSyntaxToken(doWhileStatement.Semicolon, sb);
    }

    private void ComposeDoWhileNotStatement(DoWhileNotStatementSyntax doWhileNotStatement, StringBuilder sb)
    {
        ComposeSyntaxToken(doWhileNotStatement.Do, sb);
        ComposeBlock(doWhileNotStatement.Body, sb);
        ComposeSyntaxToken(doWhileNotStatement.While, sb);
        ComposeSyntaxToken(doWhileNotStatement.Not, sb);
        ComposeSyntaxToken(doWhileNotStatement.ParenOpen, sb);
        ComposeExpression(doWhileNotStatement.Condition, sb);
        ComposeSyntaxToken(doWhileNotStatement.ParenClose, sb);
        ComposeSyntaxToken(doWhileNotStatement.Semicolon, sb);
    }

    private void ComposeBreakStatement(BreakStatementSyntax breakStatement, StringBuilder sb)
    {
        ComposeSyntaxToken(breakStatement.Break, sb);
        ComposeSyntaxToken(breakStatement.Semicolon, sb);
    }

    private void ComposeContinueStatement(ContinueStatementSyntax continueStatement, StringBuilder sb)
    {
        ComposeSyntaxToken(continueStatement.Continue, sb);
        ComposeSyntaxToken(continueStatement.Semicolon, sb);
    }

    private void ComposeReturnStatement(ReturnStatementSyntax returnStatement, StringBuilder sb)
    {
        ComposeSyntaxToken(returnStatement.Return, sb);
        if (returnStatement.Expression != null)
            ComposeExpression(returnStatement.Expression, sb);
        ComposeSyntaxToken(returnStatement.Semicolon, sb);
    }

    private void ComposeMethodInvocationStatement(MethodInvocationStatementSyntax invocation, StringBuilder sb)
    {
        ComposeMethodInvocationExpression(invocation.Method, sb);
        ComposeSyntaxToken(invocation.Semicolon, sb);
    }

    private void ComposeMethodInvocationExpression(MethodInvocationExpressionSyntax invocation, StringBuilder sb)
    {
        ComposeName(invocation.Name, sb);
        ComposeMethodInvocationParameters(invocation.Parameters, sb);
    }

    private void ComposeMethodInvocationParameters(MethodInvocationParametersSyntax invocationParameters, StringBuilder sb)
    {
        ComposeSyntaxToken(invocationParameters.ParenOpen, sb);
        ComposeLiteralExpressions(invocationParameters.ParameterList, sb);
        ComposeSyntaxToken(invocationParameters.ParenClose, sb);
    }

    private void ComposeExpression(ExpressionSyntax expression, StringBuilder sb)
    {
        switch (expression)
        {
            case SimpleMemberAccessExpressionSyntax memberAccess:
                ComposeSimpleMemberAccessExpression(memberAccess, sb);
                break;

            case QualifiedMemberAccessExpressionSyntax memberAccess:
                ComposeQualifiedMemberAccessExpression(memberAccess, sb);
                break;

            case CompoundMemberAccessExpressionSyntax memberAccess:
                ComposeCompoundMemberAccessExpression(memberAccess, sb);
                break;

            case ParenthesizedExpressionSyntax parens:
                ComposeParenthesizedExpression(parens, sb);
                break;

            case BinaryExpressionSyntax binary:
                ComposeBinaryExpression(binary, sb);
                break;

            case LogicalExpressionSyntax logical:
                ComposeLogicalExpression(logical, sb);
                break;

            case UnaryExpressionSyntax unary:
                ComposeUnaryExpression(unary, sb);
                break;

            case LiteralExpressionSyntax literal:
                ComposeLiteralExpression(literal, sb);
                break;

            case ArrayIndexExpressionSyntax arrayIndex:
                ComposeArrayIndexExpression(arrayIndex, sb);
                break;

            case PostfixExpressionSyntax postfix:
                ComposePostfixExpression(postfix, sb);
                break;

            case AssignmentExpressionSyntax assignment:
                ComposeAssignmentExpression(assignment, sb);
                break;

            case NativeMethodInvocationExpressionSyntax invocation:
                ComposeNativeMethodInvocationExpression(invocation, sb);
                break;

        }
    }

    private void ComposeSimpleMemberAccessExpression(SimpleMemberAccessExpressionSyntax memberAccess, StringBuilder sb)
    {
        ComposeSyntaxToken(memberAccess.Identifier, sb);
    }

    private void ComposeQualifiedMemberAccessExpression(QualifiedMemberAccessExpressionSyntax memberAccess, StringBuilder sb)
    {
        ComposeSyntaxToken(memberAccess.NameSpace, sb);
        ComposeSyntaxToken(memberAccess.Operator, sb);
        ComposeSyntaxToken(memberAccess.Identifier, sb);
    }

    private void ComposeCompoundMemberAccessExpression(CompoundMemberAccessExpressionSyntax memberAccess, StringBuilder sb)
    {
        ComposeParenthesizedExpression(memberAccess.Eval, sb);
        ComposeSyntaxToken(memberAccess.Operator, sb);
        ComposeSyntaxToken(memberAccess.Identifier, sb);
    }

    private void ComposeParenthesizedExpression(ParenthesizedExpressionSyntax parens, StringBuilder sb)
    {
        ComposeSyntaxToken(parens.ParenOpen, sb);
        ComposeExpression(parens.Expression, sb);
        ComposeSyntaxToken(parens.ParenClose, sb);
    }

    private void ComposeBinaryExpression(BinaryExpressionSyntax binary, StringBuilder sb)
    {
        ComposeExpression(binary.Left, sb);
        ComposeSyntaxToken(binary.Operation, sb);
        ComposeExpression(binary.Right, sb);
    }

    private void ComposeLogicalExpression(LogicalExpressionSyntax logical, StringBuilder sb)
    {
        ComposeExpression(logical.Left, sb);
        ComposeSyntaxToken(logical.Operation, sb);
        ComposeExpression(logical.Right, sb);
    }

    private void ComposeUnaryExpression(UnaryExpressionSyntax unaryExpression, StringBuilder sb)
    {
        ComposeSyntaxToken(unaryExpression.Operation, sb);
        ComposeExpression(unaryExpression.Expression, sb);
    }

    private void ComposeLiteralExpression(LiteralExpressionSyntax literal, StringBuilder sb)
    {
        ComposeSyntaxToken(literal.Literal, sb);
    }

    private void ComposeArrayIndexExpression(ArrayIndexExpressionSyntax arrayIndex, StringBuilder sb)
    {
        ComposeExpression(arrayIndex.Value, sb);
        foreach (var index in arrayIndex.Indexer)
            ComposeArrayIndexerExpression(index, sb);
    }

    private void ComposePostfixExpression(PostfixExpressionSyntax postfixUnaryExpression, StringBuilder sb)
    {
        ComposeExpression(postfixUnaryExpression.Expression, sb);
        ComposeSyntaxToken(postfixUnaryExpression.Operation, sb);
    }

    private void ComposeAssignmentExpression(AssignmentExpressionSyntax assignment, StringBuilder sb)
    {
        ComposeExpression(assignment.Left, sb);
        ComposeSyntaxToken(assignment.Operator, sb);
        ComposeExpression(assignment.Right, sb);
    }

    private void ComposeNativeMethodInvocationExpression(NativeMethodInvocationExpressionSyntax invocation, StringBuilder sb)
    {
        ComposeExpression(invocation.Name, sb);
        ComposeNativeMethodInvocationParameters(invocation.Parameters, sb);
    }

    private void ComposeNativeMethodInvocationParameters(NativeMethodInvocationParametersSyntax parameters, StringBuilder sb)
    {
        ComposeSyntaxToken(parameters.ParenOpen, sb);
        ComposeExpressions(parameters.ParameterList, sb);
        ComposeSyntaxToken(parameters.ParenClose, sb);
    }

    private void ComposeExpressions(CommaSeparatedSyntaxList<ExpressionSyntax>? valueList, StringBuilder sb)
    {
        if (valueList == null || valueList.Elements.Count <= 0)
            return;

        for (var i = 0; i < valueList.Elements.Count - 1; i++)
        {
            ComposeExpression(valueList.Elements[i], sb);
            ComposeSyntaxToken(syntaxFactory.Token(SyntaxTokenKind.Comma), sb);
        }

        ComposeExpression(valueList.Elements[^1], sb);
    }

    private void ComposeArrayIndexerExpression(ArrayIndexerExpressionSyntax indexer, StringBuilder sb)
    {
        ComposeSyntaxToken(indexer.BracketOpen, sb);
        ComposeExpression(indexer.Index, sb);
        ComposeSyntaxToken(indexer.BracketClose, sb);
    }

    private void ComposeName(NameSyntax name, StringBuilder sb)
    {
        switch (name)
        {
            case SimpleNameSyntax simpleName:
                ComposeSimpleName(simpleName, sb);
                break;

            case QualifiedNameSyntax qualifiedName:
                ComposeQualifiedName(qualifiedName, sb);
                break;
        }
    }

    private void ComposeSimpleName(SimpleNameSyntax name, StringBuilder sb)
    {
        ComposeSyntaxToken(name.Identifier, sb);
    }

    private void ComposeQualifiedName(QualifiedNameSyntax name, StringBuilder sb)
    {
        ComposeName(name.Left, sb);
        ComposeSyntaxToken(name.Dot, sb);
        ComposeName(name.Right, sb);
    }

    private void ComposeSyntaxToken(SyntaxToken token, StringBuilder sb)
    {
        if (token.LeadingTrivia.HasValue)
            sb.Append(token.LeadingTrivia.Value.Text);

        sb.Append(token.Text);

        if (token.TrailingTrivia.HasValue)
            sb.Append(token.TrailingTrivia.Value.Text);
    }
}