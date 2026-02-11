using Logic.Business.SpikeChunsoftScriptManagement.DataClasses.Conversion;
using Logic.Business.SpikeChunsoftScriptManagement.Enums.Conversion;
using Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Conversion;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;
using Logic.Domain.CodeAnalysisManagement.Contract.SpikeChunsoft;
using Logic.Domain.SpikeChunsoftManagement.Contract.DataClasses.Script;

namespace Logic.Business.SpikeChunsoftScriptManagement.Conversion;

internal class FsbScriptFileConverter : IFsbScriptFileConverter
{
    private readonly HighLevelSyntaxFactory _highLevelFactory;
    private readonly HighLevelSyntaxPatternDetector _highLevelDetector;
    private readonly ISpikeChunsoftSyntaxFactory _syntaxFactory;
    private readonly IBlockBuilder _blockBuilder;

    private readonly Dictionary<int, int> _operatorPrecedence = new()
    {
        [0x17] = 0,
        [0x18] = 0,
        [0x19] = 0,
        [0x15] = 1,
        [0x16] = 1,
        [0x13] = 2,
        [0x14] = 2,
        [0x1C] = 3,
        [0x1D] = 3,
        [0x1E] = 3,
        [0x1F] = 3,
        [0x1A] = 4,
        [0x1B] = 4,
        [0x0E] = 5,
        [0x10] = 6,
        [0x11] = 7,
        [0x0F] = 8,
        [0x12] = 9
    };

    private readonly Dictionary<SyntaxTokenKind, int> _tokenPrecedence = new()
    {
        [SyntaxTokenKind.Asterisk] = 0,
        [SyntaxTokenKind.Slash] = 0,
        [SyntaxTokenKind.Percent] = 0,
        [SyntaxTokenKind.Plus] = 1,
        [SyntaxTokenKind.Minus] = 1,
        [SyntaxTokenKind.ShiftLeft] = 2,
        [SyntaxTokenKind.ShiftRight] = 2,
        [SyntaxTokenKind.GreaterThan] = 3,
        [SyntaxTokenKind.GreaterEquals] = 3,
        [SyntaxTokenKind.SmallerThan] = 3,
        [SyntaxTokenKind.SmallerEquals] = 3,
        [SyntaxTokenKind.EqualsEquals] = 4,
        [SyntaxTokenKind.NotEquals] = 4,
        [SyntaxTokenKind.Ampersand] = 5,
        [SyntaxTokenKind.Caret] = 6,
        [SyntaxTokenKind.Pipe] = 7,
        [SyntaxTokenKind.AndKeyword] = 8,
        [SyntaxTokenKind.OrKeyword] = 9
    };

    public FsbScriptFileConverter(ISpikeChunsoftSyntaxFactory syntaxFactory, IBlockBuilder blockBuilder)
    {
        _syntaxFactory = syntaxFactory;
        _blockBuilder = blockBuilder;
        _highLevelFactory = new HighLevelSyntaxFactory(syntaxFactory);
        _highLevelDetector = new HighLevelSyntaxPatternDetector(_highLevelFactory, BuildStatementsRange, CreateStatementsFromBlock);
    }

    public CodeUnitSyntax CreateCodeUnit(Sir0Script script)
    {
        NameDeclarationSyntax name = CreateNameDeclaration(script);
        IReadOnlyList<DeclarationSyntax> methods = CreateMembers(script);

        return new CodeUnitSyntax(name, methods);
    }

    private NameDeclarationSyntax CreateNameDeclaration(Sir0Script script)
    {
        SyntaxToken nameToken = _syntaxFactory.Token(SyntaxTokenKind.NameKeyword);
        var nameLiteral = CreateStringLiteralExpression(script.Name);
        SyntaxToken semicolon = _syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new NameDeclarationSyntax(nameToken, nameLiteral, semicolon);
    }

    private IReadOnlyList<DeclarationSyntax> CreateMembers(Sir0Script script)
    {
        var result = new List<DeclarationSyntax>(script.Functions.Length);

        foreach (string globalVariable in script.GlobalVariables)
            result.Add(CreateGlobalVariableDeclaration(globalVariable));

        foreach (Sir0Function function in script.Functions)
            result.Add(CreateMethodDeclaration(function, script.ExportedLabels));

        return [.. result];
    }

