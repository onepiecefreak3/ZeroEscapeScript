namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class PostfixStatementSyntax : StatementSyntax
{
    public PostfixExpressionSyntax Postfix { get; private set; }
    public SyntaxToken Semicolon { get; private set; }

    public override SyntaxLocation Location => Postfix.Location;
    public override SyntaxSpan Span => new(Postfix.Span.Position, Semicolon.FullSpan.EndPosition);

    public PostfixStatementSyntax(PostfixExpressionSyntax postfix, SyntaxToken semicolon)
    {
        postfix.Parent = this;
        semicolon.Parent = this;

        Postfix = postfix;
        Semicolon = semicolon;

        Root.Update();
    }

    public void SetPostfix(PostfixExpressionSyntax assignment, bool updatePositions = true)
    {
        assignment.Parent = this;

        Postfix = assignment;

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
        SyntaxToken semicolon = Semicolon;

        position = Postfix.UpdatePosition(position, ref line, ref column);
        position = semicolon.UpdatePosition(position, ref line, ref column);

        Semicolon = semicolon;

        return position;
    }
}