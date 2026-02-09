namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class CompoundMemberAccessExpressionSyntax : MemberAccessExpressionSyntax
{
    public ParenthesizedExpressionSyntax Eval { get; private set; }
    public SyntaxToken Operator { get; private set; }

    public override SyntaxLocation Location => Eval.Location;
    public override SyntaxSpan Span => new(Eval.Span.Position, Identifier.FullSpan.EndPosition);

    public CompoundMemberAccessExpressionSyntax(ParenthesizedExpressionSyntax eval, SyntaxToken operatorToken, SyntaxToken identifier)
        : base(identifier)
    {
        eval.Parent = this;
        operatorToken.Parent = this;

        Eval = eval;
        Operator = operatorToken;

        Root.Update();
    }

    public void SetOperator(SyntaxToken operatorToken, bool updatePositions = true)
    {
        operatorToken.Parent = this;

        Operator = operatorToken;

        if (updatePositions)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken operatorToken = Operator;
        SyntaxToken identifier = Identifier;

        position = Eval.UpdatePosition(position, ref line, ref column);
        position = operatorToken.UpdatePosition(position, ref line, ref column);
        position = identifier.UpdatePosition(position, ref line, ref column);

        Operator = operatorToken;
        Identifier = identifier;

        return position;
    }
}