namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class MethodInvocationStatementSyntax : StatementSyntax
{
    public NameSyntax Name { get; private set; }
    public MethodInvocationParametersSyntax Parameters { get; private set; }
    public SyntaxToken Semicolon { get; private set; }

    public override SyntaxLocation Location => Name.Location;
    public override SyntaxSpan Span => new(Name.Span.Position, Parameters.Span.EndPosition);

    public MethodInvocationStatementSyntax(NameSyntax name, MethodInvocationParametersSyntax parameters, SyntaxToken semicolon)
    {
        name.Parent = this;
        parameters.Parent = this;
        semicolon.Parent = this;

        Name = name;
        Parameters = parameters;
        Semicolon = semicolon;

        Root.Update();
    }

    public void SetName(NameSyntax name, bool updatePosition = true)
    {
        name.Parent = this;

        Name = name;

        if (updatePosition)
            Root.Update();
    }

    public void SetParameters(MethodInvocationParametersSyntax parameters, bool updatePosition = true)
    {
        parameters.Parent = this;
        Parameters = parameters;

        if (updatePosition)
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
        SyntaxToken semicolon = Semicolon;

        position = Name.UpdatePosition(position, ref line, ref column);
        position = Parameters.UpdatePosition(position, ref line, ref column);
        position = semicolon.UpdatePosition(position, ref line, ref column);

        Semicolon = semicolon;

        return position;
    }
}