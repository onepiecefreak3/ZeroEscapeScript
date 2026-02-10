namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class CodeUnitSyntax : SyntaxNode
{
    public NameDeclarationSyntax NameDeclaration { get; private set; }
    public IReadOnlyList<DeclarationSyntax> Members { get; private set; }

    public override SyntaxLocation Location => NameDeclaration.Location;
    public override SyntaxSpan Span => new(NameDeclaration.Span.Position,
        Members.Count > 0 ? Members[^1].Span.EndPosition : NameDeclaration.Span.EndPosition);

    public CodeUnitSyntax(NameDeclarationSyntax nameDeclaration, IReadOnlyList<DeclarationSyntax>? members)
    {
        NameDeclaration = nameDeclaration;
        Members = members ?? new List<DeclarationSyntax>();

        nameDeclaration.Parent = this;
        foreach (DeclarationSyntax methodDeclaration in Members)
            methodDeclaration.Parent = this;

        Root.Update();
    }

    public void SetNameDeclaration(NameDeclarationSyntax nameDeclaration, bool updatePosition = true)
    {
        nameDeclaration.Parent = this;
        NameDeclaration = nameDeclaration;

        if (updatePosition)
            Root.Update();
    }

    public void SetMembers(IReadOnlyList<DeclarationSyntax>? members, bool updatePosition = true)
    {
        Members = members ?? new List<DeclarationSyntax>();
        
        foreach (DeclarationSyntax methodDeclaration in Members)
            methodDeclaration.Parent = this;

        if (updatePosition)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        position = NameDeclaration.UpdatePosition(position, ref line, ref column);

        foreach (DeclarationSyntax member in Members)
            position = member.UpdatePosition(position, ref line, ref column);

        return position;
    }
}