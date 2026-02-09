namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class QualifiedMemberAccessExpressionSyntax : MemberAccessExpressionSyntax
{
    public SyntaxToken NameSpace { get; private set; }
    public SyntaxToken Operator { get; private set; }

    public override SyntaxLocation Location => NameSpace.FullLocation;
    public override SyntaxSpan Span => new(NameSpace.FullSpan.Position, Identifier.FullSpan.EndPosition);

    public QualifiedMemberAccessExpressionSyntax(SyntaxToken nameSpace, SyntaxToken operatorToken, SyntaxToken identifier)
        : base(identifier)
    {
        nameSpace.Parent = this;
        operatorToken.Parent = this;

        NameSpace = nameSpace;
        Operator = operatorToken;

        Root.Update();
    }

    public void SetNameSpace(SyntaxToken nameSpace, bool updatePositions = true)
    {
        nameSpace.Parent = this;

        NameSpace = nameSpace;

        if (updatePositions)
            Root.Update();
    }

    public void SetOperator(SyntaxToken operatorToken, bool updatePositions = true)
    {
        operatorToken.Parent = this;

        Operator = operatorToken;

        if (updatePositions)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken nameSpace = NameSpace;
        SyntaxToken operatorToken = Operator;
        SyntaxToken identifier = Identifier;

        position = nameSpace.UpdatePosition(position, ref line, ref column);
        position = operatorToken.UpdatePosition(position, ref line, ref column);
        position = identifier.UpdatePosition(position, ref line, ref column);

        NameSpace = nameSpace;
        Operator = operatorToken;
        Identifier = identifier;

        return position;
    }
}