namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class IfNotGotoStatementSyntax : StatementSyntax
{
    public SyntaxToken If { get; private set; }
    public UnaryExpressionSyntax Comparison { get; private set; }
    public GotoExpressionSyntax Goto { get; private set; }
    public SyntaxToken Semicolon { get; private set; }

    public override SyntaxLocation Location => If.FullLocation;
    public override SyntaxSpan Span => new(If.FullSpan.Position, Goto.Span.EndPosition);

    public IfNotGotoStatementSyntax(SyntaxToken ifToken, UnaryExpressionSyntax comparison, GotoExpressionSyntax gotoExpression, SyntaxToken semicolon)
    {
        ifToken.Parent = this;
        comparison.Parent = this;
        gotoExpression.Parent = this;
        semicolon.Parent = this;

        If = ifToken;
        Comparison = comparison;
        Goto = gotoExpression;
        Semicolon = semicolon;

        Root.Update();
    }

    public void SetIf(SyntaxToken ifToken, bool updatePositions = true)
    {
        ifToken.Parent = this;

        If = ifToken;

        if (updatePositions)
            Root.Update();
    }

    public void SetSemicolon(SyntaxToken semicolon, bool updatePositions = true)
    {
        semicolon.Parent = this;

        Semicolon = semicolon;

        if (updatePositions)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken ifToken = If;
        SyntaxToken semicolon = Semicolon;

        position = ifToken.UpdatePosition(position, ref line, ref column);
        position = Comparison.UpdatePosition(position, ref line, ref column);
        position = Goto.UpdatePosition(position, ref line, ref column);
        position = semicolon.UpdatePosition(position, ref line, ref column);

        If = ifToken;
        Semicolon = semicolon;

        return position;
    }
}