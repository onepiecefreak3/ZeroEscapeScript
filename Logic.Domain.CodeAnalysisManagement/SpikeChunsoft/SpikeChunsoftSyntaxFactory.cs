using System.Globalization;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;
using Logic.Domain.CodeAnalysisManagement.Contract.SpikeChunsoft;

namespace Logic.Domain.CodeAnalysisManagement.SpikeChunsoft;

internal class SpikeChunsoftSyntaxFactory : ISpikeChunsoftSyntaxFactory
{
    public SyntaxToken Create(string text, int rawKind, SyntaxTokenTrivia? leadingTrivia = null, SyntaxTokenTrivia? trailingTrivia = null)
    {
        return new(text, rawKind, leadingTrivia, trailingTrivia);
    }

    public SyntaxToken Token(SyntaxTokenKind kind)
    {
        switch (kind)
        {
            case SyntaxTokenKind.Dot: return new(".", (int)kind);
            case SyntaxTokenKind.Comma: return new(",", (int)kind);
            case SyntaxTokenKind.Colon: return new(":", (int)kind);
            case SyntaxTokenKind.Semicolon: return new(";", (int)kind);
            case SyntaxTokenKind.ExclamationPoint: return new("!", (int)kind);
            case SyntaxTokenKind.ColonColon: return new("::", (int)kind);

            case SyntaxTokenKind.Asterisk: return new("*", (int)kind);
            case SyntaxTokenKind.Slash: return new("/", (int)kind);
            case SyntaxTokenKind.Percent: return new("%", (int)kind);
            case SyntaxTokenKind.Plus: return new("+", (int)kind);
            case SyntaxTokenKind.PlusPlus: return new("++", (int)kind);
            case SyntaxTokenKind.Minus: return new("-", (int)kind);
            case SyntaxTokenKind.MinusMinus: return new("--", (int)kind);
            case SyntaxTokenKind.Ampersand: return new("&", (int)kind);
            case SyntaxTokenKind.Pipe: return new("|", (int)kind);
            case SyntaxTokenKind.Caret: return new("^", (int)kind);
            case SyntaxTokenKind.Equals: return new("=", (int)kind);
            case SyntaxTokenKind.PlusEquals: return new("+=", (int)kind);
            case SyntaxTokenKind.MinusEquals: return new("-=", (int)kind);
            case SyntaxTokenKind.EqualsEquals: return new("==", (int)kind);
            case SyntaxTokenKind.NotEquals: return new("!=", (int)kind);
            case SyntaxTokenKind.SmallerThan: return new("<", (int)kind);
            case SyntaxTokenKind.SmallerEquals: return new("<=", (int)kind);
            case SyntaxTokenKind.GreaterThan: return new(">", (int)kind);
            case SyntaxTokenKind.GreaterEquals: return new(">=", (int)kind);
            case SyntaxTokenKind.ShiftLeft: return new("<<", (int)kind);
            case SyntaxTokenKind.ShiftRight: return new(">>", (int)kind);

            case SyntaxTokenKind.ParenOpen: return new("(", (int)kind);
            case SyntaxTokenKind.ParenClose: return new(")", (int)kind);
            case SyntaxTokenKind.BracketOpen: return new("[", (int)kind);
            case SyntaxTokenKind.BracketClose: return new("]", (int)kind);
            case SyntaxTokenKind.CurlyOpen: return new("{", (int)kind);
            case SyntaxTokenKind.CurlyClose: return new("}", (int)kind);

            case SyntaxTokenKind.ReturnKeyword: return new("return", (int)kind);
            case SyntaxTokenKind.GotoKeyword: return new("goto", (int)kind);
            case SyntaxTokenKind.AndKeyword: return new("and", (int)kind);
            case SyntaxTokenKind.OrKeyword: return new("or", (int)kind);
            case SyntaxTokenKind.NotKeyword: return new("not", (int)kind);
            case SyntaxTokenKind.IfKeyword: return new("if", (int)kind);
            case SyntaxTokenKind.ElseKeyword: return new("else", (int)kind);
            case SyntaxTokenKind.SwitchKeyword: return new("switch", (int)kind);
            case SyntaxTokenKind.CaseKeyword: return new("case", (int)kind);
            case SyntaxTokenKind.DoKeyword: return new("do", (int)kind);
            case SyntaxTokenKind.WhileKeyword: return new("while", (int)kind);
            case SyntaxTokenKind.AsyncKeyword: return new("async", (int)kind);
            case SyntaxTokenKind.TrueKeyword: return new("true", (int)kind);
            case SyntaxTokenKind.FalseKeyword: return new("false", (int)kind);
            case SyntaxTokenKind.BreakKeyword: return new("break", (int)kind);
            case SyntaxTokenKind.ContinueKeyword: return new("continue", (int)kind);
            default: throw new InvalidOperationException($"Cannot create simple token from kind {kind}. Use other methods instead.");
        }
    }

    public SyntaxToken NumericLiteral(long value)
    {
        return new($"{value}", (int)SyntaxTokenKind.NumericLiteral);
    }

    public SyntaxToken FloatingNumericLiteral(float value)
    {
        return new($"{value.ToString(CultureInfo.GetCultureInfo("en-gb"))}f", (int)SyntaxTokenKind.FloatingNumericLiteral);
    }

    public SyntaxToken StringLiteral(string text)
    {
        return new($"\"{text.Replace("\"", "\\\"")}\"", (int)SyntaxTokenKind.StringLiteral);
    }

    public SyntaxToken Identifier(string text)
    {
        return new(text, (int)SyntaxTokenKind.Identifier);
    }
}