using Logic.Domain.CodeAnalysisManagement.Contract;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;
using Logic.Domain.CodeAnalysisManagement.Contract.Exceptions.SpikeChunsoft;
using Logic.Domain.CodeAnalysisManagement.Contract.SpikeChunsoft;
using Logic.Domain.CodeAnalysisManagement.DataClasses.SpikeChunsoft;

namespace Logic.Domain.CodeAnalysisManagement.SpikeChunsoft;

internal class SpikeChunsoftScriptParser : ISpikeChunsoftScriptParser
{
    private readonly ITokenFactory<SpikeChunsoftSyntaxToken> _scriptFactory;
    private readonly ISpikeChunsoftSyntaxFactory _syntaxFactory;

    public SpikeChunsoftScriptParser(ITokenFactory<SpikeChunsoftSyntaxToken> scriptFactory, ISpikeChunsoftSyntaxFactory syntaxFactory)
    {
        _scriptFactory = scriptFactory;
        _syntaxFactory = syntaxFactory;
    }

    public CodeUnitSyntax ParseCodeUnit(string text)
    {
        IBuffer<SpikeChunsoftSyntaxToken> buffer = CreateTokenBuffer(text);

        return ParseCodeUnit(buffer);
    }

    private CodeUnitSyntax ParseCodeUnit(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        var methodDeclarations = ParseMethodDeclarations(buffer);

        return new CodeUnitSyntax(methodDeclarations);
    }

    private IReadOnlyList<MethodDeclarationSyntax> ParseMethodDeclarations(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        var result = new List<MethodDeclarationSyntax>();

        while (buffer.Peek().Kind != SyntaxTokenKind.EndOfFile)
            result.Add(ParseMethodDeclaration(buffer));

        return result;
    }

    private MethodDeclarationSyntax ParseMethodDeclaration(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        var name = ParseStringLiteralExpression(buffer);
        var parameters = ParseMethodDeclarationParameters(buffer);
        var body = ParseMethodDeclarationBody(buffer);

        return new MethodDeclarationSyntax(name, parameters, body);
    }

    private MethodDeclarationParametersSyntax ParseMethodDeclarationParameters(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        SyntaxToken parenOpenToken = ParseParenOpenToken(buffer);
        var parameterList = ParseMethodDeclarationParameterList(buffer);
        SyntaxToken parenCloseToken = ParseParenCloseToken(buffer);

        return new MethodDeclarationParametersSyntax(parenOpenToken, parameterList, parenCloseToken);
    }

    private CommaSeparatedSyntaxList<LiteralExpressionSyntax>? ParseMethodDeclarationParameterList(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        var result = new List<LiteralExpressionSyntax>();

        if (!HasTokenKind(buffer, SyntaxTokenKind.Variable))
            return null;

        LiteralExpressionSyntax variable = ParseLiteralExpression(buffer);
        result.Add(variable);

        while (HasTokenKind(buffer, SyntaxTokenKind.Comma))
        {
            SkipTokenKind(buffer, SyntaxTokenKind.Comma);

            if (!HasTokenKind(buffer, SyntaxTokenKind.Variable))
                throw CreateException(buffer, "Invalid end of parameter list.", SyntaxTokenKind.Variable);

            variable = ParseLiteralExpression(buffer);
            result.Add(variable);
        }

        return new CommaSeparatedSyntaxList<LiteralExpressionSyntax>(result);
    }

    private BlockExpression ParseMethodDeclarationBody(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        SyntaxToken curlyOpenToken = ParseCurlyOpenToken(buffer);
        var expressions = ParseStatements(buffer);
        SyntaxToken curlyCloseToken = ParseCurlyCloseToken(buffer);

        return new BlockExpression(curlyOpenToken, expressions, curlyCloseToken);
    }

    private IReadOnlyList<StatementSyntax> ParseStatements(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        var result = new List<StatementSyntax>();

        while (IsStatement(buffer))
            result.Add(ParseStatement(buffer));

        return result;
    }