    private GlobalVariableDeclarationSyntax CreateGlobalVariableDeclaration(string variableName)
    {
        SyntaxToken global = _syntaxFactory.Token(SyntaxTokenKind.GlobalKeyword);
        LiteralExpressionSyntax identifier = CreateStringLiteralExpression(variableName);
        SyntaxToken semicolon = _syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new GlobalVariableDeclarationSyntax(global, identifier, semicolon);
    }

    private MethodDeclarationSyntax CreateMethodDeclaration(Sir0Function function, string[] exportedLabels)
    {
        var name = CreateStringLiteralExpression(function.Name);
        var parameters = CreateMethodDeclarationParameters();
        var body = CreateMethodDeclarationBody(function, exportedLabels);

        return new MethodDeclarationSyntax(name, parameters, body);
    }

    private MethodDeclarationParametersSyntax CreateMethodDeclarationParameters()
    {
        SyntaxToken parenOpen = _syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        SyntaxToken parenClose = _syntaxFactory.Token(SyntaxTokenKind.ParenClose);

        return new MethodDeclarationParametersSyntax(parenOpen, null, parenClose);
    }

    private BlockExpression CreateMethodDeclarationBody(Sir0Function function, string[] exportedLabels)
    {
        SyntaxToken curlyOpen = _syntaxFactory.Token(SyntaxTokenKind.CurlyOpen);
        var expressions = CreateStatements(function, exportedLabels);
        SyntaxToken curlyClose = _syntaxFactory.Token(SyntaxTokenKind.CurlyClose);

        return new BlockExpression(curlyOpen, expressions, curlyClose);
    }

    private IReadOnlyList<StatementSyntax> CreateStatements(Sir0Function function, string[] exportedLabels)
    {
        return CreateStatements(function.Operations, exportedLabels);
    }

    private IReadOnlyList<StatementSyntax> CreateStatements(Sir0Operation[] operations, string[] exportedLabels)
    {
        IReadOnlyList<StatementBlock> blocks = _blockBuilder.Build(operations);
        return CreateStatements(blocks, exportedLabels);
    }

    private StatementSyntax? CreateStatement(Stack<ExpressionSyntax> expressionStack, Sir0Operation[] operations, ref int index, string[] exportedLabels)
    {
        while (index < operations.Length)
        {
            if (TryPushExpression(expressionStack, operations, ref index))
                continue;

            Sir0Operation operation = operations[index++];
            switch (operation.Command)
            {
                case 0x27:
                    ExpressionSyntax poppedExpression = expressionStack.Pop();
                    switch (poppedExpression)
                    {
                        case NativeMethodInvocationExpressionSyntax nativeInvocation:
                            return CreateNativeMethodInvocationStatement(nativeInvocation);

                        case AssignmentExpressionSyntax assignment:
                            return CreateAssignmentStatement(assignment);

                        case PostfixExpressionSyntax postfix:
                            return CreatePostfixStatement(postfix);
                    }

                    throw new InvalidOperationException("Could not create statement from expression.");

                case 0x2B:
                    return CreateAsyncBlockStatement(operations, ref index, exportedLabels);

                case 0x30:
                    return CreateReturnStatement();

                case 0x33:
                    return CreateGotoStatement(operation);

                case 0x34:
                    return CreateGotoLabelStatement(operation, exportedLabels);

                case 0x36:
                case 0x37:
                    return null;

                default:
                    return CreateMethodInvocationStatement(CreateName($"sub{operation.Command}"), operation);
            }
        }

        throw new InvalidOperationException("Could not finalize statement due to end of operations.");
    }

