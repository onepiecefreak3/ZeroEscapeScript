namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class NativeMethodInvocationExpressionSyntax : ExpressionSyntax
{
    public LiteralExpressionSyntax Name { get; private set; }
    public NativeMethodInvocationParametersSyntax Parameters { get; private set; }

    public override SyntaxLocation Location => Name.Location;
    public override SyntaxSpan Span => new(Name.Span.Position, Parameters.Span.EndPosition);

    public NativeMethodInvocationExpressionSyntax(LiteralExpressionSyntax name, NativeMethodInvocationParametersSyntax parameters)
    {
        name.Parent = this;
        parameters.Parent = this;

        Name = name;
        Parameters = parameters;

        Root.Update();
    }

    public void SetName(LiteralExpressionSyntax name, bool updatePosition = true)
    {
        name.Parent = this;

        Name = name;

        if (updatePosition)
            Root.Update();
    }

    public void SetParameters(NativeMethodInvocationParametersSyntax parameters, bool updatePosition = true)
    {
        parameters.Parent = this;
        Parameters = parameters;

        if (updatePosition)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        position = Name.UpdatePosition(position, ref line, ref column);
        position = Parameters.UpdatePosition(position, ref line, ref column);

        return position;
    }
}