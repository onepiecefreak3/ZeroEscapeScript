namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class AssignmentExpressionSyntax : ExpressionSyntax
{
    public ExpressionSyntax Left { get; private set; }
    public SyntaxToken Operator { get; private set; }
    public ExpressionSyntax Right { get; private set; }

    public override SyntaxLocation Location => Left.Location;
    public override SyntaxSpan Span => new(Left.Span.Position, Right.Span.EndPosition);

    public AssignmentExpressionSyntax(ExpressionSyntax left, SyntaxToken @operator, ExpressionSyntax right)
    {
        left.Parent = this;
        @operator.Parent = this;
        right.Parent = this;

        Left = left;
        Operator = @operator;
        Right = right;

        Root.Update();
    }

    public void SetOperator(SyntaxToken @operator, bool updatePositions = true)
    {
        @operator.Parent = this;

        Operator = @operator;

        if (updatePositions)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken @operator = Operator;

        position = Left.UpdatePosition(position, ref line, ref column);
        position = @operator.UpdatePosition(position, ref line, ref column);
        position = Right.UpdatePosition(position, ref line, ref column);

        Operator = @operator;

        return position;
    }
}