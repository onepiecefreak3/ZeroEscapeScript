namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class AssignmentExpressionSyntax : ExpressionSyntax
{
    public ExpressionSyntax Left { get; private set; }
    public SyntaxToken Operation { get; private set; }
    public ExpressionSyntax Right { get; private set; }

    public override SyntaxLocation Location => Left.Location;
    public override SyntaxSpan Span => new(Left.Span.Position, Right.Span.EndPosition);

    public AssignmentExpressionSyntax(ExpressionSyntax left, SyntaxToken operation, ExpressionSyntax right)
    {
        left.Parent = this;
        operation.Parent = this;
        right.Parent = this;

        Left = left;
        Operation = operation;
        Right = right;

        Root.Update();
    }

    public void SetOperator(SyntaxToken @operator, bool updatePositions = true)
    {
        @operator.Parent = this;

        Operation = @operator;

        if (updatePositions)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken @operator = Operation;

        position = Left.UpdatePosition(position, ref line, ref column);
        position = @operator.UpdatePosition(position, ref line, ref column);
        position = Right.UpdatePosition(position, ref line, ref column);

        Operation = @operator;

        return position;
    }
}