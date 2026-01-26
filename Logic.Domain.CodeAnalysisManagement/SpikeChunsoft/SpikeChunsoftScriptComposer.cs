using System.Text;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;
using Logic.Domain.CodeAnalysisManagement.Contract.SpikeChunsoft;

namespace Logic.Domain.CodeAnalysisManagement.SpikeChunsoft;

internal class SpikeChunsoftScriptComposer : ISpikeChunsoftScriptComposer
{
    private readonly ISpikeChunsoftSyntaxFactory _syntaxFactory;

    public SpikeChunsoftScriptComposer(ISpikeChunsoftSyntaxFactory syntaxFactory)
    {
        _syntaxFactory = syntaxFactory;
    }

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
            ComposeSyntaxToken(_syntaxFactory.Token(SyntaxTokenKind.Comma), sb);
        }

        ComposeLiteralExpression(valueList.Elements[^1], sb);
    }

    private void ComposeMethodDeclarationBody(MethodDeclarationBodySyntax methodDeclarationBody, StringBuilder sb)
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
            case GotoLabelStatementSyntax gotoStatement:
                ComposeGotoLabelStatement(gotoStatement, sb);
                break;

            case ReturnStatementSyntax returnStatement:
                ComposeReturnStatement(returnStatement, sb);
                break;

            case MethodInvocationStatementSyntax methodInvocationStatement:
                ComposeMethodInvocationStatement(methodInvocationStatement, sb);
                break;
        }
    }

    private void ComposeGotoLabelStatement(GotoLabelStatementSyntax gotoLabelStatement, StringBuilder sb)
    {
        ComposeLiteralExpression(gotoLabelStatement.Label, sb);
        ComposeSyntaxToken(gotoLabelStatement.Colon, sb);
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