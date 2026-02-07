namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class CaseStatementSyntax : SyntaxNode
{
    public SyntaxToken Case { get; private set; }
    public ExpressionSyntax Label { get; private set; }
    public SyntaxToken Colon { get; private set; }
    public IReadOnlyList<StatementSyntax> Statements { get; private set; }

    public override SyntaxLocation Location => Case.FullLocation;
    public override SyntaxSpan Span => new(Case.FullSpan.Position,
        Statements.Count > 0 ? Statements[^1].Span.EndPosition : Colon.FullSpan.EndPosition);

    public CaseStatementSyntax(SyntaxToken caseToken, ExpressionSyntax label, SyntaxToken colon, IReadOnlyList<StatementSyntax> statements)
    {
        caseToken.Parent = this;
        label.Parent = this;
        colon.Parent = this;

        Case = caseToken;
        Label = label;
        Colon = colon;
        Statements = statements ?? new List<StatementSyntax>();

        foreach (StatementSyntax statement in Statements)
            statement.Parent = this;

        Root.Update();
    }

    public void SetCase(SyntaxToken caseToken, bool updatePositions = true)
    {
        caseToken.Parent = this;
        Case = caseToken;
        if (updatePositions)
            Root.Update();
    }

    public void SetLabel(ExpressionSyntax label, bool updatePositions = true)
    {
        label.Parent = this;
        Label = label;
        if (updatePositions)
            Root.Update();
    }

    public void SetColon(SyntaxToken colon, bool updatePositions = true)
    {
        colon.Parent = this;
        Colon = colon;
        if (updatePositions)
            Root.Update();
    }

    public void SetStatements(IReadOnlyList<StatementSyntax> statements, bool updatePositions = true)
    {
        Statements = statements ?? new List<StatementSyntax>();
        foreach (StatementSyntax statement in Statements)
            statement.Parent = this;

        if (updatePositions)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken caseToken = Case;
        SyntaxToken colon = Colon;

        position = caseToken.UpdatePosition(position, ref line, ref column);
        position = Label.UpdatePosition(position, ref line, ref column);
        position = colon.UpdatePosition(position, ref line, ref column);
        foreach (StatementSyntax statement in Statements)
            position = statement.UpdatePosition(position, ref line, ref column);

        Case = caseToken;
        Colon = colon;

        return position;
    }
}
