using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft
{
    public class MemberAccessExpressionSyntax : ExpressionSyntax
    {
        public ParenthesizedExpressionSyntax Eval { get; private set; }
        public SyntaxToken Operator { get; private set; }
        public SyntaxToken Identifier { get; private set; }

        public override SyntaxLocation Location => Eval.Location;
        public override SyntaxSpan Span => new();

        public MemberAccessExpressionSyntax(ParenthesizedExpressionSyntax eval, SyntaxToken operatorToken, SyntaxToken identifier)
        {
            eval.Parent = this;
            operatorToken.Parent = this;
            identifier.Parent = this;

            Eval = eval;
            Operator = operatorToken;
            Identifier = identifier;

            Root.Update();
        }

        public void SetOperator(SyntaxToken operatorToken, bool updatePositions = true)
        {
            operatorToken.Parent = this;

            Operator = operatorToken;

            if (updatePositions)
                Root.Update();
        }

        public void SetIdentifier(SyntaxToken identifier, bool updatePositions = true)
        {
            identifier.Parent = this;

            Identifier = identifier;

            if (updatePositions)
                Root.Update();
        }

        internal override int UpdatePosition(int position, ref int line, ref int column)
        {
            SyntaxToken operatorToken = Operator;
            SyntaxToken identifier = Identifier;

            position = Eval.UpdatePosition(position, ref line, ref column);
            position = operatorToken.UpdatePosition(position, ref line, ref column);
            position = identifier.UpdatePosition(position, ref line, ref column);

            Operator = operatorToken;
            Identifier = identifier;

            return position;
        }
    }
}