    private bool TryPushExpression(Stack<ExpressionSyntax> expressionStack, Sir0Operation[] operations, ref int index)
    {
        ExpressionSyntax expression;

        Sir0Operation operation = operations[index++];
        switch (operation.Command)
        {
            case 0x17:
            case 0x18:
            case 0x19:
            case 0x15:
            case 0x16:
            case 0x13:
            case 0x14:
            case 0x1C:
            case 0x1D:
            case 0x1E:
            case 0x1F:
            case 0x1A:
            case 0x1B:
            case 0x0E:
            case 0x10:
            case 0x11:
                expression = CreateBinaryExpression(expressionStack, operation.Command);
                break;

            case 0x0F:
            case 0x12:
                expression = CreateLogicalExpression(expressionStack, operation.Command);
                break;

            case 0x01:
            case 0x07:
            case 0x08:
            case 0x09:
                expression = CreateUnaryExpression(expressionStack, operation.Command);
                break;

            case 0x0A:
            case 0x0B:
                expression = CreatePostfixExpression(expressionStack, operation.Command);
                break;

            case 0x0C:
                expression = CreateArrayIndexExpression(expressionStack);
                break;

            case 0xF0:
                expression = CreateFloatingNumericLiteralExpression((float)operation.Arguments[0]);
                break;

            case 0xF1:
                expression = CreateStringLiteralExpression((string)operation.Arguments[0]);
                break;

            case 0xF4:
                if (operation.Arguments.Length is 1)
                {
                    expression = CreateStringLiteralExpression((string)operation.Arguments[0]);
                    break;
                }

                if (operation.Arguments[0] is not "?_eval_")
                {
                    expression = CreateStringLiteralExpression((string)operation.Arguments[0] + "::" + (string)operation.Arguments[1]);
                    break;
                }

                expression = CreateCompoundMemberAccessExpression(expressionStack, (string)operation.Arguments[1]);
                break;

            case 0x20:
            case 0x21:
            case 0x22:
                expression = CreateAssignmentExpression(expressionStack, operation.Command);
                break;

            case 0x23:
                Stack<ExpressionSyntax> argStack = new();

                while (index < operations.Length)
                {
                    if (TryPushExpression(argStack, operations, ref index))
                        continue;

                    if (operations[index++].Command is 0x24)
                        break;

                    throw new InvalidOperationException("Incomplete method invocation operation.");
                }

                expression = CreateNativeMethodInvocationExpression(expressionStack, argStack);
                break;

            default:
                index--;
                return false;
        }

        expressionStack.Push(expression);
        return true;
    }

    private BinaryExpressionSyntax CreateBinaryExpression(Stack<ExpressionSyntax> syntax, int command)
    {
        var right = syntax.Pop();
        var left = syntax.Pop();

        SyntaxToken operatorToken;
        switch (command)
        {
            case 0x0E:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.Ampersand);
                break;

            case 0x10:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.Caret);
                break;

