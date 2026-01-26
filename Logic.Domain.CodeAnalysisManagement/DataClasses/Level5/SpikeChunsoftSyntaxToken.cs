using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

namespace Logic.Domain.CodeAnalysisManagement.DataClasses.Level5;

public struct SpikeChunsoftSyntaxToken
{
    public SyntaxTokenKind Kind { get; }
    public string Text { get; }

    public int Position { get; }
    public int Line { get; }
    public int Column { get; }

    public SpikeChunsoftSyntaxToken(SyntaxTokenKind kind, int position, int line, int column, string? text = null)
    {
        Text = text ?? string.Empty;
        Kind = kind;
        Position = position;
        Line = line;
        Column = column;
    }
}