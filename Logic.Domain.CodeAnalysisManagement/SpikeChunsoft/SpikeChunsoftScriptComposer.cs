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
        foreach (MethodDeclarationSyntax methodDeclaration in codeUnit.MethodDeclarations)
            ComposeMethodDeclaration(methodDeclaration, sb);
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
            case AsyncBlockStatement asyncStatement:
                ComposeAsyncBlockStatement(asyncStatement, sb);
                break;

            case IfStatementSyntax ifStatement:
                ComposeIfStatement(ifStatement, sb);
                break;

            case IfElseStatementSyntax ifElseStatement:
                ComposeIfElseStatement(ifElseStatement, sb);
                break;

            case DoWhileStatementSyntax doWhileStatement:
                ComposeDoWhileStatement(doWhileStatement, sb);
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

    private void ComposeAsyncBlockStatement(AsyncBlockStatement asyncStatement, StringBuilder sb)
    {
        ComposeSyntaxToken(asyncStatement.Async, sb);
        ComposeBlock(asyncStatement.Body, sb);
    }

    private void ComposeBlock(BlockExpression blockExpression, StringBuilder sb)
    {
        ComposeSyntaxToken(blockExpression.CurlyOpen, sb);

        foreach (StatementSyntax expression in blockExpression.Statements)
            ComposeStatement(expression, sb);

        ComposeSyntaxToken(blockExpression.CurlyClose, sb);
    }

    private void ComposeIfStatement(IfStatementSyntax ifStatement, StringBuilder sb)
    {
        ComposeSyntaxToken(ifStatement.If, sb);
        ComposeSyntaxToken(ifStatement.ParenOpen, sb);
        ComposeLiteralExpression(ifStatement.Condition, sb);
        ComposeSyntaxToken(ifStatement.ParenClose, sb);
        ComposeBlock(ifStatement.Body, sb);
    }

    private void ComposeIfElseStatement(IfElseStatementSyntax ifElseStatement, StringBuilder sb)
    {
        ComposeSyntaxToken(ifElseStatement.If, sb);
        ComposeSyntaxToken(ifElseStatement.ParenOpen, sb);
        ComposeLiteralExpression(ifElseStatement.Condition, sb);
        ComposeSyntaxToken(ifElseStatement.ParenClose, sb);
        ComposeBlock(ifElseStatement.Body, sb);
        ComposeSyntaxToken(ifElseStatement.Else, sb);
        ComposeBlock(ifElseStatement.ElseBody, sb);
    }

    private void ComposeDoWhileStatement(DoWhileStatementSyntax doWhileStatement, StringBuilder sb)
    {
        ComposeSyntaxToken(doWhileStatement.Do, sb);
        ComposeBlock(doWhileStatement.Body, sb);
        ComposeSyntaxToken(doWhileStatement.While, sb);
        ComposeSyntaxToken(doWhileStatement.ParenOpen, sb);
        ComposeLiteralExpression(doWhileStatement.Condition, sb);
        ComposeSyntaxToken(doWhileStatement.ParenClose, sb);
        ComposeSyntaxToken(doWhileStatement.Semicolon, sb);
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
            ComposeLiteralExpression(returnStatement.Expression, sb);
        ComposeSyntaxToken(returnStatement.Semicolon, sb);
    }

    private void ComposeMethodInvocationStatement(MethodInvocationStatementSyntax invocation, StringBuilder sb)
    {
        ComposeName(invocation.Name, sb);
        ComposeMethodInvocationParameters(invocation.Parameters, sb);
        ComposeSyntaxToken(invocation.Semicolon, sb);
    }

    private void ComposeMethodInvocationParameters(MethodInvocationParametersSyntax invocationParameters, StringBuilder sb)
    {
        ComposeSyntaxToken(invocationParameters.ParenOpen, sb);
        ComposeLiteralExpressions(invocationParameters.ParameterList, sb);
        ComposeSyntaxToken(invocationParameters.ParenClose, sb);
    }

    private void ComposeLiteralExpression(LiteralExpressionSyntax literal, StringBuilder sb)
    {
        ComposeSyntaxToken(literal.Literal, sb);
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