namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public class SimpleMemberAccessExpressionSyntax : MemberAccessExpressionSyntax
{
    public override SyntaxLocation Location => Identifier.FullLocation;
    public override SyntaxSpan Span => Identifier.FullSpan;

    public SimpleMemberAccessExpressionSyntax(SyntaxToken identifier)
        : base(identifier)
    {
        Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken identifier = Identifier;

        position = identifier.UpdatePosition(position, ref line, ref column);

        Identifier = identifier;

        return position;
    }
}