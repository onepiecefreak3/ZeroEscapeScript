namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class ExitStatementSyntax : StatementSyntax
{
    public SyntaxToken Return { get; private set; }
    public SyntaxToken Semicolon { get; private set; }

    public override SyntaxLocation Location => Return.FullLocation;
    public override SyntaxSpan Span => new(Return.FullSpan.Position, Semicolon.FullSpan.EndPosition);

    public ExitStatementSyntax(SyntaxToken returnToken, SyntaxToken semicolon)
    {
        returnToken.Parent = this;
        semicolon.Parent = this;

        Return = returnToken;
        Semicolon = semicolon;

        Root.Update();
    }

    public void SetReturn(SyntaxToken returnToken, bool updatePositions = true)
    {
        returnToken.Parent = this;
        Return = returnToken;

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
        SyntaxToken returnToken = Return;
        SyntaxToken semicolon = Semicolon;

        position = returnToken.UpdatePosition(position, ref line, ref column);
        position = semicolon.UpdatePosition(position, ref line, ref column);

        Return = returnToken;
        Semicolon = semicolon;

        return position;
    }
}