namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class SimpleNameSyntax : NameSyntax
{
    public SyntaxToken Identifier { get; private set; }

    public override SyntaxLocation Location => Identifier.FullLocation;
    public override SyntaxSpan Span => Identifier.FullSpan;

    public SimpleNameSyntax(SyntaxToken identifier)
    {
        identifier.Parent = this;

        Identifier = identifier;
    }

    public void SetIdentifier(SyntaxToken identifier, bool updatePositions = true)
    {
        identifier.Parent = this;

        Identifier = identifier;

        if (updatePositions)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken identifier = Identifier;

        position = identifier.UpdatePosition(position, ref line, ref column);

        Identifier = identifier;

        return position;
    }
}