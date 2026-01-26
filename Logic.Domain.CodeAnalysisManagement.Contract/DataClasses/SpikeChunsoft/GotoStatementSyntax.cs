namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class GotoStatementSyntax : StatementSyntax
{
    public SyntaxToken Goto { get; private set; }
    public CommaSeparatedSyntaxList<ValueExpressionSyntax> Targets { get; private set; }
    public SyntaxToken Semicolon { get; private set; }

    public override SyntaxLocation Location => Goto.FullLocation;
    public override SyntaxSpan Span => new(Goto.FullSpan.Position, Targets.Span.EndPosition);

    public GotoStatementSyntax(SyntaxToken gotoToken, CommaSeparatedSyntaxList<ValueExpressionSyntax> targets, SyntaxToken semicolon)
    {
        gotoToken.Parent = this;
        targets.Parent = this;
        semicolon.Parent = this;

        Goto = gotoToken;
        Targets = targets;
        Semicolon = semicolon;

        Root.Update();
    }

    public void SetGoto(SyntaxToken gotoToken, bool updatePositions = true)
    {
        gotoToken.Parent = this;

        Goto = gotoToken;

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
        SyntaxToken gotoToken = Goto;
        SyntaxToken semicolon = Semicolon;

        position = gotoToken.UpdatePosition(position, ref line, ref column);
        position = Targets.UpdatePosition(position, ref line, ref column);
        position = semicolon.UpdatePosition(position, ref line, ref column);

        Goto = gotoToken;
        Semicolon = semicolon;

        return position;
    }
}