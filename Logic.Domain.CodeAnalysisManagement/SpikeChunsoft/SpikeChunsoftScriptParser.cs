using Logic.Domain.CodeAnalysisManagement.Contract;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;
using Logic.Domain.CodeAnalysisManagement.Contract.Exceptions.SpikeChunsoft;
using Logic.Domain.CodeAnalysisManagement.Contract.SpikeChunsoft;
using Logic.Domain.CodeAnalysisManagement.DataClasses.SpikeChunsoft;

namespace Logic.Domain.CodeAnalysisManagement.SpikeChunsoft;

internal class SpikeChunsoftScriptParser : ISpikeChunsoftScriptParser
{
    private readonly Dictionary<SyntaxTokenKind, int> _tokenPrecedence = new()
    {
        [SyntaxTokenKind.Asterisk] = 9,
        [SyntaxTokenKind.Slash] = 9,
        [SyntaxTokenKind.Percent] = 9,
        [SyntaxTokenKind.Plus] = 8,
        [SyntaxTokenKind.Minus] = 8,
        [SyntaxTokenKind.ShiftLeft] = 7,
        [SyntaxTokenKind.ShiftRight] = 7,
        [SyntaxTokenKind.GreaterThan] = 6,
        [SyntaxTokenKind.GreaterEquals] = 6,
        [SyntaxTokenKind.SmallerThan] = 6,
        [SyntaxTokenKind.SmallerEquals] = 6,
        [SyntaxTokenKind.EqualsEquals] = 5,
        [SyntaxTokenKind.NotEquals] = 5,
        [SyntaxTokenKind.Ampersand] = 4,
        [SyntaxTokenKind.Caret] = 3,
        [SyntaxTokenKind.Pipe] = 2,
        [SyntaxTokenKind.AndKeyword] = 1,
        [SyntaxTokenKind.OrKeyword] = 0
    };

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
        var nameDeclaration = ParseNameDeclaration(buffer);
        var methodDeclarations = ParseMethodDeclarations(buffer);