            case 0x11:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.Pipe);
                break;

            case 0x13:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.ShiftRight);
                break;

            case 0x14:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.ShiftLeft);
                break;

            case 0x15:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.Plus);
                break;

            case 0x16:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.Minus);
                break;

            case 0x17:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.Asterisk);
                break;

            case 0x18:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.Slash);
                break;

            case 0x19:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.Percent);
                break;

            case 0x1A:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.EqualsEquals);
                break;

            case 0x1B:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.NotEquals);
                break;

            case 0x1C:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.SmallerEquals);
                break;

            case 0x1D:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.GreaterEquals);
                break;

            case 0x1E:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.SmallerThan);
                break;

            case 0x1F:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.GreaterThan);
                break;

            default:
                throw new InvalidOperationException($"Unknown binary command {command}.");
        }

        int currentPrecedence = _operatorPrecedence[command];

        if (left is LogicalExpressionSyntax)
            left = CreateParenthesizedExpression(left);
        else if (left is BinaryExpressionSyntax binary)
        {
            int leftPrecedence = _tokenPrecedence[(SyntaxTokenKind)binary.Operation.RawKind];

            if (currentPrecedence < leftPrecedence)
                left = CreateParenthesizedExpression(left);
        }

        if (right is LogicalExpressionSyntax)
            right = CreateParenthesizedExpression(right);
        else if (right is BinaryExpressionSyntax binary)
        {
            int rightPrecedence = _tokenPrecedence[(SyntaxTokenKind)binary.Operation.RawKind];

            if (currentPrecedence <= rightPrecedence)
                right = CreateParenthesizedExpression(right);
        }

        return new BinaryExpressionSyntax(left, operatorToken, right);
    }

    private LogicalExpressionSyntax CreateLogicalExpression(Stack<ExpressionSyntax> syntax, int command)
    {
        var right = syntax.Pop();
        var left = syntax.Pop();

        SyntaxToken operatorToken;
        switch (command)
        {
            case 0x0F:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.AndKeyword);
                break;

            case 0x12:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.OrKeyword);
                break;

            default:
                throw new InvalidOperationException($"Unknown logical command {command}.");
        }

        int currentPrecedence = _operatorPrecedence[command];

        if (left is LogicalExpressionSyntax logical)
        {
            int leftPrecedence = _tokenPrecedence[(SyntaxTokenKind)logical.Operation.RawKind];

            if (currentPrecedence < leftPrecedence)
                left = CreateParenthesizedExpression(left);
        }

        if (right is LogicalExpressionSyntax logical1)
        {
            int rightPrecedence = _tokenPrecedence[(SyntaxTokenKind)logical1.Operation.RawKind];

            if (currentPrecedence < rightPrecedence)
                right = CreateParenthesizedExpression(right);
        }

        return new LogicalExpressionSyntax(left, operatorToken, right);
    }

    private UnaryExpressionSyntax CreateUnaryExpression(Stack<ExpressionSyntax> syntax, int command)
    {
        ExpressionSyntax expression = syntax.Pop();

        if (expression is BinaryExpressionSyntax or LogicalExpressionSyntax or UnaryExpressionSyntax)
            expression = CreateParenthesizedExpression(expression);

        SyntaxToken operatorToken;
        switch (command)
        {
            case 0x01:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.Minus);
                break;

            case 0x07:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.ExclamationPoint);
                break;

            case 0x08:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.PlusPlus);
                break;

            case 0x09:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.MinusMinus);
                break;

            default:
                throw new InvalidOperationException($"Unknown unary command {command}.");
        }

        return new UnaryExpressionSyntax(operatorToken, expression);
    }

    private PostfixExpressionSyntax CreatePostfixExpression(Stack<ExpressionSyntax> syntax, int command)
    {
        ExpressionSyntax expression = syntax.Pop();

        if (expression is BinaryExpressionSyntax or LogicalExpressionSyntax)
            expression = CreateParenthesizedExpression(expression);

        SyntaxToken operatorToken;
        switch (command)
        {
            case 0x0A:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.PlusPlus);
                break;

            case 0x0B:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.MinusMinus);
                break;

            default:
                throw new InvalidOperationException($"Unknown postfix command {command}.");
        }

        return new PostfixExpressionSyntax(expression, operatorToken);
    }

    private ArrayIndexExpressionSyntax CreateArrayIndexExpression(Stack<ExpressionSyntax> syntax)
    {
        ExpressionSyntax index = syntax.Pop();
        ExpressionSyntax expression = syntax.Pop();

        if (expression is not MethodInvocationExpressionSyntax and not LiteralExpressionSyntax and not ArrayIndexExpressionSyntax)
            expression = CreateParenthesizedExpression(expression);

        return new ArrayIndexExpressionSyntax(expression, [CreateArrayIndexerExpression(index)]);
    }

    private ArrayIndexerExpressionSyntax CreateArrayIndexerExpression(ExpressionSyntax index)
    {
        SyntaxToken bracketOpen = _syntaxFactory.Token(SyntaxTokenKind.BracketOpen);
        SyntaxToken bracketClose = _syntaxFactory.Token(SyntaxTokenKind.BracketClose);

        return new ArrayIndexerExpressionSyntax(bracketOpen, index, bracketClose);
    }

    private ParenthesizedExpressionSyntax CreateParenthesizedExpression(ExpressionSyntax expression)
    {
        var parenOpen = _syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        var parenClose = _syntaxFactory.Token(SyntaxTokenKind.ParenClose);

        return new ParenthesizedExpressionSyntax(parenOpen, expression, parenClose);
    }

    private AssignmentExpressionSyntax CreateAssignmentExpression(Stack<ExpressionSyntax> syntax, int command)
    {
        var right = syntax.Pop();
        var left = syntax.Pop();

        SyntaxToken operatorToken;
        switch (command)
        {
            case 0x20:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.Equals);
                break;

            case 0x21:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.PlusEquals);
                break;

            case 0x22:
                operatorToken = _syntaxFactory.Token(SyntaxTokenKind.MinusEquals);
                break;

            default:
                throw new InvalidOperationException($"Unknown postfix command {command}.");
        }

        return new AssignmentExpressionSyntax(left, operatorToken, right);
    }

    private NativeMethodInvocationExpressionSyntax CreateNativeMethodInvocationExpression(Stack<ExpressionSyntax> syntax, Stack<ExpressionSyntax> args)
    {
        ExpressionSyntax nameExpression = syntax.Pop();

        if (nameExpression is LiteralExpressionSyntax { Literal.RawKind: (int)SyntaxTokenKind.StringLiteral } literal)
        {
            var texts = literal.Literal.Text[1..^1].Split("::");

            if (texts[0][0] is '?')
                texts[0] = texts[0][1..];

            if (texts.Length > 1)
                nameExpression = CreateQualifiedMemberAccessExpression(texts[0], texts[1]);
            else
                nameExpression = CreateSimpleMemberAccessExpression(texts[0]);
        }

        if (nameExpression is not MemberAccessExpressionSyntax)
            throw new InvalidOperationException("Need method name for invocation.");

        return new NativeMethodInvocationExpressionSyntax(nameExpression, CreateNativeMethodInvocationParameters([.. args.Reverse()]));
    }

    private SimpleMemberAccessExpressionSyntax CreateSimpleMemberAccessExpression(string name)
    {
        SyntaxToken identifier = _syntaxFactory.Identifier(name);

        return new SimpleMemberAccessExpressionSyntax(identifier);
    }

    private CompoundMemberAccessExpressionSyntax CreateCompoundMemberAccessExpression(Stack<ExpressionSyntax> syntax, string name)
    {
        var left = CreateParenthesizedExpression(syntax.Pop());
        SyntaxToken operatorToken = _syntaxFactory.Token(SyntaxTokenKind.ColonColon);
        SyntaxToken identifier = _syntaxFactory.Identifier(name);

        return new CompoundMemberAccessExpressionSyntax(left, operatorToken, identifier);
    }

    private QualifiedMemberAccessExpressionSyntax CreateQualifiedMemberAccessExpression(string nameSpace, string name)
    {
        SyntaxToken nameSpaceIdentifier = _syntaxFactory.Identifier(nameSpace);
        SyntaxToken operatorToken = _syntaxFactory.Token(SyntaxTokenKind.ColonColon);
        SyntaxToken identifier = _syntaxFactory.Identifier(name);

        return new QualifiedMemberAccessExpressionSyntax(nameSpaceIdentifier, operatorToken, identifier);
    }

    private AssignmentStatementSyntax CreateAssignmentStatement(AssignmentExpressionSyntax assignment)
    {
        SyntaxToken semicolon = _syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new AssignmentStatementSyntax(assignment, semicolon);
    }

    private PostfixStatementSyntax CreatePostfixStatement(PostfixExpressionSyntax postfix)
    {
        SyntaxToken semicolon = _syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new PostfixStatementSyntax(postfix, semicolon);
    }

    private ReturnStatementSyntax CreateReturnStatement(ExpressionSyntax? expression = null)
    {
        SyntaxToken returnToken = _syntaxFactory.Token(SyntaxTokenKind.ReturnKeyword);
        SyntaxToken semicolon = _syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new ReturnStatementSyntax(returnToken, expression, semicolon);
    }

    private GotoStatementSyntax CreateGotoStatement(Sir0Operation operation)
    {
        SyntaxToken gotoToken = _syntaxFactory.Token(SyntaxTokenKind.GotoKeyword);
        SyntaxToken semicolon = _syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        var label = CreateStringLiteralExpression((string)operation.Arguments[0]);

        return new GotoStatementSyntax(gotoToken, label, semicolon);
    }

    private StatementSyntax CreateGotoLabelStatement(Sir0Operation operation, string[] exportedLabels)
    {
        SyntaxToken colon = _syntaxFactory.Token(SyntaxTokenKind.Colon);

        var label = (string)operation.Arguments[0];
        var labelLiteral = CreateStringLiteralExpression(label);

        if (exportedLabels.Contains(label))
        {
            SyntaxToken export = _syntaxFactory.Token(SyntaxTokenKind.ExportKeyword);
            return new ExportedGotoLabelStatementSyntax(export, labelLiteral, colon);
        }

        return new GotoLabelStatementSyntax(labelLiteral, colon);
    }

    private AsyncBlockStatement CreateAsyncBlockStatement(Sir0Operation[] operations, ref int index, string[] exportedLabels)
    {
        for (var i = index; i < operations.Length; i++)
        {
            if (operations[i].Command is not 0x2C)
                continue;

            SyntaxToken asyncToken = _syntaxFactory.Token(SyntaxTokenKind.AsyncKeyword);
            var asyncStatements = CreateAsyncBlockBody(operations[index..i], exportedLabels);

            index = i + 1;

            return new AsyncBlockStatement(asyncToken, asyncStatements);
        }

        throw new InvalidOperationException("Incomplete async block.");
    }

    private BlockExpression CreateAsyncBlockBody(Sir0Operation[] operations, string[] exportedLabels)
    {
        SyntaxToken curlyOpen = _syntaxFactory.Token(SyntaxTokenKind.CurlyOpen);
        var statements = CreateStatements(operations, exportedLabels);
        SyntaxToken curlyClose = _syntaxFactory.Token(SyntaxTokenKind.CurlyClose);

        return new BlockExpression(curlyOpen, statements, curlyClose);
    }

    private MethodInvocationStatementSyntax CreateMethodInvocationStatement(NameSyntax methodName, Sir0Operation operation)
    {
        var method = CreateMethodInvocationExpression(methodName, operation);
        SyntaxToken semicolon = _syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new MethodInvocationStatementSyntax(method, semicolon);
    }

    private NativeMethodInvocationStatementSyntax CreateNativeMethodInvocationStatement(NativeMethodInvocationExpressionSyntax methodInvocation)
    {
        SyntaxToken semicolon = _syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new NativeMethodInvocationStatementSyntax(methodInvocation, semicolon);
    }

    private MethodInvocationExpressionSyntax CreateMethodInvocationExpression(NameSyntax methodName, Sir0Operation operation)
    {
        var parameters = CreateMethodInvocationParameters(operation);

        return new MethodInvocationExpressionSyntax(methodName, parameters);
    }

    private NativeMethodInvocationParametersSyntax CreateNativeMethodInvocationParameters(ExpressionSyntax[] arguments)
    {
        SyntaxToken parenOpen = _syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        var parameterList = new CommaSeparatedSyntaxList<ExpressionSyntax>(arguments);
        SyntaxToken parenClose = _syntaxFactory.Token(SyntaxTokenKind.ParenClose);

        return new NativeMethodInvocationParametersSyntax(parenOpen, parameterList, parenClose);
    }

    private MethodInvocationParametersSyntax CreateMethodInvocationParameters(Sir0Operation operation)
    {
        SyntaxToken parenOpen = _syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        var parameterList = CreateValueList(operation);
        SyntaxToken parenClose = _syntaxFactory.Token(SyntaxTokenKind.ParenClose);

        return new MethodInvocationParametersSyntax(parenOpen, parameterList, parenClose);
    }

    private CommaSeparatedSyntaxList<LiteralExpressionSyntax>? CreateValueList(Sir0Operation operation)
    {
        if (operation.Arguments.Length <= 0)
            return null;

        var result = new List<LiteralExpressionSyntax>();

        foreach (object argument in operation.Arguments)
        {
            switch (argument)
            {
                case float floatValue:
                    result.Add(CreateFloatingNumericLiteralExpression(floatValue));
                    break;

                case int intValue:
                    result.Add(CreateNumericLiteralExpression(intValue));
                    break;

                case string stringValue:
                    result.Add(CreateStringLiteralExpression(stringValue));
                    break;

                default:
                    throw new InvalidOperationException($"Unknown value type for operation 0x{operation.Command:X2}.");
            }
        }

        return new CommaSeparatedSyntaxList<LiteralExpressionSyntax>(result);
    }

    private LiteralExpressionSyntax CreateNumericLiteralExpression(int value)
    {
        return new LiteralExpressionSyntax(_syntaxFactory.NumericLiteral(value));
    }

    private LiteralExpressionSyntax CreateFloatingNumericLiteralExpression(float value)
    {
        return new LiteralExpressionSyntax(_syntaxFactory.FloatingNumericLiteral(value));
    }

    private LiteralExpressionSyntax CreateStringLiteralExpression(string value)
    {
        return new LiteralExpressionSyntax(_syntaxFactory.StringLiteral(value));
    }

    private NameSyntax CreateName(string name)
    {
        if (name.Contains('.'))
            return new SimpleNameSyntax(_syntaxFactory.Identifier(name));

        NameSyntax? result = null;

        foreach (string part in name.Split('.').Reverse())
        {
            if (result is null)
                result = new SimpleNameSyntax(_syntaxFactory.Identifier(part));
            else
                result = new QualifiedNameSyntax(new SimpleNameSyntax(_syntaxFactory.Identifier(part)), _syntaxFactory.Token(SyntaxTokenKind.Dot), result);
        }

        return result!;
    }

    private IReadOnlyList<StatementSyntax> CreateStatements(IReadOnlyList<StatementBlock> blocks, string[] exportedLabels)
    {
        if (blocks.Count == 0)
            return [];

        Dictionary<string, int> labelLookup = CreateLabelLookup(blocks);
        Dictionary<int, LoopBound> loopBounds = CreateLoopBounds(blocks, labelLookup);

        return BuildStatementsRange(blocks, labelLookup, loopBounds, 0, blocks.Count, exportedLabels, out _);
    }

    private Dictionary<string, int> CreateLabelLookup(IReadOnlyList<StatementBlock> blocks)
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < blocks.Count; i++)
        {
            foreach (string label in blocks[i].Labels)
            {
                if (!result.TryAdd(label, i))
                    throw new InvalidOperationException($"Duplicate jump label {label}.");
            }
        }

        return result;
    }

    private Dictionary<int, LoopBound> CreateLoopBounds(IReadOnlyList<StatementBlock> blocks, IReadOnlyDictionary<string, int> labelLookup)
    {
        var result = new Dictionary<int, LoopBound>();
        for (var i = 0; i < blocks.Count; i++)
        {
            StatementBlock block = blocks[i];
            if (block.TerminalCommand is not (0x35 or 0x36 or 0x37))
                continue;

            if (block.JumpLabel is null || !labelLookup.TryGetValue(block.JumpLabel, out int targetIndex))
                continue;

            if (targetIndex > i)
                continue;

            LoopConditionKind conditionKind = block.TerminalCommand switch
            {
                0x35 => LoopConditionKind.True,
                0x36 => LoopConditionKind.Not,
                0x37 => LoopConditionKind.Normal,
                _ => throw new InvalidOperationException($"Unknown terminal command 0x{block.TerminalCommand:X00}.")
            };

            _ = result.TryAdd(targetIndex, new LoopBound(i, conditionKind));
        }

        return result;
    }

    private IReadOnlyList<StatementSyntax> BuildStatementsRange(IReadOnlyList<StatementBlock> blocks,
        Dictionary<string, int> labelLookup, Dictionary<int, LoopBound> loopBounds, int startIndex, int endIndex,
        string[] exportedLabels, out ExpressionSyntax? condition, bool skipLoopStart = false, LoopContext? loopContext = null)
    {
        condition = null;

        var result = new List<StatementSyntax>();
        for (var i = startIndex; i < endIndex;)
        {
            if (!skipLoopStart && loopBounds.TryGetValue(i, out LoopBound loopBound) && loopBound.EndIndex < endIndex)
            {
                int exitIndex = Math.Min(loopBound.EndIndex + 1, blocks.Count);
                var nestedContext = new LoopContext(i, loopBound.EndIndex, exitIndex);
                IReadOnlyList<StatementSyntax> bodyStatements = BuildStatementsRange(blocks, labelLookup, loopBounds, i,
                    loopBound.EndIndex + 1, exportedLabels, out condition, true,
                    nestedContext);
                result.Add(_highLevelFactory.CreateLoopStatement(loopBound.ConditionKind, bodyStatements, condition));
                i = loopBound.EndIndex + 1;
                continue;
            }

            skipLoopStart = false;

            IReadOnlyList<StatementSyntax> statements = CreateStatementsFromBlock(blocks, labelLookup, loopContext, i, true, exportedLabels, out condition);
            result.AddRange(statements);
            if (_highLevelDetector.TryBuildSwitchStatement(blocks, labelLookup, loopBounds, i, endIndex, loopContext, statements, condition, exportedLabels,
                    out StatementSyntax? switchStatement, out int nextSwitchIndex, out int removeLeadingStatements))
            {
                if (removeLeadingStatements > 0)
                    result.RemoveRange(result.Count - removeLeadingStatements, removeLeadingStatements);

                result.Add(switchStatement);
                i = nextSwitchIndex;
                continue;
            }

            if (_highLevelDetector.TryBuildIfStatement(blocks, labelLookup, loopBounds, i, endIndex, loopContext, condition, exportedLabels,
                    out StatementSyntax? ifStatement, out int nextIndex))
            {
                if (_highLevelDetector.TryMergeLoopControlIf(result, ifStatement, out StatementSyntax mergedStatement))
                    result[^1] = mergedStatement;
                else
                    result.Add(ifStatement);
                i = nextIndex;
                continue;
            }

            i++;
        }

        return result;
    }

    private IReadOnlyList<StatementSyntax> CreateStatementsFromBlock(IReadOnlyList<StatementBlock> blocks,
        IReadOnlyDictionary<string, int>? labelLookup, LoopContext? loopContext, int blockIndex, bool skipTerminalJump,
        string[] exportedLabels, out ExpressionSyntax? condition)
    {
        condition = null;

        var result = new List<StatementSyntax>();
        StatementBlock block = blocks[blockIndex];
        Sir0Operation[] blockOperations = [.. block.Operations];
        int endIndex = blockOperations.Length;

        if (skipTerminalJump && block.TerminalCommand is 0x35 or 0x36 or 0x37 && endIndex > 0)
            endIndex--;

        if (loopContext is not null && labelLookup is not null && block.TerminalCommand is 0x35 or 0x36 or 0x37 &&
            block.JumpLabel is not null && labelLookup.TryGetValue(block.JumpLabel, out int targetIndex))
        {
            if (targetIndex == loopContext.Value.StartIndex && blockIndex != loopContext.Value.EndIndex)
            {
                result.AddRange(CreateStatementsFromBlock(blocks, labelLookup, null, blockIndex, true, exportedLabels, out condition));
                _highLevelFactory.AddLoopControlStatements(result, block.TerminalCommand.Value, LoopControlKind.Continue, condition);

                return result;
            }

            if (targetIndex == loopContext.Value.ExitIndex)
            {
                result.AddRange(CreateStatementsFromBlock(blocks, labelLookup, null, blockIndex, true, exportedLabels, out condition));
                _highLevelFactory.AddLoopControlStatements(result, block.TerminalCommand.Value, LoopControlKind.Break, condition);

                return result;
            }
        }

        Stack<ExpressionSyntax> expressionStack = new();

        for (var index = 0; index < endIndex;)
        {
            if (blockOperations[index].Command is 0x25 or 0x26)
            {
                index++;
                continue;
            }

            StatementSyntax? statement = CreateStatement(expressionStack, blockOperations, ref index, exportedLabels);
            if (statement is null)
                continue;

            result.Add(statement);
        }

        if (expressionStack.Count > 1)
            throw new InvalidOperationException("Too many expressions on stack.");

        if (expressionStack.Count > 0)
            condition = expressionStack.Pop();

        ApplyReturnAssignmentRewrite(result);

        return result;
    }

    private void ApplyReturnAssignmentRewrite(List<StatementSyntax> statements)
    {
        for (int i = 0; i < statements.Count - 1; i++)
        {
            if (statements[i] is not AssignmentStatementSyntax assignment)
                continue;

            if (statements[i + 1] is not ReturnStatementSyntax returnStatement || returnStatement.Expression != null)
                continue;

            if (!TryGetReturnAssignmentValue(assignment, out ExpressionSyntax? returnValue))
                continue;

            statements[i] = CreateReturnStatement(returnValue);
            statements.RemoveAt(i + 1);
        }
    }

    private static bool TryGetReturnAssignmentValue(AssignmentStatementSyntax assignment, out ExpressionSyntax? returnValue)
    {
        returnValue = null;

        if (assignment.Assignment.Operator.RawKind != (int)SyntaxTokenKind.Equals)
            return false;

        if (assignment.Assignment.Left is not LiteralExpressionSyntax targetLiteral)
            return false;

        if (!IsReturnTargetLiteral(targetLiteral))
            return false;

        returnValue = assignment.Assignment.Right;
        return true;
    }

    private static bool IsReturnTargetLiteral(LiteralExpressionSyntax literal)
    {
        return literal.Literal.Text.Equals("\"?4\"", StringComparison.Ordinal);
    }

}