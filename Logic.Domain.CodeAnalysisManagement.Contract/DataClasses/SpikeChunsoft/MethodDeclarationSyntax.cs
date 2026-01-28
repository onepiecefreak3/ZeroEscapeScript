namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class MethodDeclarationSyntax : SyntaxNode
{
    public LiteralExpressionSyntax Name { get; private set; }
    public MethodDeclarationParametersSyntax Parameters { get; private set; }
    public BlockExpression Body { get; private set; }

    public override SyntaxLocation Location => Name.Location;
    public override SyntaxSpan Span => new(Name.Span.Position, Body.Span.EndPosition);

    public MethodDeclarationSyntax(LiteralExpressionSyntax name, MethodDeclarationParametersSyntax parameters, BlockExpression body)
    {
        name.Parent = this;
        parameters.Parent = this;
        body.Parent = this;

        Name = name;
        Parameters = parameters;
        Body = body;

        Root.Update();
    }

    public void SetName(LiteralExpressionSyntax name, bool updatePosition = true)
    {
        name.Parent = this;
        Name = name;

        if (updatePosition)
            Root.Update();
    }

    public void SetParameters(MethodDeclarationParametersSyntax parameters, bool updatePosition = true)
    {
        parameters.Parent = this;
        Parameters = parameters;

        if (updatePosition)
            Root.Update();
    }

    public void SetBody(BlockExpression body, bool updatePosition = true)
    {
        body.Parent = this;
        Body = body;

        if (updatePosition)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        position = Name.UpdatePosition(position, ref line, ref column);
        position = Parameters.UpdatePosition(position, ref line, ref column);
        position = Body.UpdatePosition(position, ref line, ref column);

        return position;
    }
}