        return new CodeUnitSyntax(nameDeclaration, methodDeclarations);
    }

    private NameDeclarationSyntax ParseNameDeclaration(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        SyntaxToken nameKeyword = ParseNameKeyword(buffer);
        var literal = ParseStringLiteralExpression(buffer);
        SyntaxToken semicolon = ParseSemicolonToken(buffer);

        return new NameDeclarationSyntax(nameKeyword, literal, semicolon);
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

        if (!IsLiteralExpression(buffer))
            return null;

        LiteralExpressionSyntax variable = ParseLiteralExpression(buffer);
        result.Add(variable);

        while (HasTokenKind(buffer, SyntaxTokenKind.Comma))
        {
            SkipTokenKind(buffer, SyntaxTokenKind.Comma);

            if (!IsLiteralExpression(buffer))
                throw CreateException(buffer, "Invalid end of parameter list.");

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
        return HasTokenKind(buffer, SyntaxTokenKind.GotoKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.ExportKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.StringLiteral) ||
               HasTokenKind(buffer, SyntaxTokenKind.ReturnKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.AsyncKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.SwitchKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.IfKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.DoKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.BreakKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.ContinueKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.ParenOpen) ||
               IsMethodInvocation(buffer);
    }

    private bool IsMethodInvocation(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return HasTokenKind(buffer, SyntaxTokenKind.Identifier) &&
               HasTokenKind(buffer, 1, SyntaxTokenKind.ParenOpen);
    }

    private StatementSyntax ParseStatement(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        if (IsPostfixStatement(buffer))
            return ParsePostfixStatement(buffer);

        if (IsExportedGotoLabelStatement(buffer))
            return ParseExportedGotoLabelStatement(buffer);

        if (IsGotoLabelStatement(buffer))
            return ParseGotoLabelStatement(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.StringLiteral) || HasTokenKind(buffer, SyntaxTokenKind.ParenOpen))
        {
            var left = ParseAtomicExpression(buffer);

            if (IsAssignmentStatement(buffer))
                return ParseAssignmentStatement(buffer, left);

            if (left is NativeMethodInvocationExpressionSyntax invocation && HasTokenKind(buffer, SyntaxTokenKind.Semicolon))
                return ParseNativeMethodInvocationStatement(buffer, invocation);

            throw CreateException(buffer, "Unknown statement.", SyntaxTokenKind.Plus, SyntaxTokenKind.PlusEquals, SyntaxTokenKind.MinusEquals);
        }

        if (IsMethodInvocation(buffer))
            return ParseMethodInvocationStatement(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.GotoKeyword))
            return ParseGotoStatement(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.ReturnKeyword))
            return ParseReturnStatement(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.AsyncKeyword))
            return ParseAsyncBlockStatement(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.SwitchKeyword))
            return ParseSwitchStatement(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.IfKeyword))
            return ParseIfStatement(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.DoKeyword))
            return ParseDoWhileStatement(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.BreakKeyword))
            return ParseBreakStatement(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.ContinueKeyword))
            return ParseContinueStatement(buffer);

        throw CreateException(buffer, "Unknown statement.", SyntaxTokenKind.ReturnKeyword, SyntaxTokenKind.StringLiteral, SyntaxTokenKind.Identifier,
            SyntaxTokenKind.AsyncKeyword, SyntaxTokenKind.IfKeyword, SyntaxTokenKind.DoKeyword, SyntaxTokenKind.BreakKeyword, SyntaxTokenKind.ContinueKeyword);
    }

    private bool IsAssignmentStatement(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return HasTokenKind(buffer, SyntaxTokenKind.Equals) ||
                HasTokenKind(buffer, SyntaxTokenKind.PlusEquals) ||
                HasTokenKind(buffer, SyntaxTokenKind.MinusEquals);
    }

    private bool IsPostfixStatement(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return HasTokenKind(buffer, SyntaxTokenKind.StringLiteral) &&
               (HasTokenKind(buffer, 1, SyntaxTokenKind.PlusPlus) ||
                HasTokenKind(buffer, 1, SyntaxTokenKind.MinusMinus));
    }

    private bool IsExportedGotoLabelStatement(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return HasTokenKind(buffer, SyntaxTokenKind.ExportKeyword) &&
               HasTokenKind(buffer, 1, SyntaxTokenKind.StringLiteral) &&
               HasTokenKind(buffer, 2, SyntaxTokenKind.Colon);
    }

    private bool IsGotoLabelStatement(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return HasTokenKind(buffer, SyntaxTokenKind.StringLiteral) &&
               HasTokenKind(buffer, 1, SyntaxTokenKind.Colon);
    }

    private AssignmentStatementSyntax ParseAssignmentStatement(IBuffer<SpikeChunsoftSyntaxToken> buffer, ExpressionSyntax left)
    {
        var assignment = ParseAssignmentExpression(buffer, left);
        SyntaxToken semicolon = ParseSemicolonToken(buffer);

        return new AssignmentStatementSyntax(assignment, semicolon);
    }

    private NativeMethodInvocationStatementSyntax ParseNativeMethodInvocationStatement(IBuffer<SpikeChunsoftSyntaxToken> buffer, NativeMethodInvocationExpressionSyntax left)
    {
        SyntaxToken semicolon = ParseSemicolonToken(buffer);

        return new NativeMethodInvocationStatementSyntax(left, semicolon);
    }

    private PostfixStatementSyntax ParsePostfixStatement(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        var literal = ParseLiteralExpression(buffer);
        var postfix = ParsePostfixExpression(buffer, literal);
        SyntaxToken semicolon = ParseSemicolonToken(buffer);

        return new PostfixStatementSyntax(postfix, semicolon);
    }

    private PostfixExpressionSyntax ParsePostfixExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer, ExpressionSyntax left)
    {
        SyntaxToken operatorToken = ParsePostfixOperatorToken(buffer);

        return new PostfixExpressionSyntax(left, operatorToken);
    }

    private ReturnStatementSyntax ParseReturnStatement(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        SyntaxToken returnToken = ParseReturnKeywordToken(buffer);

        ExpressionSyntax? returnValue = null;
        if (!HasTokenKind(buffer, SyntaxTokenKind.Semicolon))
            returnValue = ParseExpression(buffer);

        SyntaxToken semicolon = ParseSemicolonToken(buffer);

        return new ReturnStatementSyntax(returnToken, returnValue, semicolon);
    }

    private ExportedGotoLabelStatementSyntax ParseExportedGotoLabelStatement(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        SyntaxToken export = ParseExportKeywordToken(buffer);
        LiteralExpressionSyntax identifier = ParseStringLiteralExpression(buffer);
        SyntaxToken colon = ParseColonToken(buffer);

        return new ExportedGotoLabelStatementSyntax(export, identifier, colon);
    }

    private GotoLabelStatementSyntax ParseGotoLabelStatement(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        LiteralExpressionSyntax identifier = ParseStringLiteralExpression(buffer);
        SyntaxToken colon = ParseColonToken(buffer);

        return new GotoLabelStatementSyntax(identifier, colon);
    }

    private GotoStatementSyntax ParseGotoStatement(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        SyntaxToken gotoToken = ParseGotoKeyword(buffer);
        LiteralExpressionSyntax identifier = ParseStringLiteralExpression(buffer);
        SyntaxToken semicolon = ParseSemicolonToken(buffer);

        return new GotoStatementSyntax(gotoToken, identifier, semicolon);
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
        ExpressionSyntax condition = ParseExpression(buffer);
        SyntaxToken parenClose = ParseParenCloseToken(buffer);
        BlockExpression body = ParseBlockExpression(buffer);

        if (!HasTokenKind(buffer, SyntaxTokenKind.ElseKeyword))
        {
            if (notToken is null)
                return new IfStatementSyntax(ifToken, parenOpen, condition, parenClose, body);

            return new IfNotStatementSyntax(ifToken, notToken.Value, parenOpen, condition, parenClose, body);
        }

        SyntaxToken elseToken = ParseElseKeywordToken(buffer);
        BlockExpression elseBody;
        if (HasTokenKind(buffer, SyntaxTokenKind.IfKeyword))
        {
            StatementSyntax elseStatement = ParseIfStatement(buffer);
            elseBody = CreateInlineBlockExpression(elseStatement);
        }
        else
        {
            elseBody = ParseBlockExpression(buffer);
        }

        if (notToken is null)
            return new IfElseStatementSyntax(ifToken, parenOpen, condition, parenClose, body, elseToken, elseBody);

        return new IfNotElseStatementSyntax(ifToken, notToken.Value, parenOpen, condition, parenClose, body, elseToken, elseBody);
    }

    private SwitchStatementSyntax ParseSwitchStatement(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        SyntaxToken switchToken = ParseSwitchKeywordToken(buffer);
        SyntaxToken parenOpen = ParseParenOpenToken(buffer);
        ExpressionSyntax expression = ParseExpression(buffer);
        SyntaxToken parenClose = ParseParenCloseToken(buffer);
        SyntaxToken curlyOpen = ParseCurlyOpenToken(buffer);
        IReadOnlyList<CaseStatementSyntax> cases = ParseCaseStatements(buffer);
        SyntaxToken curlyClose = ParseCurlyCloseToken(buffer);

        return new SwitchStatementSyntax(switchToken, parenOpen, expression, parenClose, curlyOpen, cases, curlyClose);
    }

    private IReadOnlyList<CaseStatementSyntax> ParseCaseStatements(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        var result = new List<CaseStatementSyntax>();

        while (HasTokenKind(buffer, SyntaxTokenKind.CaseKeyword))
            result.Add(ParseCaseStatement(buffer));

        return result;
    }

    private CaseStatementSyntax ParseCaseStatement(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        SyntaxToken caseToken = ParseCaseKeywordToken(buffer);
        ExpressionSyntax label = ParseExpression(buffer);
        SyntaxToken colon = ParseColonToken(buffer);
        IReadOnlyList<StatementSyntax> statements = ParseCaseStatementsBody(buffer);

        return new CaseStatementSyntax(caseToken, label, colon, statements);
    }

    private IReadOnlyList<StatementSyntax> ParseCaseStatementsBody(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        var result = new List<StatementSyntax>();

        while (IsStatement(buffer))
            result.Add(ParseStatement(buffer));

        return result;
    }

    private BlockExpression CreateInlineBlockExpression(StatementSyntax statement)
    {
        SyntaxToken curlyOpen = _syntaxFactory.Create(string.Empty, (int)SyntaxTokenKind.CurlyOpen);
        SyntaxToken curlyClose = _syntaxFactory.Create(string.Empty, (int)SyntaxTokenKind.CurlyClose);

        return new BlockExpression(curlyOpen, new List<StatementSyntax> { statement }, curlyClose);
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
        ExpressionSyntax condition = ParseExpression(buffer);
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
        var invocation = ParseMethodInvocationExpression(buffer);
        SyntaxToken semicolon = ParseSemicolonToken(buffer);

        return new MethodInvocationStatementSyntax(invocation, semicolon);
    }

    private MethodInvocationExpressionSyntax ParseMethodInvocationExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        NameSyntax name = ParseName(buffer);
        var methodInvocationParameters = ParseMethodInvocationParameters(buffer);

        return new MethodInvocationExpressionSyntax(name, methodInvocationParameters);
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
        if (!IsLiteralExpression(buffer))
            return null;

        var result = new List<LiteralExpressionSyntax>();

        LiteralExpressionSyntax parameter = ParseLiteralExpression(buffer);
        result.Add(parameter);

        while (HasTokenKind(buffer, SyntaxTokenKind.Comma))
        {
            SkipTokenKind(buffer, SyntaxTokenKind.Comma);

            if (!IsLiteralExpression(buffer))
                throw CreateException(buffer, "Invalid end of parameter list.");

            parameter = ParseLiteralExpression(buffer);
            result.Add(parameter);
        }

        return new CommaSeparatedSyntaxList<LiteralExpressionSyntax>(result);
    }

    private ExpressionSyntax ParseExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer, int minPrecedence = 0)
    {
        ExpressionSyntax left = ParseUnaryOrAtomicExpression(buffer);

        if (left is LiteralExpressionSyntax && IsAssignmentOperation(buffer))
            return ParseAssignmentExpression(buffer, left);

        while (IsCompoundExpression(buffer))
        {
            if (IsPostfixExpression(buffer))
            {
                if (minPrecedence > 11)
                    break;

                left = ParsePostfixExpression(buffer, left);
                continue;
            }

            int currentPrecedence = _tokenPrecedence[buffer.Peek().Kind];

            if (currentPrecedence < minPrecedence)
                break;

            bool isBinary = IsBinaryExpression(buffer);
            bool isLogical = IsLogicalExpression(buffer);

            SyntaxToken operatorToken = ParseOperatorToken(buffer);
            int newMinPrecedence = currentPrecedence + 1;

            ExpressionSyntax right = ParseExpression(buffer, newMinPrecedence);

            if (isBinary)
                left = new BinaryExpressionSyntax(left, operatorToken, right);
            else if (isLogical)
                left = new LogicalExpressionSyntax(left, operatorToken, right);
        }

        return left;
    }

    private ExpressionSyntax ParseUnaryOrAtomicExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        if (IsUnaryExpression(buffer))
        {
            SyntaxToken operatorToken = ParseUnaryOperatorToken(buffer);
            ExpressionSyntax expression = ParseExpression(buffer, 10);

            return new UnaryExpressionSyntax(operatorToken, expression);
        }

        return ParseAtomicExpression(buffer);
    }

    private SyntaxToken ParseOperatorToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        if (HasTokenKind(buffer, SyntaxTokenKind.Asterisk))
            return ParseAsteriskToken(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.Slash))
            return ParseSlashToken(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.Percent))
            return ParsePercentToken(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.Plus))
            return ParsePlusToken(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.Minus))
            return ParseMinusToken(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.ShiftLeft))
            return ParseShiftLeftToken(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.ShiftRight))
            return ParseShiftRightToken(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.GreaterThan))
            return ParseGreaterThanToken(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.GreaterEquals))
            return ParseGreaterEqualsToken(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.SmallerThan))
            return ParseSmallerThanToken(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.SmallerEquals))
            return ParseSmallerEqualsToken(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.EqualsEquals))
            return ParseEqualsEqualsToken(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.NotEquals))
            return ParseNotEqualsToken(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.Ampersand))
            return ParseAmpersandToken(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.Caret))
            return ParseCaretToken(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.Pipe))
            return ParsePipeToken(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.AndKeyword))
            return ParseAndKeywordToken(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.OrKeyword))
            return ParseOrKeywordToken(buffer);

        throw CreateException(buffer, "Invalid expression.", SyntaxTokenKind.Asterisk, SyntaxTokenKind.Slash, SyntaxTokenKind.Percent, SyntaxTokenKind.Plus,
            SyntaxTokenKind.Minus, SyntaxTokenKind.ShiftLeft, SyntaxTokenKind.ShiftRight, SyntaxTokenKind.GreaterThan, SyntaxTokenKind.GreaterEquals,
            SyntaxTokenKind.SmallerThan, SyntaxTokenKind.SmallerEquals, SyntaxTokenKind.EqualsEquals, SyntaxTokenKind.NotEquals, SyntaxTokenKind.Ampersand,
            SyntaxTokenKind.Caret, SyntaxTokenKind.Pipe, SyntaxTokenKind.AndKeyword, SyntaxTokenKind.OrKeyword);
    }

    private SyntaxToken ParseUnaryOperatorToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        if (HasTokenKind(buffer, SyntaxTokenKind.Minus))
            return ParseMinusToken(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.PlusPlus))
            return ParsePlusPlusToken(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.MinusMinus))
            return ParseMinusMinusToken(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.ExclamationPoint))
            return ParseExclamationPointToken(buffer);

        throw CreateException(buffer, "Invalid unary expression.", SyntaxTokenKind.Minus, SyntaxTokenKind.PlusPlus, SyntaxTokenKind.MinusMinus, SyntaxTokenKind.ExclamationPoint);
    }

    private SyntaxToken ParsePostfixOperatorToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        if (HasTokenKind(buffer, SyntaxTokenKind.PlusPlus))
            return ParsePlusPlusToken(buffer);

        if (HasTokenKind(buffer, SyntaxTokenKind.MinusMinus))
            return ParseMinusMinusToken(buffer);

        throw CreateException(buffer, "Invalid postfix expression.", SyntaxTokenKind.PlusPlus, SyntaxTokenKind.MinusMinus);
    }

    private bool IsAssignmentOperation(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return HasTokenKind(buffer, SyntaxTokenKind.Equals) ||
               HasTokenKind(buffer, SyntaxTokenKind.PlusEquals) ||
               HasTokenKind(buffer, SyntaxTokenKind.MinusEquals);
    }

    private bool IsCompoundExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return IsBinaryExpression(buffer) ||
               IsLogicalExpression(buffer) ||
               IsPostfixExpression(buffer);
    }

    private bool IsUnaryExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return (HasTokenKind(buffer, SyntaxTokenKind.Minus) && PeekTrivia(buffer, 1) is null) ||
               HasTokenKind(buffer, SyntaxTokenKind.PlusPlus) ||
               HasTokenKind(buffer, SyntaxTokenKind.MinusMinus) ||
               HasTokenKind(buffer, SyntaxTokenKind.ExclamationPoint);
    }

    private bool IsPostfixExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return HasTokenKind(buffer, SyntaxTokenKind.PlusPlus) ||
               HasTokenKind(buffer, SyntaxTokenKind.MinusMinus);
    }

    private bool IsBinaryExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return HasTokenKind(buffer, SyntaxTokenKind.Asterisk) ||
               HasTokenKind(buffer, SyntaxTokenKind.Slash) ||
               HasTokenKind(buffer, SyntaxTokenKind.Percent) ||
               HasTokenKind(buffer, SyntaxTokenKind.Plus) ||
               HasTokenKind(buffer, SyntaxTokenKind.Minus) ||
               HasTokenKind(buffer, SyntaxTokenKind.ShiftLeft) ||
               HasTokenKind(buffer, SyntaxTokenKind.ShiftRight) ||
               HasTokenKind(buffer, SyntaxTokenKind.GreaterThan) ||
               HasTokenKind(buffer, SyntaxTokenKind.GreaterEquals) ||
               HasTokenKind(buffer, SyntaxTokenKind.SmallerThan) ||
               HasTokenKind(buffer, SyntaxTokenKind.SmallerEquals) ||
               HasTokenKind(buffer, SyntaxTokenKind.EqualsEquals) ||
               HasTokenKind(buffer, SyntaxTokenKind.NotEquals) ||
               HasTokenKind(buffer, SyntaxTokenKind.Ampersand) ||
               HasTokenKind(buffer, SyntaxTokenKind.Caret) ||
               HasTokenKind(buffer, SyntaxTokenKind.Pipe);
    }

    private bool IsLogicalExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return HasTokenKind(buffer, SyntaxTokenKind.AndKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.OrKeyword);
    }

    private ExpressionSyntax ParseAtomicExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        if (IsParenthesizedExpression(buffer))
        {
            ParenthesizedExpressionSyntax eval = ParseParenthesizedExpression(buffer);

            if (HasTokenKind(buffer, SyntaxTokenKind.ColonColon))
            {
                ExpressionSyntax left = ParseMemberAccessExpression(buffer, eval);

                if (HasTokenKind(buffer, SyntaxTokenKind.ParenOpen))
                    return ParseNativeMethodInvocationExpression(buffer, left);

                return left;
            }

            return eval;
        }

        if (IsNativeMethodInvocation(buffer))
            return ParseNativeMethodInvocationExpression(buffer);

        if (IsLiteralExpression(buffer))
        {
            LiteralExpressionSyntax literal = ParseLiteralExpression(buffer);

            if (IsArrayIndexExpression(buffer))
                return ParseArrayIndexExpression(literal, buffer);

            return literal;
        }

        throw CreateException(buffer, "Invalid atomic expression.", SyntaxTokenKind.StringLiteral,
            SyntaxTokenKind.NumericLiteral, SyntaxTokenKind.ParenOpen);
    }

    private MemberAccessExpressionSyntax ParseMemberAccessExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer, ParenthesizedExpressionSyntax eval)
    {
        var operatorToken = ParseColonColonToken(buffer);
        var identifier = ParseIdentifierToken(buffer);

        return new MemberAccessExpressionSyntax(eval, operatorToken, identifier);
    }

    private bool IsNativeMethodInvocation(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return HasTokenKind(buffer, SyntaxTokenKind.StringLiteral) &&
               HasTokenKind(buffer, 1, SyntaxTokenKind.ParenOpen);
    }

    private AssignmentExpressionSyntax ParseAssignmentExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer, ExpressionSyntax left)
    {
        SyntaxToken operatorToken;
        if (HasTokenKind(buffer, SyntaxTokenKind.Equals))
            operatorToken = ParseEqualsToken(buffer);
        else if (HasTokenKind(buffer, SyntaxTokenKind.PlusEquals))
            operatorToken = ParsePlusEqualsToken(buffer);
        else if (HasTokenKind(buffer, SyntaxTokenKind.MinusEquals))
            operatorToken = ParseMinusEqualsToken(buffer);
        else
            throw CreateException(buffer, "Invalid assignment operation.", SyntaxTokenKind.Equals, SyntaxTokenKind.PlusEquals,
                SyntaxTokenKind.MinusEquals);

        return new AssignmentExpressionSyntax(left, operatorToken, ParseExpression(buffer));
    }

    private NativeMethodInvocationExpressionSyntax ParseNativeMethodInvocationExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        LiteralExpressionSyntax name = ParseLiteralExpression(buffer);

        return ParseNativeMethodInvocationExpression(buffer, name);
    }

    private NativeMethodInvocationExpressionSyntax ParseNativeMethodInvocationExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer, ExpressionSyntax name)
    {
        var parameters = ParseNativeMethodInvocationParameters(buffer);

        return new NativeMethodInvocationExpressionSyntax(name, parameters);
    }

    private NativeMethodInvocationParametersSyntax ParseNativeMethodInvocationParameters(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        SyntaxToken parenOpen = ParseParenOpenToken(buffer);
        var parameters = ParseNativeMethodParameters(buffer);
        SyntaxToken parenClose = ParseParenCloseToken(buffer);

        return new NativeMethodInvocationParametersSyntax(parenOpen, parameters, parenClose);
    }

    private CommaSeparatedSyntaxList<ExpressionSyntax>? ParseNativeMethodParameters(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        if (HasTokenKind(buffer, SyntaxTokenKind.ParenClose))
            return null;

        var result = new List<ExpressionSyntax>();

        ExpressionSyntax parameter = ParseExpression(buffer);
        result.Add(parameter);

        while (HasTokenKind(buffer, SyntaxTokenKind.Comma))
        {
            SkipTokenKind(buffer, SyntaxTokenKind.Comma);

            parameter = ParseExpression(buffer);
            result.Add(parameter);
        }

        return new CommaSeparatedSyntaxList<ExpressionSyntax>(result);
    }

    private bool IsArrayIndexExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return HasTokenKind(buffer, SyntaxTokenKind.BracketOpen);
    }

    private ArrayIndexExpressionSyntax ParseArrayIndexExpression(ExpressionSyntax arrayExpression, IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        var indexers = new List<ArrayIndexerExpressionSyntax>();

        while (IsArrayIndexExpression(buffer))
            indexers.Add(ParseArrayIndexerExpression(buffer));

        return new ArrayIndexExpressionSyntax(arrayExpression, indexers);
    }

    private ArrayIndexerExpressionSyntax ParseArrayIndexerExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        var bracketOpen = ParseBracketOpenToken(buffer);
        var index = ParseExpression(buffer);
        var bracketClose = ParseBracketCloseToken(buffer);

        return new ArrayIndexerExpressionSyntax(bracketOpen, index, bracketClose);
    }

    private bool IsParenthesizedExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return HasTokenKind(buffer, SyntaxTokenKind.ParenOpen);
    }

    private ParenthesizedExpressionSyntax ParseParenthesizedExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        var parenOpen = ParseParenOpenToken(buffer);
        var expression = ParseExpression(buffer);
        var parenClose = ParseParenCloseToken(buffer);

        return new ParenthesizedExpressionSyntax(parenOpen, expression, parenClose);
    }

    private bool IsLiteralExpression(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return HasTokenKind(buffer, SyntaxTokenKind.TrueKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.FalseKeyword) ||
               HasTokenKind(buffer, SyntaxTokenKind.StringLiteral) ||
               HasTokenKind(buffer, SyntaxTokenKind.NumericLiteral) ||
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

    private SyntaxToken ParseColonColonToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.ColonColon);
    }

    private SyntaxToken ParseAsteriskToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Asterisk);
    }

    private SyntaxToken ParseSlashToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Slash);
    }

    private SyntaxToken ParsePercentToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Percent);
    }

    private SyntaxToken ParsePlusToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Plus);
    }

    private SyntaxToken ParseMinusToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Minus);
    }

    private SyntaxToken ParsePlusPlusToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.PlusPlus);
    }

    private SyntaxToken ParseMinusMinusToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.MinusMinus);
    }

    private SyntaxToken ParseExclamationPointToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.ExclamationPoint);
    }

    private SyntaxToken ParseShiftLeftToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.ShiftLeft);
    }

    private SyntaxToken ParseShiftRightToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.ShiftRight);
    }

    private SyntaxToken ParseGreaterThanToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.GreaterThan);
    }

    private SyntaxToken ParseGreaterEqualsToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.GreaterEquals);
    }

    private SyntaxToken ParseSmallerThanToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.SmallerThan);
    }

    private SyntaxToken ParseSmallerEqualsToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.SmallerEquals);
    }

    private SyntaxToken ParseEqualsEqualsToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.EqualsEquals);
    }

    private SyntaxToken ParseNotEqualsToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.NotEquals);
    }

    private SyntaxToken ParseEqualsToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Equals);
    }

    private SyntaxToken ParsePlusEqualsToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.PlusEquals);
    }

    private SyntaxToken ParseMinusEqualsToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.MinusEquals);
    }

    private SyntaxToken ParseAmpersandToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Ampersand);
    }

    private SyntaxToken ParseCaretToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Caret);
    }

    private SyntaxToken ParsePipeToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.Pipe);
    }

    private SyntaxToken ParseParenOpenToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.ParenOpen);
    }

    private SyntaxToken ParseParenCloseToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.ParenClose);
    }

    private SyntaxToken ParseBracketOpenToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.BracketOpen);
    }

    private SyntaxToken ParseBracketCloseToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.BracketClose);
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

    private SyntaxToken ParseSwitchKeywordToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.SwitchKeyword);
    }

    private SyntaxToken ParseCaseKeywordToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.CaseKeyword);
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

    private SyntaxToken ParseExportKeywordToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.ExportKeyword);
    }

    private SyntaxToken ParseTrueKeywordToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.TrueKeyword);
    }

    private SyntaxToken ParseFalseKeywordToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.FalseKeyword);
    }

    private SyntaxToken ParseAndKeywordToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.AndKeyword);
    }

    private SyntaxToken ParseOrKeywordToken(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.OrKeyword);
    }

    private SyntaxToken ParseGotoKeyword(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.GotoKeyword);
    }

    private SyntaxToken ParseNameKeyword(IBuffer<SpikeChunsoftSyntaxToken> buffer)
    {
        return CreateToken(buffer, SyntaxTokenKind.NameKeyword);
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

    private SyntaxTokenTrivia? PeekTrivia(IBuffer<SpikeChunsoftSyntaxToken> buffer, int position)
    {
        if (buffer.Peek(position).Kind == SyntaxTokenKind.Trivia)
        {
            SpikeChunsoftSyntaxToken token = buffer.Peek(position);
            return new SyntaxTokenTrivia(token.Text);
        }

        return null;
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