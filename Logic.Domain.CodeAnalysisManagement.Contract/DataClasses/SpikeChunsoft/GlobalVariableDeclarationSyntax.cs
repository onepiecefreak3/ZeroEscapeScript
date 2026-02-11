using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft
{
    public class GlobalVariableDeclarationSyntax : DeclarationSyntax
    {
        public SyntaxToken Global { get; private set; }
        public LiteralExpressionSyntax Identifier { get; private set; }
        public SyntaxToken Semicolon { get; private set; }

        public override SyntaxLocation Location => Global.FullLocation;
        public override SyntaxSpan Span => new(Global.FullSpan.Position, Semicolon.FullSpan.EndPosition);

        public GlobalVariableDeclarationSyntax(SyntaxToken global, LiteralExpressionSyntax identifier, SyntaxToken semicolon)
        {
            global.Parent = this;
            identifier.Parent = this;
            semicolon.Parent = this;

            Global = global;
            Identifier = identifier;
            Semicolon = semicolon;

            Root.Update();
        }

        public void SetGlobal(SyntaxToken global, bool updatePosition = true)
        {
            global.Parent = this;
            Global = global;

            if (updatePosition)
                Root.Update();
        }

        public void SetSemicolon(SyntaxToken semicolon, bool updatePosition = true)
        {
            semicolon.Parent = this;
            Semicolon = semicolon;

            if (updatePosition)
                Root.Update();
        }

        internal override int UpdatePosition(int position, ref int line, ref int column)
        {
            SyntaxToken global = Global;
            SyntaxToken semicolon = Semicolon;

            position = global.UpdatePosition(position, ref line, ref column);
            position = Identifier.UpdatePosition(position, ref line, ref column);
            position = semicolon.UpdatePosition(position, ref line, ref column);

            Global = global;
            Semicolon = semicolon;

            return position;
        }
    }
}
