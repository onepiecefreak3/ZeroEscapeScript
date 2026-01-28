using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft
{
    public class AsyncBlockStatement : StatementSyntax
    {
        public SyntaxToken Async { get; private set; }
        public BlockExpression Body { get; private set; }

        public override SyntaxLocation Location => Async.FullLocation;
        public override SyntaxSpan Span => new(Async.FullSpan.Position, Body.Span.EndPosition);

        public AsyncBlockStatement(SyntaxToken asyncToken, BlockExpression body)
        {
            asyncToken.Parent = this;
            body.Parent = this;

            Async = asyncToken;
            Body = body;

            Root.Update();
        }

        public void SetAsync(SyntaxToken asyncToken, bool updatePosition = true)
        {
            asyncToken.Parent = this;
            Async = asyncToken;

            if (updatePosition)
                Root.Update();
        }

        public void SetBody(BlockExpression body, bool updatePosition = true)
        {
            body.Parent = this;
            Body = body;

            if (updatePosition)
                Root.Update();
        }

        internal override int UpdatePosition(int position, ref int line, ref int column)
        {
            SyntaxToken asyncToken = Async;

            position = asyncToken.UpdatePosition(position, ref line, ref column);
            position = Body.UpdatePosition(position, ref line, ref column);

            Async = asyncToken;

            return position;
        }
    }
}
