namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class CodeUnitSyntax : SyntaxNode
{
    public NameDeclarationSyntax NameDeclaration { get; private set; }
    public IReadOnlyList<MethodDeclarationSyntax> MethodDeclarations { get; private set; }

    public override SyntaxLocation Location => NameDeclaration.Location;
    public override SyntaxSpan Span => new(NameDeclaration.Span.Position,
        MethodDeclarations.Count > 0 ? MethodDeclarations[^1].Span.EndPosition : NameDeclaration.Span.EndPosition);

    public CodeUnitSyntax(NameDeclarationSyntax nameDeclaration, IReadOnlyList<MethodDeclarationSyntax>? methodDeclarations)
    {
        NameDeclaration = nameDeclaration;
        MethodDeclarations = methodDeclarations ?? new List<MethodDeclarationSyntax>();

        nameDeclaration.Parent = this;
        foreach (MethodDeclarationSyntax methodDeclaration in MethodDeclarations)
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

    public void SetMethodDeclarations(IReadOnlyList<MethodDeclarationSyntax>? methodDeclarations, bool updatePosition = true)
    {
        MethodDeclarations = methodDeclarations ?? new List<MethodDeclarationSyntax>();
        
        foreach (MethodDeclarationSyntax methodDeclaration in MethodDeclarations)
            methodDeclaration.Parent = this;

        if (updatePosition)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        position = NameDeclaration.UpdatePosition(position, ref line, ref column);

        foreach (MethodDeclarationSyntax methodDeclaration in MethodDeclarations)
            position = methodDeclaration.UpdatePosition(position, ref line, ref column);

        return position;
    }
}