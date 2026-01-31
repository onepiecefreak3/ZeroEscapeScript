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
            case SyntaxTokenKind.Underscore: return new("_", (int)kind);
            case SyntaxTokenKind.EqualsSign: return new("=", (int)kind);
            case SyntaxTokenKind.Complement: return new("~", (int)kind);
            case SyntaxTokenKind.Minus: return new("-", (int)kind);
            case SyntaxTokenKind.Plus: return new("+", (int)kind);
            case SyntaxTokenKind.Mul: return new("*", (int)kind);
            case SyntaxTokenKind.Div: return new("/", (int)kind);
            case SyntaxTokenKind.Mod: return new("%", (int)kind);
            case SyntaxTokenKind.And: return new("&", (int)kind);
            case SyntaxTokenKind.AndAnd: return new("&&", (int)kind);
            case SyntaxTokenKind.Or: return new("|", (int)kind);
            case SyntaxTokenKind.OrOr: return new("||", (int)kind);
            case SyntaxTokenKind.Xor: return new("^", (int)kind);

            case SyntaxTokenKind.Equals: return new("==", (int)kind);
            case SyntaxTokenKind.NotEquals: return new("!=", (int)kind);
            case SyntaxTokenKind.SmallerEquals: return new("<=", (int)kind);
            case SyntaxTokenKind.GreaterEquals: return new(">=", (int)kind);
            case SyntaxTokenKind.PlusEquals: return new("+=", (int)kind);
            case SyntaxTokenKind.MinusEquals: return new("-=", (int)kind);
            case SyntaxTokenKind.MulEquals: return new("*=", (int)kind);
            case SyntaxTokenKind.DivEquals: return new("/=", (int)kind);
            case SyntaxTokenKind.ModEquals: return new("%=", (int)kind);
            case SyntaxTokenKind.AndEquals: return new("&=", (int)kind);
            case SyntaxTokenKind.OrEquals: return new("|=", (int)kind);
            case SyntaxTokenKind.XorEquals: return new("^=", (int)kind);
            case SyntaxTokenKind.LeftShiftEquals: return new("<<=", (int)kind);
            case SyntaxTokenKind.RightShiftEquals: return new(">>=", (int)kind);
            case SyntaxTokenKind.ArrowRight: return new("=>", (int)kind);
            case SyntaxTokenKind.Decrement: return new("--", (int)kind);
            case SyntaxTokenKind.Increment: return new("++", (int)kind);
            case SyntaxTokenKind.Smaller: return new("<", (int)kind);
            case SyntaxTokenKind.Greater: return new(">", (int)kind);
            case SyntaxTokenKind.LeftShift: return new("<<", (int)kind);
            case SyntaxTokenKind.RightShift: return new(">>", (int)kind);

            case SyntaxTokenKind.ParenOpen: return new("(", (int)kind);
            case SyntaxTokenKind.ParenClose: return new(")", (int)kind);
            case SyntaxTokenKind.CurlyOpen: return new("{", (int)kind);
            case SyntaxTokenKind.CurlyClose: return new("}", (int)kind);

            case SyntaxTokenKind.TrueKeyword: return new("true", (int)kind);
            case SyntaxTokenKind.FalseKeyword: return new("false", (int)kind);
            case SyntaxTokenKind.ReturnKeyword: return new("return", (int)kind);
            case SyntaxTokenKind.AsyncKeyword: return new("async", (int)kind);
            case SyntaxTokenKind.IfKeyword: return new("if", (int)kind);
            case SyntaxTokenKind.ElseKeyword: return new("else", (int)kind);
            case SyntaxTokenKind.DoKeyword: return new("do", (int)kind);
            case SyntaxTokenKind.WhileKeyword: return new("while", (int)kind);
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