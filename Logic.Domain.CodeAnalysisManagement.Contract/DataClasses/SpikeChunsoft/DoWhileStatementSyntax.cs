namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class DoWhileStatementSyntax : StatementSyntax
{
    public SyntaxToken Do { get; private set; }
    public BlockExpression Body { get; private set; }
    public SyntaxToken While { get; private set; }
    public SyntaxToken ParenOpen { get; private set; }
    public LiteralExpressionSyntax Condition { get; private set; }
    public SyntaxToken ParenClose { get; private set; }
    public SyntaxToken Semicolon { get; private set; }

    public override SyntaxLocation Location => Do.FullLocation;
    public override SyntaxSpan Span => new(Do.FullSpan.Position, Semicolon.FullSpan.EndPosition);

    public DoWhileStatementSyntax(SyntaxToken doToken, BlockExpression body, SyntaxToken whileToken, SyntaxToken parenOpen,
        LiteralExpressionSyntax condition, SyntaxToken parenClose, SyntaxToken semicolon)
    {
        doToken.Parent = this;
        body.Parent = this;
        whileToken.Parent = this;
        parenOpen.Parent = this;
        condition.Parent = this;
        parenClose.Parent = this;
        semicolon.Parent = this;

        Do = doToken;
        Body = body;
        While = whileToken;
        ParenOpen = parenOpen;
        Condition = condition;
        ParenClose = parenClose;
        Semicolon = semicolon;

        Root.Update();
    }

    public void SetDo(SyntaxToken doToken, bool updatePositions = true)
    {
        doToken.Parent = this;
        Do = doToken;
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

    public void SetWhile(SyntaxToken whileToken, bool updatePositions = true)
    {
        whileToken.Parent = this;
        While = whileToken;
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

    public void SetSemicolon(SyntaxToken semicolon, bool updatePositions = true)
    {
        semicolon.Parent = this;
        Semicolon = semicolon;
        if (updatePositions)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken doToken = Do;
        SyntaxToken whileToken = While;
        SyntaxToken parenOpen = ParenOpen;
        SyntaxToken parenClose = ParenClose;
        SyntaxToken semicolon = Semicolon;

        position = doToken.UpdatePosition(position, ref line, ref column);
        position = Body.UpdatePosition(position, ref line, ref column);
        position = whileToken.UpdatePosition(position, ref line, ref column);
        position = parenOpen.UpdatePosition(position, ref line, ref column);
        position = Condition.UpdatePosition(position, ref line, ref column);
        position = parenClose.UpdatePosition(position, ref line, ref column);
        position = semicolon.UpdatePosition(position, ref line, ref column);

        Do = doToken;
        While = whileToken;
        ParenOpen = parenOpen;
        ParenClose = parenClose;
        Semicolon = semicolon;

        return position;
    }
}
