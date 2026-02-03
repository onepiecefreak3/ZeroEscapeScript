namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class NativeMethodInvocationStatementSyntax : StatementSyntax
{
    public NativeMethodInvocationExpressionSyntax Method { get; private set; }
    public SyntaxToken Semicolon { get; private set; }

    public override SyntaxLocation Location => Method.Location;
    public override SyntaxSpan Span => new(Method.Span.Position, Semicolon.FullSpan.EndPosition);

    public NativeMethodInvocationStatementSyntax(NativeMethodInvocationExpressionSyntax method, SyntaxToken semicolon)
    {
        method.Parent = this;
        semicolon.Parent = this;

        Method = method;
        Semicolon = semicolon;

        Root.Update();
    }

    public void SetMethod(NativeMethodInvocationExpressionSyntax method, bool updatePosition = true)
    {
        method.Parent = this;
        Method = method;

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

        position = Method.UpdatePosition(position, ref line, ref column);
        position = semicolon.UpdatePosition(position, ref line, ref column);

        Semicolon = semicolon;

        return position;
    }
}