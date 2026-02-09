namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public abstract class MemberAccessExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken Identifier { get; protected set; }

    public MemberAccessExpressionSyntax(SyntaxToken identifier)
    {
        identifier.Parent = this;

        Identifier = identifier;
    }

    public void SetIdentifier(SyntaxToken identifier, bool updatePositions = true)
    {
        identifier.Parent = this;

        Identifier = identifier;

        if (updatePositions)
            Root.Update();
    }
}