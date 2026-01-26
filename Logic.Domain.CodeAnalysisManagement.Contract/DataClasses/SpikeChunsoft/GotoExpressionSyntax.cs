namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class GotoExpressionSyntax : StatementSyntax
{
    public SyntaxToken Goto { get; private set; }
    public ValueExpressionSyntax Target { get; private set; }

    public override SyntaxLocation Location => Goto.FullLocation;
    public override SyntaxSpan Span => new(Goto.FullSpan.Position, Target.Span.EndPosition);

    public GotoExpressionSyntax(SyntaxToken gotoToken, ValueExpressionSyntax target)
    {
        gotoToken.Parent = this;
        target.Parent = this;

        Goto = gotoToken;
        Target = target;

        Root.Update();
    }

    public void SetGoto(SyntaxToken gotoToken, bool updatePositions = true)
    {
        gotoToken.Parent = this;

        Goto = gotoToken;

        if (updatePositions)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken gotoToken = Goto;

        position = gotoToken.UpdatePosition(position, ref line, ref column);
        position = Target.UpdatePosition(position, ref line, ref column);

        Goto = gotoToken;

        return position;
    }
}