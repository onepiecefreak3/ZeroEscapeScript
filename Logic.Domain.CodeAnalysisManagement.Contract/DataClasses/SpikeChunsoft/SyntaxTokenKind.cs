namespace Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

public enum SyntaxTokenKind
{
    Dot,
    Comma,
    Colon,
    Semicolon,
    ExclamationPoint,
    ColonColon,

    Asterisk,
    Slash,
    Percent,
    Plus,
    PlusPlus,
    Minus,
    MinusMinus,
    Ampersand,
    Pipe,
    Caret,
    Equals,
    PlusEquals,
    MinusEquals,
    EqualsEquals,
    NotEquals,
    SmallerThan,
    SmallerEquals,
    GreaterThan,
    GreaterEquals,
    ShiftLeft,
    ShiftRight,

    ParenOpen,
    ParenClose,
    BracketOpen,
    BracketClose,
    CurlyOpen,
    CurlyClose,

    Trivia,

    StringLiteral,
    NumericLiteral,
    FloatingNumericLiteral,

    Identifier,

    ReturnKeyword,
    GotoKeyword,
    AndKeyword,
    OrKeyword,
    NotKeyword,
    IfKeyword,
    ElseKeyword,
    SwitchKeyword,
    CaseKeyword,
    DoKeyword,
    WhileKeyword,
    AsyncKeyword,
    TrueKeyword,
    FalseKeyword,
    BreakKeyword,
    ContinueKeyword,

    EndOfFile
}