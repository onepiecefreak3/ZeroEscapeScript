namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class IfStatementSyntax : StatementSyntax
{
    public SyntaxToken If { get; private set; }
    public SyntaxToken ParenOpen { get; private set; }
    public LiteralExpressionSyntax Condition { get; private set; }
    public SyntaxToken ParenClose { get; private set; }
    public BlockExpression Body { get; private set; }

    public override SyntaxLocation Location => If.FullLocation;
    public override SyntaxSpan Span => new(If.FullSpan.Position, Body.Span.EndPosition);

    public IfStatementSyntax(SyntaxToken ifToken, SyntaxToken parenOpen, LiteralExpressionSyntax condition, SyntaxToken parenClose,
        BlockExpression body)
    {
        ifToken.Parent = this;
        parenOpen.Parent = this;
        condition.Parent = this;
        parenClose.Parent = this;
        body.Parent = this;

        If = ifToken;
        ParenOpen = parenOpen;
        Condition = condition;
        ParenClose = parenClose;
        Body = body;

        Root.Update();
    }

    public void SetIf(SyntaxToken ifToken, bool updatePositions = true)
    {
        ifToken.Parent = this;
        If = ifToken;
        if (updatePositions)
            Root.Update();
    }

    public void SetParenOpen(SyntaxToken parenOpen, bool updatePositions = true)
    {
        parenOpen.Parent = this;
        ParenOpen = parenOpen;
        if (updatePositions)
            Root.Update();
    }

    public void SetParenClose(SyntaxToken parenClose, bool updatePositions = true)
    {
        parenClose.Parent = this;
        ParenClose = parenClose;
        if (updatePositions)
            Root.Update();
    }

    public void SetBody(BlockExpression body, bool updatePositions = true)
    {
        body.Parent = this;
        Body = body;
        if (updatePositions)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken ifToken = If;
        SyntaxToken parenOpen = ParenOpen;
        SyntaxToken parenClose = ParenClose;

        position = ifToken.UpdatePosition(position, ref line, ref column);
        position = parenOpen.UpdatePosition(position, ref line, ref column);
        position = Condition.UpdatePosition(position, ref line, ref column);
        position = parenClose.UpdatePosition(position, ref line, ref column);
        position = Body.UpdatePosition(position, ref line, ref column);

        If = ifToken;
        ParenOpen = parenOpen;
        ParenClose = parenClose;

        return position;
    }
}
