namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class GotoStatementSyntax : StatementSyntax
{
    public SyntaxToken Goto { get; private set; }
    public LiteralExpressionSyntax Label { get; private set; }
    public SyntaxToken Semicolon { get; private set; }

    public override SyntaxLocation Location => Goto.FullLocation;
    public override SyntaxSpan Span => new(Goto.FullSpan.Position, Semicolon.FullSpan.EndPosition);

    public GotoStatementSyntax(SyntaxToken gotoToken, LiteralExpressionSyntax label, SyntaxToken semicolon)
    {
        label.Parent = this;
        gotoToken.Parent = this;
        semicolon.Parent = this;

        Label = label;
        Goto = gotoToken;
        Semicolon = semicolon;

        Root.Update();
    }

    public void SetGoto(SyntaxToken gotoToken, bool updatePosition = true)
    {
        gotoToken.Parent = this;
        Goto = gotoToken;

        if (updatePosition)
            Root.Update();
    }

    public void SetLabel(LiteralExpressionSyntax label, bool updatePosition = true)
    {
        label.Parent = this;
        Label = label;

        if (updatePosition)
            Root.Update();
    }

    public void SetSemicolon(SyntaxToken semicolon, bool updatePosition = true)
    {
        semicolon.Parent = this;
        Semicolon = semicolon;

        if (updatePosition)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken gotoToken = Goto;
        SyntaxToken semicolon = Semicolon;

        position = gotoToken.UpdatePosition(position, ref line, ref column);
        position = Label.UpdatePosition(position, ref line, ref column);
        position = semicolon.UpdatePosition(position, ref line, ref column);

        Goto = gotoToken;
        Semicolon = semicolon;

        return position;
    }
}