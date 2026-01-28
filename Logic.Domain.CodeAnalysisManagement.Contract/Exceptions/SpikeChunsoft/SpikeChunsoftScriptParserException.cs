using System.Runtime.Serialization;

namespace Logic.Domain.CodeAnalysisManagement.Contract.Exceptions.SpikeChunsoft;

public class SpikeChunsoftScriptParserException : Exception
{
    public int Line { get; }
    public int Column { get; }

    public SpikeChunsoftScriptParserException()
    {
    }

    public SpikeChunsoftScriptParserException(string message) : base(message)
    {
    }

    public SpikeChunsoftScriptParserException(string message, Exception inner) : base(message, inner)
    {
    }

    public SpikeChunsoftScriptParserException(string message, int line, int column) : base(message)
    {
        Line = line;
        Column = column;
    }

    protected SpikeChunsoftScriptParserException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}