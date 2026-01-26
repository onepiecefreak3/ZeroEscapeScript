using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

namespace Logic.Domain.CodeAnalysisManagement.Contract.SpikeChunsoft;

public interface ISpikeChunsoftSyntaxFactory
{
    SyntaxToken Create(string text, int rawKind, SyntaxTokenTrivia? leadingTrivia = null, SyntaxTokenTrivia? trailingTrivia = null);

    SyntaxToken Token(SyntaxTokenKind kind);

    SyntaxToken NumericLiteral(long value);
    SyntaxToken HashNumericLiteral(ulong value);
    SyntaxToken HashStringLiteral(string text);
    SyntaxToken FloatingNumericLiteral(float value);
    SyntaxToken StringLiteral(string text);
    SyntaxToken Identifier(string text);
    SyntaxToken Variable(string name, uint slot);
}