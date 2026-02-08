namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class ExportedGotoLabelStatementSyntax : StatementSyntax
{
    public SyntaxToken Export { get; private set; }
    public LiteralExpressionSyntax Label { get; private set; }
    public SyntaxToken Colon { get; private set; }

    public override SyntaxLocation Location => Label.Location;
    public override SyntaxSpan Span => new(Label.Span.Position, Colon.FullSpan.EndPosition);

    public ExportedGotoLabelStatementSyntax(SyntaxToken export, LiteralExpressionSyntax label, SyntaxToken colon)
    {
        export.Parent = this;
        label.Parent = this;
        colon.Parent = this;

        Export = export;
        Label = label;
        Colon = colon;

        Root.Update();
    }

    public void SetExport(SyntaxToken export, bool updatePosition = true)
    {
        export.Parent = this;
        Export = export;

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

    public void SetColon(SyntaxToken colon, bool updatePosition = true)
    {
        colon.Parent = this;
        Colon = colon;

        if (updatePosition)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken export = Export;
        SyntaxToken colon = Colon;

        position = export.UpdatePosition(position, ref line, ref column);
        position = Label.UpdatePosition(position, ref line, ref column);
        position = colon.UpdatePosition(position, ref line, ref column);

        Export = export;
        Colon = colon;

        return position;
    }
}