namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class IfElseStatementSyntax : StatementSyntax
{
    public SyntaxToken If { get; private set; }
    public SyntaxToken ParenOpen { get; private set; }
    public ExpressionSyntax Condition { get; private set; }
    public SyntaxToken ParenClose { get; private set; }
    public BlockExpression Body { get; private set; }
    public SyntaxToken Else { get; private set; }
    public BlockExpression ElseBody { get; private set; }

    public override SyntaxLocation Location => If.FullLocation;
    public override SyntaxSpan Span => new(If.FullSpan.Position, ElseBody.Span.EndPosition);

    public IfElseStatementSyntax(SyntaxToken ifToken, SyntaxToken parenOpen, ExpressionSyntax condition, SyntaxToken parenClose,
        BlockExpression body, SyntaxToken elseToken, BlockExpression elseBody)
    {
        ifToken.Parent = this;
        parenOpen.Parent = this;
        condition.Parent = this;
        parenClose.Parent = this;
        body.Parent = this;
        elseToken.Parent = this;
        elseBody.Parent = this;

        If = ifToken;
        ParenOpen = parenOpen;
        Condition = condition;
        ParenClose = parenClose;
        Body = body;
        Else = elseToken;
        ElseBody = elseBody;

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

    public void SetElse(SyntaxToken elseToken, bool updatePositions = true)
    {
        elseToken.Parent = this;
        Else = elseToken;
        if (updatePositions)
            Root.Update();
    }

    public void SetElseBody(BlockExpression elseBody, bool updatePositions = true)
    {
        elseBody.Parent = this;
        ElseBody = elseBody;
        if (updatePositions)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken ifToken = If;
        SyntaxToken parenOpen = ParenOpen;
        SyntaxToken parenClose = ParenClose;
        SyntaxToken elseToken = Else;

        position = ifToken.UpdatePosition(position, ref line, ref column);
        position = parenOpen.UpdatePosition(position, ref line, ref column);
        position = Condition.UpdatePosition(position, ref line, ref column);
        position = parenClose.UpdatePosition(position, ref line, ref column);
        position = Body.UpdatePosition(position, ref line, ref column);
        position = elseToken.UpdatePosition(position, ref line, ref column);
        position = ElseBody.UpdatePosition(position, ref line, ref column);

        If = ifToken;
        ParenOpen = parenOpen;
        ParenClose = parenClose;
        Else = elseToken;

        return position;
    }
}
