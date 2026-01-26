namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class QualifiedNameSyntax : NameSyntax
{
    public NameSyntax Left { get; private set; }
    public SyntaxToken Dot { get; private set; }
    public NameSyntax Right { get; private set; }

    public override SyntaxLocation Location => Left.Location;
    public override SyntaxSpan Span => new(Left.Span.Position, Right.Span.EndPosition);

    public QualifiedNameSyntax(NameSyntax left, SyntaxToken dot, NameSyntax right)
    {
        left.Parent = this;
        dot.Parent = this;
        right.Parent = this;

        Left = left;
        Dot = dot;
        Right = right;
    }

    public void SetLeft(NameSyntax name, bool updatePositions = true)
    {
        name.Parent = this;

        Left = name;

        if (updatePositions)
            Root.Update();
    }

    public void SetDot(SyntaxToken dot, bool updatePositions = true)
    {
        dot.Parent = this;

        Dot = dot;

        if (updatePositions)
            Root.Update();
    }

    public void SetRight(NameSyntax name, bool updatePositions = true)
    {
        name.Parent = this;

        Right = name;

        if (updatePositions)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken dot = Dot;

        position = Left.UpdatePosition(position, ref line, ref column);
        position = dot.UpdatePosition(position, ref line, ref column);
        position = Right.UpdatePosition(position, ref line, ref column);

        Dot = dot;

        return position;
    }
}