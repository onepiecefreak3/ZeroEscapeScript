namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft
{
    public class NameDeclarationSyntax : DeclarationSyntax
    {
        public SyntaxToken NameToken { get; private set; }
        public LiteralExpressionSyntax Name { get; private set; }
        public SyntaxToken Semicolon { get; private set; }

        public override SyntaxLocation Location => NameToken.Location;
        public override SyntaxSpan Span => new(NameToken.FullSpan.Position, Semicolon.FullSpan.EndPosition);

        public NameDeclarationSyntax(SyntaxToken nameToken, LiteralExpressionSyntax name, SyntaxToken semicolon)
        {
            nameToken.Parent = this;
            name.Parent = this;
            semicolon.Parent = this;

            NameToken = nameToken;
            Name = name;
            Semicolon = semicolon;

            Root.Update();
        }

        public void SetNameToken(SyntaxToken nameToken, bool updatePosition = true)
        {
            nameToken.Parent = this;
            NameToken = nameToken;

            if (updatePosition)
                Root.Update();
        }

        public void SetName(LiteralExpressionSyntax name, bool updatePosition = true)
        {
            name.Parent = this;
            Name = name;

            if (updatePosition)
                Root.Update();
        }

        public void SetSemicolon(SyntaxToken colon, bool updatePosition = true)
        {
            colon.Parent = this;
            Semicolon = colon;

            if (updatePosition)
                Root.Update();
        }

        internal override int UpdatePosition(int position, ref int line, ref int column)
        {
            SyntaxToken nameToken = NameToken;
            SyntaxToken semicolon = Semicolon;

            position = nameToken.UpdatePosition(position, ref line, ref column);
            position = Name.UpdatePosition(position, ref line, ref column);
            position = semicolon.UpdatePosition(position, ref line, ref column);

            NameToken = nameToken;
            Semicolon = semicolon;

            return position;
        }
    }
}