    private bool IsStatement(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return HasTokenKind(buffer, SyntaxTokenKind.StringLiteral) ||
               HasTokenKind(buffer, SyntaxTokenKind.ReturnKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.AsyncKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.IfKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.DoKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.BreakKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.ContinueKeyword) ||
               IsMethodInvocation(buffer);
    }

    private bool IsMethodInvocation(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return HasTokenKind(buffer, SyntaxTokenKind.Identifier) &&
               HasTokenKind(buffer, 1, SyntaxTokenKind.ParenOpen);
    }

    private StatementSyntax ParseStatement(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        if (HasTokenKind(buffer, SyntaxTokenKind.ReturnKeyword))
            return ParseReturnStatement(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.StringLiteral))
            return ParseGotoLabelStatement(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.AsyncKeyword))
            return ParseAsyncBlockStatement(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.IfKeyword))
            return ParseIfStatement(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.DoKeyword))
            return ParseDoWhileStatement(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.BreakKeyword))
            return ParseBreakStatement(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.ContinueKeyword))
            return ParseContinueStatement(buffer);

        if (IsMethodInvocation(buffer))
            return ParseMethodInvocationStatement(buffer);

        throw CreateException(buffer, "Unknown statement.", SyntaxTokenKind.ReturnKeyword, SyntaxTokenKind.StringLiteral, SyntaxTokenKind.Identifier);
    }

    private ReturnStatementSyntax ParseReturnStatement(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        SyntaxToken returnToken = ParseReturnKeywordToken(buffer);
        SyntaxToken semicolon = ParseSemicolonToken(buffer);

        return new ReturnStatementSyntax(returnToken, null, semicolon);
    }

    private GotoLabelStatementSyntax ParseGotoLabelStatement(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        LiteralExpressionSyntax identifier = ParseStringLiteralExpression(buffer);
        SyntaxToken colon = ParseColonToken(buffer);

        return new GotoLabelStatementSyntax(identifier, colon);
    }

    private AsyncBlockStatement ParseAsyncBlockStatement(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        SyntaxToken asyncToken = ParseAsyncKeywordToken(buffer);
        BlockExpression body = ParseAsyncBlockBody(buffer);

        return new AsyncBlockStatement(asyncToken, body);
    }

    private StatementSyntax ParseIfStatement(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        SyntaxToken ifToken = ParseIfKeywordToken(buffer);
        SyntaxToken? notToken = null;
        if (HasTokenKind(buffer, SyntaxTokenKind.NotKeyword))
            notToken = ParseNotKeywordToken(buffer);
        SyntaxToken parenOpen = ParseParenOpenToken(buffer);
        LiteralExpressionSyntax condition = ParseLiteralExpression(buffer);
        SyntaxToken parenClose = ParseParenCloseToken(buffer);
        BlockExpression body = ParseBlockExpression(buffer);

        if (!HasTokenKind(buffer, SyntaxTokenKind.ElseKeyword))
        {
            if (notToken is null)
                return new IfStatementSyntax(ifToken, parenOpen, condition, parenClose, body);

            return new IfNotStatementSyntax(ifToken, notToken.Value, parenOpen, condition, parenClose, body);
        }

        SyntaxToken elseToken = ParseElseKeywordToken(buffer);
        BlockExpression elseBody = ParseBlockExpression(buffer);

        if (notToken is null)
            return new IfElseStatementSyntax(ifToken, parenOpen, condition, parenClose, body, elseToken, elseBody);

        return new IfNotElseStatementSyntax(ifToken, notToken.Value, parenOpen, condition, parenClose, body, elseToken, elseBody);
    }

    private StatementSyntax ParseDoWhileStatement(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        SyntaxToken doToken = ParseDoKeywordToken(buffer);
        BlockExpression body = ParseBlockExpression(buffer);
        SyntaxToken whileToken = ParseWhileKeywordToken(buffer);
        SyntaxToken? notToken = null;
        if (HasTokenKind(buffer, SyntaxTokenKind.NotKeyword))
            notToken = ParseNotKeywordToken(buffer);
        SyntaxToken parenOpen = ParseParenOpenToken(buffer);
        LiteralExpressionSyntax condition = ParseLiteralExpression(buffer);
        SyntaxToken parenClose = ParseParenCloseToken(buffer);
        SyntaxToken semicolon = ParseSemicolonToken(buffer);

        if (notToken is null)
            return new DoWhileStatementSyntax(doToken, body, whileToken, parenOpen, condition, parenClose, semicolon);

        return new DoWhileNotStatementSyntax(doToken, body, whileToken, notToken.Value, parenOpen, condition, parenClose, semicolon);
    }

    private StatementSyntax ParseBreakStatement(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        SyntaxToken breakToken = ParseBreakKeywordToken(buffer);
        SyntaxToken semicolon = ParseSemicolonToken(buffer);

        return new BreakStatementSyntax(breakToken, semicolon);
    }

    private StatementSyntax ParseContinueStatement(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        SyntaxToken continueToken = ParseContinueKeywordToken(buffer);
        SyntaxToken semicolon = ParseSemicolonToken(buffer);

        return new ContinueStatementSyntax(continueToken, semicolon);
    }

    private BlockExpression ParseAsyncBlockBody(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        SyntaxToken curlyOpenToken = ParseCurlyOpenToken(buffer);
        var expressions = ParseStatements(buffer);
        SyntaxToken curlyCloseToken = ParseCurlyCloseToken(buffer);

        return new BlockExpression(curlyOpenToken, expressions, curlyCloseToken);
    }

    private BlockExpression ParseBlockExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        SyntaxToken curlyOpenToken = ParseCurlyOpenToken(buffer);
        var expressions = ParseStatements(buffer);
        SyntaxToken curlyCloseToken = ParseCurlyCloseToken(buffer);

        return new BlockExpression(curlyOpenToken, expressions, curlyCloseToken);
    }

    private MethodInvocationStatementSyntax ParseMethodInvocationStatement(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        NameSyntax name = ParseName(buffer);
        var methodInvocationParameters = ParseMethodInvocationParameters(buffer);
        SyntaxToken semicolon = ParseSemicolonToken(buffer);

        return new MethodInvocationStatementSyntax(name, methodInvocationParameters, semicolon);
    }

    private MethodInvocationParametersSyntax ParseMethodInvocationParameters(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        SyntaxToken parenOpen = ParseParenOpenToken(buffer);
        var parameters = ParseValueList(buffer);
        SyntaxToken parenClose = ParseParenCloseToken(buffer);

        return new MethodInvocationParametersSyntax(parenOpen, parameters, parenClose);
    }

    private CommaSeparatedSyntaxList<LiteralExpressionSyntax>? ParseValueList(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        if (!IsValueExpression(buffer))
            return null;

        var result = new List<LiteralExpressionSyntax>();

        LiteralExpressionSyntax parameter = ParseLiteralExpression(buffer);
        result.Add(parameter);

        while (HasTokenKind(buffer, SyntaxTokenKind.Comma))
        {
            SkipTokenKind(buffer, SyntaxTokenKind.Comma);

            if (!IsValueExpression(buffer))
                throw CreateException(buffer, "Invalid end of parameter list.", SyntaxTokenKind.StringLiteral, SyntaxTokenKind.NumericLiteral,
                    SyntaxTokenKind.FloatingNumericLiteral);

            parameter = ParseLiteralExpression(buffer);
            result.Add(parameter);
        }

        return new CommaSeparatedSyntaxList<LiteralExpressionSyntax>(result);
    }

    private bool IsValueExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return HasTokenKind(buffer, SyntaxTokenKind.Variable) ||
               IsLiteralExpression(buffer);
    }

    private bool IsLiteralExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return HasTokenKind(buffer, SyntaxTokenKind.TrueKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.FalseKeyword) || 
               HasTokenKind(buffer, SyntaxTokenKind.StringLiteral) ||
               HasTokenKind(buffer, SyntaxTokenKind.NumericLiteral) ||
               HasTokenKind(buffer, SyntaxTokenKind.UnsignedNumericLiteral) ||
               HasTokenKind(buffer, SyntaxTokenKind.HashStringLiteral) ||
               HasTokenKind(buffer, SyntaxTokenKind.HashNumericLiteral) ||
               HasTokenKind(buffer, SyntaxTokenKind.FloatingNumericLiteral);
    }

    private LiteralExpressionSyntax ParseLiteralExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        if (HasTokenKind(buffer, SyntaxTokenKind.TrueKeyword))
            return ParseTrueLiteralExpression(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.FalseKeyword))
            return ParseFalseLiteralExpression(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.StringLiteral))
            return ParseStringLiteralExpression(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.NumericLiteral))
            return ParseNumericLiteralExpression(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.FloatingNumericLiteral))
            return ParseFloatingNumericLiteralExpression(buffer);

        throw CreateException(buffer, "Unknown value expression.", SyntaxTokenKind.StringLiteral, SyntaxTokenKind.NumericLiteral,
            SyntaxTokenKind.FloatingNumericLiteral);
    }

    private LiteralExpressionSyntax ParseTrueLiteralExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        SyntaxToken literal = ParseTrueKeywordToken(buffer);

        return new LiteralExpressionSyntax(literal);
    }

    private LiteralExpressionSyntax ParseFalseLiteralExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        SyntaxToken literal = ParseFalseKeywordToken(buffer);

        return new LiteralExpressionSyntax(literal);
    }

    private LiteralExpressionSyntax ParseStringLiteralExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        SyntaxToken literal = ParseStringLiteralToken(buffer);

        return new LiteralExpressionSyntax(literal);
    }

    private LiteralExpressionSyntax ParseNumericLiteralExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        SyntaxToken literal = ParseNumericLiteralToken(buffer);

        return new LiteralExpressionSyntax(literal);
    }

    private LiteralExpressionSyntax ParseFloatingNumericLiteralExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        SyntaxToken literal = ParseFloatingNumericLiteralToken(buffer);

        return new LiteralExpressionSyntax(literal);
    }

    private NameSyntax ParseName(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        if (!HasTokenKind(buffer, SyntaxTokenKind.Identifier))
            throw CreateException(buffer, "Invalid name syntax.", SyntaxTokenKind.Identifier);

        NameSyntax left = new SimpleNameSyntax(ParseIdentifierToken(buffer));
        if (!HasTokenKind(buffer, SyntaxTokenKind.Dot))
            return left;

        SyntaxToken dot = ParseDotToken(buffer);

        return new QualifiedNameSyntax(left, dot, ParseName(buffer));
    }

    private SyntaxToken ParseDotToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Dot);
    }

    private SyntaxToken ParseSemicolonToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Semicolon);
    }

    private SyntaxToken ParseColonToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Colon);
    }

    private SyntaxToken ParseParenOpenToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.ParenOpen);
    }

    private SyntaxToken ParseParenCloseToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.ParenClose);
    }

    private SyntaxToken ParseCurlyOpenToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.CurlyOpen);
    }

    private SyntaxToken ParseCurlyCloseToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.CurlyClose);
    }

    private SyntaxToken ParseReturnKeywordToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.ReturnKeyword);
    }

    private SyntaxToken ParseAsyncKeywordToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.AsyncKeyword);
    }

    private SyntaxToken ParseIfKeywordToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.IfKeyword);
    }

    private SyntaxToken ParseNotKeywordToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.NotKeyword);
    }

    private SyntaxToken ParseElseKeywordToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.ElseKeyword);
    }

    private SyntaxToken ParseDoKeywordToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.DoKeyword);
    }

    private SyntaxToken ParseWhileKeywordToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.WhileKeyword);
    }

    private SyntaxToken ParseBreakKeywordToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.BreakKeyword);
    }

    private SyntaxToken ParseContinueKeywordToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.ContinueKeyword);
    }

    private SyntaxToken ParseNumericLiteralToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.NumericLiteral);
    }

    private SyntaxToken ParseFloatingNumericLiteralToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.FloatingNumericLiteral);
    }

    private SyntaxToken ParseStringLiteralToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.StringLiteral);
    }

    private SyntaxToken ParseTrueKeywordToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.TrueKeyword);
    }

    private SyntaxToken ParseFalseKeywordToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.FalseKeyword);
    }

    private SyntaxToken ParseIdentifierToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Identifier);
    }

    private SyntaxToken CreateToken(IBuffer<SpikeChunsoftSyntaxToken> buffer, SyntaxTokenKind expectedKind)
    {
        SyntaxTokenTrivia? leadingTrivia = ReadTrivia(buffer);

        if (buffer.Peek().Kind != expectedKind)
            throw CreateException(buffer, $"Unexpected token {buffer.Peek().Kind}.", expectedKind);
        SpikeChunsoftSyntaxToken content = buffer.Read();

        SyntaxTokenTrivia? trailingTrivia = ReadTrivia(buffer);

        return _syntaxFactory.Create(content.Text, (int)expectedKind, leadingTrivia, trailingTrivia);
    }

    private SyntaxTokenTrivia? ReadTrivia(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        if (buffer.Peek().Kind == SyntaxTokenKind.Trivia)
        {
            SpikeChunsoftSyntaxToken token = buffer.Read();
            return new SyntaxTokenTrivia(token.Text);
        }

        return null;
    }

    private void SkipTokenKind(IBuffer<SpikeChunsoftSyntaxToken> buffer, SyntaxTokenKind expectedKind)
    {
        int toSkip = 1;

        SpikeChunsoftSyntaxToken peekedToken = buffer.Peek();
        if (peekedToken.Kind == SyntaxTokenKind.Trivia)
        {
            peekedToken = buffer.Peek(1);
            toSkip++;
        }

        if (peekedToken.Kind != expectedKind)
            throw CreateException(buffer, $"Unexpected token {peekedToken.Kind}.", expectedKind);

        for (var i = 0; i < toSkip; i++)
            buffer.Read();
    }

    protected bool HasTokenKind(IBuffer<SpikeChunsoftSyntaxToken> buffer, SyntaxTokenKind expectedKind)
    {
        return HasTokenKind(buffer, 0, expectedKind);
    }

    protected bool HasTokenKind(IBuffer<SpikeChunsoftSyntaxToken> buffer, int position, SyntaxTokenKind expectedKind)
    {
        var toPeek = 0;
        SpikeChunsoftSyntaxToken peekedToken = buffer.Peek(toPeek);

        position = Math.Max(0, position);
        for (var i = 0; i < position + 1; i++)
        {
            peekedToken = buffer.Peek(toPeek++);
            if (peekedToken.Kind == SyntaxTokenKind.Trivia)
                peekedToken = buffer.Peek(toPeek++);
        }

        return peekedToken.Kind == expectedKind;
    }

    private (int, int) GetCurrentLineAndColumn(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        var toPeek = 0;

        if (buffer.Peek().Kind == SyntaxTokenKind.Trivia)
            toPeek++;

        SpikeChunsoftSyntaxToken token = buffer.Peek(toPeek);
        return (token.Line, token.Column);
    }

    private IBuffer<SpikeChunsoftSyntaxToken> CreateTokenBuffer(string text)
    {
        ILexer<SpikeChunsoftSyntaxToken> lexer = _scriptFactory.CreateLexer(text);
        return _scriptFactory.CreateTokenBuffer(lexer);
    }

    private Exception CreateException(IBuffer<SpikeChunsoftSyntaxToken> buffer, string message, params SyntaxTokenKind[] expected)
    {
        (int line, int column) = GetCurrentLineAndColumn(buffer);
        return CreateException(message, line, column, expected);
    }

    private Exception CreateException(string message, int line, int column, params SyntaxTokenKind[] expected)
    {
        message = $"{message} (Line {line}, Column {column})";

        if (expected.Length > 0)
        {
            message = expected.Length == 1 ?
                $"{message} (Expected {expected[0]})" :
                $"{message} (Expected any of {string.Join(", ", expected)})";
        }

        throw new SpikeChunsoftScriptParserException(message, line, column);
    }
}