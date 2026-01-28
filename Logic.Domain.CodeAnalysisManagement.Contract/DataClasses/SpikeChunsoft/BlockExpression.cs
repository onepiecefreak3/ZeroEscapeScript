using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft
{
    public class BlockExpression : SyntaxNode
    {
        public SyntaxToken CurlyOpen { get; private set; }
        public IReadOnlyList<StatementSyntax> Statements { get; private set; }
        public SyntaxToken CurlyClose { get; private set; }

        public override SyntaxLocation Location => CurlyOpen.FullLocation;
        public override SyntaxSpan Span => new(CurlyOpen.FullSpan.Position, CurlyClose.FullSpan.EndPosition);

        public BlockExpression(SyntaxToken curlyOpen, IReadOnlyList<StatementSyntax>? expressions, SyntaxToken curlyClose)
        {
            curlyOpen.Parent = this;
            curlyClose.Parent = this;

            CurlyOpen = curlyOpen;
            Statements = expressions ?? new List<StatementSyntax>();
            CurlyClose = curlyClose;

            foreach (StatementSyntax expression in Statements)
                expression.Parent = this;

            Root.Update();
        }

        public void SetCurlyOpen(SyntaxToken curlyOpen, bool updatePosition = true)
        {
            curlyOpen.Parent = this;
            CurlyOpen = curlyOpen;

            if (updatePosition)
                Root.Update();
        }

        public void SetExpressions(IReadOnlyList<StatementSyntax> expressions, bool updatePosition = true)
        {
            Statements = expressions;
            foreach (StatementSyntax expression in Statements)
                expression.Parent = this;

            if (updatePosition)
                Root.Update();
        }

        public void SetCurlyClose(SyntaxToken curlyClose, bool updatePosition = true)
        {
            curlyClose.Parent = this;
            CurlyClose = curlyClose;

            if (updatePosition)
                Root.Update();
        }

        internal override int UpdatePosition(int position, ref int line, ref int column)
        {
            SyntaxToken curlyOpen = CurlyOpen;
            SyntaxToken curlyClose = CurlyClose;

            position = curlyOpen.UpdatePosition(position, ref line, ref column);
            foreach (StatementSyntax expression in Statements)
                position = expression.UpdatePosition(position, ref line, ref column);
            position = curlyClose.UpdatePosition(position, ref line, ref column);

            CurlyOpen = curlyOpen;
            CurlyClose = curlyClose;

            return position;
        }
    }
}
