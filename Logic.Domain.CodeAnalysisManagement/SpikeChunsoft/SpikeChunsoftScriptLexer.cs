using System.Text;
using Logic.Domain.CodeAnalysisManagement.Contract;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;
using Logic.Domain.CodeAnalysisManagement.Contract.Exceptions;
using Logic.Domain.CodeAnalysisManagement.DataClasses.SpikeChunsoft;

namespace Logic.Domain.CodeAnalysisManagement.SpikeChunsoft;

internal class SpikeChunsoftScriptLexer : ILexer<SpikeChunsoftSyntaxToken>
{
    private readonly StringBuilder _sb;
    private readonly IBuffer<int> _buffer;

    public bool IsEndOfInput => _buffer.IsEndOfInput;

    private int Line { get; set; } = 1;
    private int Column { get; set; } = 1;
    private int Position { get; set; }

    public SpikeChunsoftScriptLexer(IBuffer<int> buffer)
    {
        _sb = new StringBuilder();
        _buffer = buffer;
    }

    public SpikeChunsoftSyntaxToken Read()
    {
        if (!TryPeekChar(out char character))
            return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.EndOfFile, Position, Line, Column);

        switch (character)
        {
            case ',':
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.Comma, Position, Line, Column, $"{ReadChar()}");
            case ':':
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.Colon, Position, Line, Column, $"{ReadChar()}");
            case ';':
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.Semicolon, Position, Line, Column, $"{ReadChar()}");
            case '=':
                if (IsPeekedChar(1, '='))
                    return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.EqualsEquals, Position, Line, Column, $"{ReadChar()}{ReadChar()}");

                break;

            case '!':
                if (IsPeekedChar(1, '='))
                    return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.NotEquals, Position, Line, Column, $"{ReadChar()}{ReadChar()}");

                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.ExclamationPoint, Position, Line, Column, $"{ReadChar()}");

            case '(':
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.ParenOpen, Position, Line, Column, $"{ReadChar()}");
            case ')':
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.ParenClose, Position, Line, Column, $"{ReadChar()}");
            case '{':
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.CurlyOpen, Position, Line, Column, $"{ReadChar()}");
            case '}':
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.CurlyClose, Position, Line, Column, $"{ReadChar()}");
            case '[':
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.BracketOpen, Position, Line, Column, $"{ReadChar()}");
            case ']':
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.BracketClose, Position, Line, Column, $"{ReadChar()}");
            case '<':
                if (IsPeekedChar(1, '='))
                    return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.SmallerEquals, Position, Line, Column, $"{ReadChar()}{ReadChar()}");

                if (IsPeekedChar(1, '<'))
                    return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.ShiftLeft, Position, Line, Column, $"{ReadChar()}{ReadChar()}");

                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.SmallerThan, Position, Line, Column, $"{ReadChar()}");

            case '>':
                if (IsPeekedChar(1, '='))
                    return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.GreaterEquals, Position, Line, Column, $"{ReadChar()}{ReadChar()}");

                if (IsPeekedChar(1, '>'))
                    return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.ShiftRight, Position, Line, Column, $"{ReadChar()}{ReadChar()}");

                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.GreaterThan, Position, Line, Column, $"{ReadChar()}");

            case '*':
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.Asterisk, Position, Line, Column, $"{ReadChar()}");

            case '/':
                if (!IsPeekedChar(1, '/'))
                    return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.Slash, Position, Line, Column, $"{ReadChar()}");

                goto case ' ';

            case '%':
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.Percent, Position, Line, Column, $"{ReadChar()}");

            case '&':
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.Ampersand, Position, Line, Column, $"{ReadChar()}");

            case '|':
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.Pipe, Position, Line, Column, $"{ReadChar()}");

            case '^':
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.Caret, Position, Line, Column, $"{ReadChar()}");

            case '-':
                if (IsPeekedChar(1, '.') || (TryPeekChar(1, out character) && character is >= '0' and <= '9'))
                    goto case '.';

                if (IsPeekedChar(1, '-'))
                    return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.MinusMinus, Position, Line, Column, $"{ReadChar()}{ReadChar()}");

                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.Minus, Position, Line, Column, $"{ReadChar()}");

            case '+':
                if (IsPeekedChar(1, '+'))
                    return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.PlusPlus, Position, Line, Column, $"{ReadChar()}{ReadChar()}");

                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.Plus, Position, Line, Column, $"{ReadChar()}");

            case ' ':
            case '\t':
            case '\r':
            case '\n':
                return ReadTriviaAndComments();

            case '"':
                return ReadStringLiteral();

            case '.':
                if (TryPeekChar(1, out character) && character is >= '0' and <= '9')
                    goto case '0';

                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.Dot, Position, Line, Column, $"{ReadChar()}");

            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
                return ReadNumericLiteral();

            case 'a':
            case 'b':
            case 'c':
            case 'd':
            case 'e':
            case 'f':
            case 'g':
            case 'h':
            case 'i':
            case 'j':
            case 'k':
            case 'l':
            case 'm':
            case 'n':
            case 'o':
            case 'p':
            case 'q':
            case 'r':
            case 's':
            case 't':
            case 'u':
            case 'v':
            case 'w':
            case 'x':
            case 'y':
            case 'z':
            case 'A':
            case 'B':
            case 'C':
            case 'D':
            case 'E':
            case 'F':
            case 'G':
            case 'H':
            case 'I':
            case 'J':
            case 'K':
            case 'L':
            case 'M':
            case 'N':
            case 'O':
            case 'P':
            case 'Q':
            case 'R':
            case 'S':
            case 'T':
            case 'U':
            case 'V':
            case 'W':
            case 'X':
            case 'Y':
            case 'Z':
            case '_':
            case '@':
                return ReadIdentifierOrKeyword();
        }

        throw CreateException("Invalid character.");
    }

    private SpikeChunsoftSyntaxToken ReadTriviaAndComments()
    {
        int position = Position;
        int line = Line;
        int column = Column;

        _sb.Clear();

        while (TryPeekChar(out char character))
        {
            switch (character)
            {
                case '/':
                    if (IsPeekedChar(1, '/'))
                    {
                        _sb.Append(ReadChar());
                        _sb.Append(ReadChar());

                        while (!IsPeekedChar('\n'))
                            _sb.Append(ReadChar());

                        continue;
                    }

                    if (IsPeekedChar(1, '*'))
                    {
                        _sb.Append(ReadChar());
                        _sb.Append(ReadChar());

                        while (!IsPeekedChar('*') || !IsPeekedChar(1, '/'))
                            _sb.Append(ReadChar());

                        _sb.Append(ReadChar());
                        _sb.Append(ReadChar());

                        continue;
                    }

                    break;

                case ' ':
                case '\t':
                case '\r':
                case '\n':
                    _sb.Append(ReadChar());
                    continue;
            }

            break;
        }

        return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.Trivia, position, line, column, _sb.ToString());
    }

    private SpikeChunsoftSyntaxToken ReadStringLiteral()
    {
        int position = Position;
        int line = Line;
        int column = Column;

        _sb.Clear();

        if (!IsPeekedChar('"'))
            throw CreateException("Invalid string literal start.", "\"");

        _sb.Append(ReadChar());

        while (!IsPeekedChar('"'))
        {
            if (IsPeekedChar('\\'))
                _sb.Append(ReadChar());

            _sb.Append(ReadChar());
        }

        if (_buffer.IsEndOfInput)
            throw CreateException("Invalid string literal end.", "\"");

        _sb.Append(ReadChar());

        return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.StringLiteral, position, line, column, _sb.ToString());

    }

    private SpikeChunsoftSyntaxToken ReadNumericLiteral()
    {
        int position = Position;
        int line = Line;
        int column = Column;

        _sb.Clear();

        var isHex = false;
        var hasDot = false;
        int dotColumn = Column;
        var kind = SyntaxTokenKind.NumericLiteral;

        while (TryPeekChar(out char character))
        {
            switch (character)
            {
                case '0':
                    if (!IsPeekedChar(1, 'x'))
                        goto case '1';

                    if (_sb.Length != 0)
                        throw CreateException($"Invalid hex identifier in numeric literal {character} in numeric literal.");

                    _sb.Append(ReadChar());
                    _sb.Append(ReadChar());

                    isHex = true;
                    continue;

                case '-':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    _sb.Append(ReadChar());
                    continue;

                case 'E':
                    _sb.Append(ReadChar());
                    continue;

                case 'A':
                case 'B':
                case 'C':
                case 'D':
                case 'F':
                case 'a':
                case 'b':
                case 'c':
                case 'd':
                case 'e':
                    if (!isHex)
                        throw CreateException("Invalid character in numeric literal.");

                    _sb.Append(ReadChar());
                    continue;

                case '.':
                    if (hasDot)
                        throw CreateException("Invalid floating numeric literal with multiple dots.");

                    hasDot = true;
                    dotColumn = Column;

                    _sb.Append(ReadChar());
                    continue;

                case 'f':
                    if (isHex)
                        goto case 'A';

                    if (hasDot && dotColumn == Column - 1)
                        throw CreateException("Floating numeric value misses fractional part.");

                    kind = SyntaxTokenKind.FloatingNumericLiteral;

                    _sb.Append(ReadChar());
                    break;
            }

            break;
        }

        if (hasDot && kind != SyntaxTokenKind.FloatingNumericLiteral)
            kind = SyntaxTokenKind.FloatingNumericLiteral;

        if (hasDot && dotColumn == Column - 1)
            throw CreateException("Floating numeric value misses fractional part.");

        return new SpikeChunsoftSyntaxToken(kind, position, line, column, _sb.ToString());
    }

    private SpikeChunsoftSyntaxToken ReadIdentifierOrKeyword()
    {
        int position = Position;
        int line = Line;
        int column = Column;

        _sb.Clear();

        var firstChar = true;
        while (TryPeekChar(out char character))
        {
            switch (character)
            {
                case 'a':
                case 'b':
                case 'c':
                case 'd':
                case 'e':
                case 'f':
                case 'g':
                case 'h':
                case 'i':
                case 'j':
                case 'k':
                case 'l':
                case 'm':
                case 'n':
                case 'o':
                case 'p':
                case 'q':
                case 'r':
                case 's':
                case 't':
                case 'u':
                case 'v':
                case 'w':
                case 'x':
                case 'y':
                case 'z':
                case 'A':
                case 'B':
                case 'C':
                case 'D':
                case 'E':
                case 'F':
                case 'G':
                case 'H':
                case 'I':
                case 'J':
                case 'K':
                case 'L':
                case 'M':
                case 'N':
                case 'O':
                case 'P':
                case 'Q':
                case 'R':
                case 'S':
                case 'T':
                case 'U':
                case 'V':
                case 'W':
                case 'X':
                case 'Y':
                case 'Z':
                case '_':
                    firstChar = false;

                    _sb.Append(ReadChar());
                    continue;

                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    if (firstChar)
                        throw CreateException("Invalid identifier starting with numbers.");

                    firstChar = false;

                    _sb.Append(ReadChar());
                    continue;
            }

            if (firstChar)
                throw CreateException("Invalid identifier.");

            break;
        }

        var finalValue = _sb.ToString();
        switch (finalValue)
        {
            case "return":
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.ReturnKeyword, position, line, column, finalValue);

            case "goto":
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.GotoKeyword, position, line, column, finalValue);

            case "and":
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.AndKeyword, position, line, column, finalValue);

            case "or":
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.OrKeyword, position, line, column, finalValue);

            case "not":
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.NotKeyword, position, line, column, finalValue);

            case "if":
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.IfKeyword, position, line, column, finalValue);

            case "else":
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.ElseKeyword, position, line, column, finalValue);

            case "do":
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.DoKeyword, position, line, column, finalValue);

            case "while":
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.WhileKeyword, position, line, column, finalValue);

            case "async":
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.AsyncKeyword, position, line, column, finalValue);

            case "true":
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.TrueKeyword, position, line, column, finalValue);

            case "false":
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.FalseKeyword, position, line, column, finalValue);

            case "break":
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.BreakKeyword, position, line, column, finalValue);

            case "continue":
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.ContinueKeyword, position, line, column, finalValue);

            default:
                return new SpikeChunsoftSyntaxToken(SyntaxTokenKind.Identifier, position, line, column, finalValue);
        }
    }

    private bool IsPeekedChar(char expected)
    {
        return IsPeekedChar(0, expected);
    }

    private bool IsPeekedChar(int position, char expected)
    {
        return TryPeekChar(position, out char character) && character == expected;
    }

    private bool TryPeekChar(out char character)
    {
        return TryPeekChar(0, out character);
    }

    private bool TryPeekChar(int position, out char character)
    {
        character = default;

        int result = _buffer.Peek(position);
        if (result < 0)
            return false;

        character = (char)result;
        return true;
    }

    private char ReadChar()
    {
        int result = _buffer.Read();
        if (result < 0)
            throw CreateException("Could not read character.");

        if (result == '\n')
        {
            Line++;
            Column = 0;
        }

        if (result == '\t')
            Column += 3;

        Column++;
        Position++;

        return (char)result;
    }

    private Exception CreateException(string message, string? expected = null)
    {
        message = $"{message} (Line {Line}, Column {Column})";

        if (!string.IsNullOrEmpty(expected))
            message = $"{message} (Expected \"{expected}\")";

        throw new LexerException(message, Line, Column);
    }
}