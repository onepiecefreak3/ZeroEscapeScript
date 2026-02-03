namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class AssignmentStatementSyntax : StatementSyntax
{
    public AssignmentExpressionSyntax Assignment { get; private set; }
    public SyntaxToken Semicolon { get; private set; }

    public override SyntaxLocation Location => Assignment.Location;
    public override SyntaxSpan Span => new(Assignment.Span.Position, Semicolon.FullSpan.EndPosition);

    public AssignmentStatementSyntax(AssignmentExpressionSyntax assignment, SyntaxToken semicolon)
    {
        assignment.Parent = this;
        semicolon.Parent = this;

        Assignment = assignment;
        Semicolon = semicolon;

        Root.Update();
    }

    public void SetAssignment(AssignmentExpressionSyntax assignment, bool updatePositions = true)
    {
        assignment.Parent = this;

        Assignment = assignment;

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
        SyntaxToken semicolon = Semicolon;

        position = Assignment.UpdatePosition(position, ref line, ref column);
        position = semicolon.UpdatePosition(position, ref line, ref column);

        Semicolon = semicolon;

        return position;
    }
}