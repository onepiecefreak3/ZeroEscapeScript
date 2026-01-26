namespace Logic.Domain.CodeAnalysisManagement.Contract;

public interface ILexer<out TToken> where TToken : struct
{
    bool IsEndOfInput { get; }

    TToken Read();
}