namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class SwitchStatementSyntax : StatementSyntax
{
    public SyntaxToken Switch { get; private set; }
    public SyntaxToken ParenOpen { get; private set; }
    public ExpressionSyntax Expression { get; private set; }
    public SyntaxToken ParenClose { get; private set; }
    public SyntaxToken CurlyOpen { get; private set; }
    public IReadOnlyList<CaseStatementSyntax> Cases { get; private set; }
    public SyntaxToken CurlyClose { get; private set; }

    public override SyntaxLocation Location => Switch.FullLocation;
    public override SyntaxSpan Span => new(Switch.FullSpan.Position, CurlyClose.FullSpan.EndPosition);

    public SwitchStatementSyntax(SyntaxToken switchToken, SyntaxToken parenOpen, ExpressionSyntax expression, SyntaxToken parenClose,
        SyntaxToken curlyOpen, IReadOnlyList<CaseStatementSyntax> cases, SyntaxToken curlyClose)
    {
        switchToken.Parent = this;
        parenOpen.Parent = this;
        expression.Parent = this;
        parenClose.Parent = this;
        curlyOpen.Parent = this;
        curlyClose.Parent = this;

        Switch = switchToken;
        ParenOpen = parenOpen;
        Expression = expression;
        ParenClose = parenClose;
        CurlyOpen = curlyOpen;
        Cases = cases ?? new List<CaseStatementSyntax>();
        CurlyClose = curlyClose;

        foreach (CaseStatementSyntax @case in Cases)
            @case.Parent = this;

        Root.Update();
    }

    public void SetSwitch(SyntaxToken switchToken, bool updatePositions = true)
    {
        switchToken.Parent = this;
        Switch = switchToken;
        if (updatePositions)
            Root.Update();
    }

    public void SetParenOpen(SyntaxToken parenOpen, bool updatePositions = true)
    {
        parenOpen.Parent = this;
        ParenOpen = parenOpen;
        if (updatePositions)
            Root.Update();
    }

    public void SetParenClose(SyntaxToken parenClose, bool updatePositions = true)
    {
        parenClose.Parent = this;
        ParenClose = parenClose;
        if (updatePositions)
            Root.Update();
    }

    public void SetExpression(ExpressionSyntax expression, bool updatePositions = true)
    {
        expression.Parent = this;
        Expression = expression;
        if (updatePositions)
            Root.Update();
    }

    public void SetCurlyOpen(SyntaxToken curlyOpen, bool updatePositions = true)
    {
        curlyOpen.Parent = this;
        CurlyOpen = curlyOpen;
        if (updatePositions)
            Root.Update();
    }

    public void SetCases(IReadOnlyList<CaseStatementSyntax> cases, bool updatePositions = true)
    {
        Cases = cases ?? new List<CaseStatementSyntax>();
        foreach (CaseStatementSyntax @case in Cases)
            @case.Parent = this;
        if (updatePositions)
            Root.Update();
    }

    public void SetCurlyClose(SyntaxToken curlyClose, bool updatePositions = true)
    {
        curlyClose.Parent = this;
        CurlyClose = curlyClose;
        if (updatePositions)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken switchToken = Switch;
        SyntaxToken parenOpen = ParenOpen;
        SyntaxToken parenClose = ParenClose;
        SyntaxToken curlyOpen = CurlyOpen;
        SyntaxToken curlyClose = CurlyClose;

        position = switchToken.UpdatePosition(position, ref line, ref column);
        position = parenOpen.UpdatePosition(position, ref line, ref column);
        position = Expression.UpdatePosition(position, ref line, ref column);
        position = parenClose.UpdatePosition(position, ref line, ref column);
        position = curlyOpen.UpdatePosition(position, ref line, ref column);
        foreach (CaseStatementSyntax @case in Cases)
            position = @case.UpdatePosition(position, ref line, ref column);
        position = curlyClose.UpdatePosition(position, ref line, ref column);

        Switch = switchToken;
        ParenOpen = parenOpen;
        ParenClose = parenClose;
        CurlyOpen = curlyOpen;
        CurlyClose = curlyClose;

        return position;
    }
}
