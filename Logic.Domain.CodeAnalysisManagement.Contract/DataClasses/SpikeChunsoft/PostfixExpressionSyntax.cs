namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class PostfixExpressionSyntax : ExpressionSyntax
{
    public ExpressionSyntax Expression { get; private set; }
    public SyntaxToken Operation { get; private set; }

    public override SyntaxLocation Location => Expression.Location;
    public override SyntaxSpan Span => new(Expression.Span.Position, Operation.FullSpan.EndPosition);

    public PostfixExpressionSyntax(ExpressionSyntax expression, SyntaxToken operation)
    {
        operation.Parent = this;
        expression.Parent = this;

        Operation = operation;
        Expression = expression;

        Root.Update();
    }

    public void SetExpression(ExpressionSyntax expression, bool updatePositions = true)
    {
        expression.Parent = this;

        Expression = expression;

        if (updatePositions)
            Root.Update();
    }

    public void SetOperation(SyntaxToken operation, bool updatePositions = true)
    {
        operation.Parent = this;

        Operation = operation;

        if (updatePositions)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken operation = Operation;

        position = operation.UpdatePosition(position, ref line, ref column);
        position = Expression.UpdatePosition(position, ref line, ref column);

        Operation = operation;

        return position;
    }
}
