using Logic.Business.SpikeChunsoftScriptManagement.DataClasses.Conversion;
using Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Conversion;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;
using Logic.Domain.CodeAnalysisManagement.Contract.SpikeChunsoft;
using Logic.Domain.SpikeChunsoftManagement.Contract.DataClasses.Script;
using System.Diagnostics.CodeAnalysis;

namespace Logic.Business.SpikeChunsoftScriptManagement.Conversion;

internal class FsbScriptFileConverter(ISpikeChunsoftSyntaxFactory syntaxFactory, IBlockBuilder blockBuilder) : IFsbScriptFileConverter
{
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

    public CodeUnitSyntax CreateCodeUnit(Sir0Function[] functions)
    {
        IReadOnlyList<MethodDeclarationSyntax> methods = CreateMethodDeclarations(functions);

        return new CodeUnitSyntax(methods);
    }

    private IReadOnlyList<MethodDeclarationSyntax> CreateMethodDeclarations(Sir0Function[] functions)
    {
        var result = new List<MethodDeclarationSyntax>(functions.Length);

        foreach (Sir0Function function in functions)
            result.Add(CreateMethodDeclaration(function));

        return [.. result];
    }

    private MethodDeclarationSyntax CreateMethodDeclaration(Sir0Function function)
    {
        var name = CreateStringLiteralExpression(function.Name);
        var parameters = CreateMethodDeclarationParameters();
        var body = CreateMethodDeclarationBody(function);

        return new MethodDeclarationSyntax(name, parameters, body);
    }

    private MethodDeclarationParametersSyntax CreateMethodDeclarationParameters()
    {
        SyntaxToken parenOpen = syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        SyntaxToken parenClose = syntaxFactory.Token(SyntaxTokenKind.ParenClose);

        return new MethodDeclarationParametersSyntax(parenOpen, null, parenClose);
    }

    private BlockExpression CreateMethodDeclarationBody(Sir0Function function)
    {
        SyntaxToken curlyOpen = syntaxFactory.Token(SyntaxTokenKind.CurlyOpen);
        var expressions = CreateStatements(function);
        SyntaxToken curlyClose = syntaxFactory.Token(SyntaxTokenKind.CurlyClose);

        return new BlockExpression(curlyOpen, expressions, curlyClose);
    }

    private IReadOnlyList<StatementSyntax> CreateStatements(Sir0Function function)
    {
        return CreateStatements(function.Operations);
    }

    private IReadOnlyList<StatementSyntax> CreateStatements(Sir0Operation[] operations)
    {
        IReadOnlyList<StatementBlock> blocks = blockBuilder.Build(operations);
        return CreateStatements(blocks);
    }

    private StatementSyntax? CreateStatement(Stack<ExpressionSyntax> expressionStack, Sir0Operation[] operations, ref int index)
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
                    return CreateAsyncBlockStatement(operations, ref index);

                case 0x30:
                    return CreateReturnStatement();

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

                expression = CreateMemberAccessExpression(expressionStack, (string)operation.Arguments[1]);
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
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.Ampersand);
                break;

            case 0x10:
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.Caret);
                break;

            case 0x11:
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.Pipe);
                break;

            case 0x13:
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.ShiftRight);
                break;

            case 0x14:
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.ShiftLeft);
                break;

            case 0x15:
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.Plus);
                break;

            case 0x16:
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.Minus);
                break;

            case 0x17:
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.Asterisk);
                break;

            case 0x18:
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.Slash);
                break;

            case 0x19:
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.Percent);
                break;

            case 0x1A:
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.EqualsEquals);
                break;

            case 0x1B:
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.NotEquals);
                break;

            case 0x1C:
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.SmallerEquals);
                break;

            case 0x1D:
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.GreaterEquals);
                break;

            case 0x1E:
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.SmallerThan);
                break;

            case 0x1F:
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.GreaterThan);
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
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.AndKeyword);
                break;

            case 0x12:
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.OrKeyword);
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
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.Minus);
                break;

            case 0x07:
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.ExclamationPoint);
                break;

            case 0x08:
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.PlusPlus);
                break;

            case 0x09:
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.MinusMinus);
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
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.PlusPlus);
                break;

            case 0x0B:
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.MinusMinus);
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

        if (expression is not MethodInvocationExpressionSyntax and not LiteralExpressionSyntax)
            expression = CreateParenthesizedExpression(expression);

        return new ArrayIndexExpressionSyntax(expression, [CreateArrayIndexerExpression(index)]);
    }

    private ArrayIndexerExpressionSyntax CreateArrayIndexerExpression(ExpressionSyntax index)
    {
        SyntaxToken bracketOpen = syntaxFactory.Token(SyntaxTokenKind.BracketOpen);
        SyntaxToken bracketClose = syntaxFactory.Token(SyntaxTokenKind.BracketClose);

        return new ArrayIndexerExpressionSyntax(bracketOpen, index, bracketClose);
    }

    private ParenthesizedExpressionSyntax CreateParenthesizedExpression(ExpressionSyntax expression)
    {
        var parenOpen = syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        var parenClose = syntaxFactory.Token(SyntaxTokenKind.ParenClose);

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
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.Equals);
                break;

            case 0x21:
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.PlusEquals);
                break;

            case 0x22:
                operatorToken = syntaxFactory.Token(SyntaxTokenKind.MinusEquals);
                break;

            default:
                throw new InvalidOperationException($"Unknown postfix command {command}.");
        }

        return new AssignmentExpressionSyntax(left, operatorToken, right);
    }

    private NativeMethodInvocationExpressionSyntax CreateNativeMethodInvocationExpression(Stack<ExpressionSyntax> syntax, Stack<ExpressionSyntax> args)
    {
        ExpressionSyntax nameExpression = syntax.Pop();
        
        if (nameExpression is not MemberAccessExpressionSyntax && (nameExpression is not LiteralExpressionSyntax literal || literal.Literal.RawKind != (int)SyntaxTokenKind.StringLiteral))
            throw new InvalidOperationException("Need method name for invocation.");

        return new NativeMethodInvocationExpressionSyntax(nameExpression, CreateNativeMethodInvocationParameters([.. args.Reverse()]));
    }

    private MemberAccessExpressionSyntax CreateMemberAccessExpression(Stack<ExpressionSyntax> syntax, string name)
    {
        var left = CreateParenthesizedExpression(syntax.Pop());
        SyntaxToken operatorToken = syntaxFactory.Token(SyntaxTokenKind.ColonColon);
        SyntaxToken identifier = syntaxFactory.Identifier(name);

        return new MemberAccessExpressionSyntax(left, operatorToken, identifier);
    }

    private AssignmentStatementSyntax CreateAssignmentStatement(AssignmentExpressionSyntax assignment)
    {
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new AssignmentStatementSyntax(assignment, semicolon);
    }

    private PostfixStatementSyntax CreatePostfixStatement(PostfixExpressionSyntax postfix)
    {
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new PostfixStatementSyntax(postfix, semicolon);
    }

    private GotoStatementSyntax CreateGotoStatement(string jumpLabel)
    {
        SyntaxToken gotoToken = syntaxFactory.Token(SyntaxTokenKind.GotoKeyword);
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        var label = CreateStringLiteralExpression(jumpLabel);

        return new GotoStatementSyntax(gotoToken, label, semicolon);
    }

    private ReturnStatementSyntax CreateReturnStatement()
    {
        SyntaxToken returnToken = syntaxFactory.Token(SyntaxTokenKind.ReturnKeyword);
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new ReturnStatementSyntax(returnToken, null, semicolon);
    }

    private AsyncBlockStatement CreateAsyncBlockStatement(Sir0Operation[] operations, ref int index)
    {
        for (var i = index; i < operations.Length; i++)
        {
            if (operations[i].Command is not 0x2C)
                continue;

            SyntaxToken asyncToken = syntaxFactory.Token(SyntaxTokenKind.AsyncKeyword);
            var asyncStatements = CreateAsyncBlockBody(operations[index..i]);

            index = i + 1;

            return new AsyncBlockStatement(asyncToken, asyncStatements);
        }

        throw new InvalidOperationException("Incomplete async block.");
    }

    private BlockExpression CreateAsyncBlockBody(Sir0Operation[] operations)
    {
        SyntaxToken curlyOpen = syntaxFactory.Token(SyntaxTokenKind.CurlyOpen);
        var expressions = CreateStatements(operations);
        SyntaxToken curlyClose = syntaxFactory.Token(SyntaxTokenKind.CurlyClose);

        return new BlockExpression(curlyOpen, expressions, curlyClose);
    }

    private MethodInvocationStatementSyntax CreateMethodInvocationStatement(NameSyntax methodName, Sir0Operation operation)
    {
        var method = CreateMethodInvocationExpression(methodName, operation);
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new MethodInvocationStatementSyntax(method, semicolon);
    }

    private NativeMethodInvocationStatementSyntax CreateNativeMethodInvocationStatement(NativeMethodInvocationExpressionSyntax methodInvocation)
    {
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new NativeMethodInvocationStatementSyntax(methodInvocation, semicolon);
    }

    private MethodInvocationExpressionSyntax CreateMethodInvocationExpression(NameSyntax methodName, Sir0Operation operation)
    {
        var parameters = CreateMethodInvocationParameters(operation);

        return new MethodInvocationExpressionSyntax(methodName, parameters);
    }

    private NativeMethodInvocationParametersSyntax CreateNativeMethodInvocationParameters(ExpressionSyntax[] arguments)
    {
        SyntaxToken parenOpen = syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        var parameterList = new CommaSeparatedSyntaxList<ExpressionSyntax>(arguments);
        SyntaxToken parenClose = syntaxFactory.Token(SyntaxTokenKind.ParenClose);

        return new NativeMethodInvocationParametersSyntax(parenOpen, parameterList, parenClose);
    }

    private MethodInvocationParametersSyntax CreateMethodInvocationParameters(Sir0Operation operation)
    {
        SyntaxToken parenOpen = syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        var parameterList = CreateValueList(operation);
        SyntaxToken parenClose = syntaxFactory.Token(SyntaxTokenKind.ParenClose);

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
        return new LiteralExpressionSyntax(syntaxFactory.NumericLiteral(value));
    }

    private LiteralExpressionSyntax CreateFloatingNumericLiteralExpression(float value)
    {
        return new LiteralExpressionSyntax(syntaxFactory.FloatingNumericLiteral(value));
    }

    private LiteralExpressionSyntax CreateStringLiteralExpression(string value)
    {
        return new LiteralExpressionSyntax(syntaxFactory.StringLiteral(value));
    }

    private LiteralExpressionSyntax CreateTrueLiteralExpression()
    {
        return new LiteralExpressionSyntax(syntaxFactory.Token(SyntaxTokenKind.TrueKeyword));
    }

    private GotoLabelStatementSyntax CreateGotoLabelStatement(string jumpLabel)
    {
        var colon = syntaxFactory.Token(SyntaxTokenKind.Colon);

        return new GotoLabelStatementSyntax(CreateStringLiteralExpression(jumpLabel), colon);
    }

    private NameSyntax CreateName(string name)
    {
        if (name.Contains('.'))
            return new SimpleNameSyntax(syntaxFactory.Identifier(name));

        NameSyntax? result = null;

        foreach (string part in name.Split('.').Reverse())
        {
            if (result is null)
                result = new SimpleNameSyntax(syntaxFactory.Identifier(part));
            else
                result = new QualifiedNameSyntax(new SimpleNameSyntax(syntaxFactory.Identifier(part)), syntaxFactory.Token(SyntaxTokenKind.Dot), result);
        }

        return result!;
    }

    private IReadOnlyList<StatementSyntax> CreateStatements(IReadOnlyList<StatementBlock> blocks)
    {
        if (blocks.Count == 0)
            return [];

        Dictionary<string, int> labelLookup = CreateLabelLookup(blocks);
        Dictionary<int, LoopBound> loopBounds = CreateLoopBounds(blocks, labelLookup);

        return BuildStatementsRange(blocks, labelLookup, loopBounds, 0, blocks.Count, out _);
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
        out ExpressionSyntax? condition, bool skipLoopStart = false, LoopContext? loopContext = null)
    {
        condition = null;

        var result = new List<StatementSyntax>();
        for (var i = startIndex; i < endIndex;)
        {
            if (!skipLoopStart && loopBounds.TryGetValue(i, out LoopBound? loopBound) && loopBound.EndIndex < endIndex)
            {
                int exitIndex = Math.Min(loopBound.EndIndex + 1, blocks.Count);
                var nestedContext = new LoopContext(i, loopBound.EndIndex, exitIndex);
                IReadOnlyList<StatementSyntax> bodyStatements = BuildStatementsRange(blocks, labelLookup, loopBounds, i,
                    loopBound.EndIndex + 1, out condition, true,
                    nestedContext);
                result.Add(CreateLoopStatement(loopBound.ConditionKind, bodyStatements, condition));
                i = loopBound.EndIndex + 1;
                continue;
            }

            skipLoopStart = false;

            result.AddRange(CreateStatementsFromBlock(blocks, labelLookup, loopContext, i, true, out condition));
            if (TryBuildIfStatement(blocks, labelLookup, loopBounds, i, endIndex, loopContext, condition, out StatementSyntax? ifStatement,
                    out int nextIndex))
            {
                if (TryMergeLoopControlIf(result, ifStatement, out StatementSyntax mergedStatement))
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

    private bool TryBuildIfStatement(IReadOnlyList<StatementBlock> blocks, Dictionary<string, int> labelLookup,
        Dictionary<int, LoopBound> loopBounds, int index, int endIndex, LoopContext? loopContext, ExpressionSyntax? condition,
        [NotNullWhen(true)] out StatementSyntax? statement, out int nextIndex)
    {
        statement = null;
        nextIndex = index + 1;

        StatementBlock block = blocks[index];
        if (block.TerminalCommand is not (0x36 or 0x37) || block.JumpLabel is null)
            return false;

        if (!labelLookup.TryGetValue(block.JumpLabel, out int targetIndex))
            return false;

        if (targetIndex <= index || targetIndex > endIndex)
            return false;

        if (condition is null)
            return false;

        if (block.TerminalCommand is 0x37)
        {
            int thenStart = index + 1;
            int thenEnd = targetIndex;
            if (TryBuildIfElseOnFalse(blocks, labelLookup, loopBounds, thenStart, thenEnd, targetIndex, loopContext, condition, out statement,
                    out nextIndex))
                return true;

            IReadOnlyList<StatementSyntax> thenStatements = BuildStatementsRange(blocks, labelLookup, loopBounds, thenStart, thenEnd, out _, false,
                loopContext);
            statement = CreateIfStatement(thenStatements, condition);
            nextIndex = targetIndex;
            return true;
        }

        int elseStart = index + 1;
        int elseEnd = targetIndex;
        if (TryBuildIfElseOnTrue(blocks, labelLookup, loopBounds, elseStart, elseEnd, targetIndex, loopContext, condition, out statement, out nextIndex))
            return true;

        IReadOnlyList<StatementSyntax> notStatements = BuildStatementsRange(blocks, labelLookup, loopBounds, elseStart, elseEnd, out _, false,
            loopContext);
        statement = CreateIfNotStatement(notStatements, condition);
        nextIndex = targetIndex;
        return true;
    }

    private bool TryBuildIfElseOnFalse(IReadOnlyList<StatementBlock> blocks, Dictionary<string, int> labelLookup,
        Dictionary<int, LoopBound> loopBounds, int thenStart, int thenEnd, int targetIndex, LoopContext? loopContext, ExpressionSyntax condition,
        [NotNullWhen(true)] out StatementSyntax? statement, out int nextIndex)
    {
        statement = null;
        nextIndex = targetIndex;

        if (thenStart >= thenEnd)
            return false;

        StatementBlock endThenBlock = blocks[thenEnd - 1];
        if (endThenBlock.TerminalCommand is not 0x35 || endThenBlock.JumpLabel is null)
            return false;

        if (!labelLookup.TryGetValue(endThenBlock.JumpLabel, out int endIndex) || endIndex <= targetIndex)
            return false;

        IReadOnlyList<StatementSyntax> thenStatements = BuildStatementsRange(blocks, labelLookup, loopBounds, thenStart, thenEnd, out _, false,
            loopContext);
        IReadOnlyList<StatementSyntax> elseStatements = BuildStatementsRange(blocks, labelLookup, loopBounds, targetIndex, endIndex, out _, false,
            loopContext);

        if (elseStatements.Count <= 0)
            return false;

        statement = CreateIfElseStatement(thenStatements, elseStatements, condition);
        nextIndex = endIndex;
        return true;
    }

    private bool TryBuildIfElseOnTrue(IReadOnlyList<StatementBlock> blocks, Dictionary<string, int> labelLookup,
        Dictionary<int, LoopBound> loopBounds, int elseStart, int elseEnd, int targetIndex, LoopContext? loopContext, ExpressionSyntax condition,
        [NotNullWhen(true)] out StatementSyntax? statement, out int nextIndex)
    {
        statement = null;
        nextIndex = targetIndex;

        if (elseStart >= elseEnd)
            return false;

        StatementBlock endElseBlock = blocks[elseEnd - 1];
        if (endElseBlock.TerminalCommand is not 0x35 || endElseBlock.JumpLabel is null)
            return false;

        if (!labelLookup.TryGetValue(endElseBlock.JumpLabel, out int endIndex) || endIndex <= targetIndex)
            return false;

        IReadOnlyList<StatementSyntax> thenStatements = BuildStatementsRange(blocks, labelLookup, loopBounds, elseStart, elseEnd, out _, false,
            loopContext);
        IReadOnlyList<StatementSyntax> elseStatements = BuildStatementsRange(blocks, labelLookup, loopBounds, targetIndex, endIndex, out _, false,
            loopContext);

        if (elseStatements.Count <= 0)
            return false;

        statement = CreateIfNotElseStatement(thenStatements, elseStatements, condition);
        nextIndex = endIndex;
        return true;
    }

    private IReadOnlyList<StatementSyntax> CreateStatementsFromBlock(IReadOnlyList<StatementBlock> blocks,
        IReadOnlyDictionary<string, int>? labelLookup, LoopContext? loopContext, int blockIndex, bool skipTerminalJump,
        out ExpressionSyntax? condition)
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
            if (targetIndex == loopContext.StartIndex && blockIndex != loopContext.EndIndex)
            {
                result.AddRange(CreateStatementsFromBlock(blocks, labelLookup, null, blockIndex, true, out condition));
                AddLoopControlStatements(result, block.TerminalCommand.Value, LoopControlKind.Continue, condition);

                return result;
            }

            if (targetIndex == loopContext.ExitIndex)
            {
                result.AddRange(CreateStatementsFromBlock(blocks, labelLookup, null, blockIndex, true, out condition));
                AddLoopControlStatements(result, block.TerminalCommand.Value, LoopControlKind.Break, condition);

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

            StatementSyntax? statement = CreateStatement(expressionStack, blockOperations, ref index);
            if (statement is null)
                continue;

            result.Add(statement);
        }

        if (expressionStack.Count > 1)
            throw new InvalidOperationException("Too many expressions on stack.");

        if (expressionStack.Count > 0)
            condition = expressionStack.Pop();

        return result;
    }

    private IfStatementSyntax CreateIfStatement(IReadOnlyList<StatementSyntax> thenStatements, ExpressionSyntax condition)
    {
        SyntaxToken ifToken = syntaxFactory.Token(SyntaxTokenKind.IfKeyword);
        SyntaxToken parenOpen = syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        SyntaxToken parenClose = syntaxFactory.Token(SyntaxTokenKind.ParenClose);
        BlockExpression body = CreateBlockExpression(thenStatements);

        return new IfStatementSyntax(ifToken, parenOpen, condition, parenClose, body);
    }

    private IfElseStatementSyntax CreateIfElseStatement(IReadOnlyList<StatementSyntax> thenStatements, IReadOnlyList<StatementSyntax> elseStatements, ExpressionSyntax condition)
    {
        SyntaxToken ifToken = syntaxFactory.Token(SyntaxTokenKind.IfKeyword);
        SyntaxToken parenOpen = syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        SyntaxToken parenClose = syntaxFactory.Token(SyntaxTokenKind.ParenClose);
        BlockExpression body = CreateBlockExpression(thenStatements);
        SyntaxToken elseToken = syntaxFactory.Token(SyntaxTokenKind.ElseKeyword);
        BlockExpression elseBody = CreateElseBlockExpression(elseStatements);

        return new IfElseStatementSyntax(ifToken, parenOpen, condition, parenClose, body, elseToken, elseBody);
    }

    private IfNotStatementSyntax CreateIfNotStatement(IReadOnlyList<StatementSyntax> thenStatements, ExpressionSyntax condition)
    {
        SyntaxToken ifToken = syntaxFactory.Token(SyntaxTokenKind.IfKeyword);
        SyntaxToken notToken = syntaxFactory.Token(SyntaxTokenKind.NotKeyword);
        SyntaxToken parenOpen = syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        SyntaxToken parenClose = syntaxFactory.Token(SyntaxTokenKind.ParenClose);
        BlockExpression body = CreateBlockExpression(thenStatements);

        return new IfNotStatementSyntax(ifToken, notToken, parenOpen, condition, parenClose, body);
    }

    private IfNotElseStatementSyntax CreateIfNotElseStatement(IReadOnlyList<StatementSyntax> thenStatements,
        IReadOnlyList<StatementSyntax> elseStatements, ExpressionSyntax condition)
    {
        SyntaxToken ifToken = syntaxFactory.Token(SyntaxTokenKind.IfKeyword);
        SyntaxToken notToken = syntaxFactory.Token(SyntaxTokenKind.NotKeyword);
        SyntaxToken parenOpen = syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        SyntaxToken parenClose = syntaxFactory.Token(SyntaxTokenKind.ParenClose);
        BlockExpression body = CreateBlockExpression(thenStatements);
        SyntaxToken elseToken = syntaxFactory.Token(SyntaxTokenKind.ElseKeyword);
        BlockExpression elseBody = CreateElseBlockExpression(elseStatements);

        return new IfNotElseStatementSyntax(ifToken, notToken, parenOpen, condition, parenClose, body, elseToken, elseBody);
    }

    private DoWhileStatementSyntax CreateDoWhileStatement(IReadOnlyList<StatementSyntax> bodyStatements, ExpressionSyntax condition)
    {
        SyntaxToken doToken = syntaxFactory.Token(SyntaxTokenKind.DoKeyword);
        BlockExpression body = CreateBlockExpression(bodyStatements);
        SyntaxToken whileToken = syntaxFactory.Token(SyntaxTokenKind.WhileKeyword);
        SyntaxToken parenOpen = syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        SyntaxToken parenClose = syntaxFactory.Token(SyntaxTokenKind.ParenClose);
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new DoWhileStatementSyntax(doToken, body, whileToken, parenOpen, condition, parenClose, semicolon);
    }

    private DoWhileNotStatementSyntax CreateDoWhileNotStatement(IReadOnlyList<StatementSyntax> bodyStatements, ExpressionSyntax condition)
    {
        SyntaxToken doToken = syntaxFactory.Token(SyntaxTokenKind.DoKeyword);
        BlockExpression body = CreateBlockExpression(bodyStatements);
        SyntaxToken whileToken = syntaxFactory.Token(SyntaxTokenKind.WhileKeyword);
        SyntaxToken notToken = syntaxFactory.Token(SyntaxTokenKind.NotKeyword);
        SyntaxToken parenOpen = syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        SyntaxToken parenClose = syntaxFactory.Token(SyntaxTokenKind.ParenClose);
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new DoWhileNotStatementSyntax(doToken, body, whileToken, notToken, parenOpen, condition, parenClose, semicolon);
    }

    private StatementSyntax CreateLoopStatement(LoopConditionKind conditionKind, IReadOnlyList<StatementSyntax> bodyStatements, ExpressionSyntax? condition)
    {
        if (conditionKind is LoopConditionKind.True)
            return CreateDoWhileStatement(bodyStatements, CreateTrueLiteralExpression());

        if (condition is null)
            throw new InvalidOperationException("No condition for loop.");

        return conditionKind switch
        {
            LoopConditionKind.Not => CreateDoWhileNotStatement(bodyStatements, condition),
            LoopConditionKind.Normal => CreateDoWhileStatement(bodyStatements, condition),
            _ => throw new InvalidOperationException($"Unknown loop condition {conditionKind}.")
        };
    }

    private BlockExpression CreateBlockExpression(IReadOnlyList<StatementSyntax> statements)
    {
        SyntaxToken curlyOpen = syntaxFactory.Token(SyntaxTokenKind.CurlyOpen);
        SyntaxToken curlyClose = syntaxFactory.Token(SyntaxTokenKind.CurlyClose);

        return new BlockExpression(curlyOpen, statements, curlyClose);
    }

    private BlockExpression CreateElseBlockExpression(IReadOnlyList<StatementSyntax> statements)
    {
        if (IsElseIfCandidate(statements))
            return CreateInlineBlockExpression(statements);

        return CreateBlockExpression(statements);
    }

    private static bool IsElseIfCandidate(IReadOnlyList<StatementSyntax> statements)
    {
        if (statements.Count != 1)
            return false;

        return statements[0] is IfStatementSyntax
            or IfNotStatementSyntax
            or IfElseStatementSyntax
            or IfNotElseStatementSyntax;
    }

    private BlockExpression CreateInlineBlockExpression(IReadOnlyList<StatementSyntax> statements)
    {
        SyntaxToken curlyOpen = syntaxFactory.Create(string.Empty, (int)SyntaxTokenKind.CurlyOpen);
        SyntaxToken curlyClose = syntaxFactory.Create(string.Empty, (int)SyntaxTokenKind.CurlyClose);

        return new BlockExpression(curlyOpen, statements, curlyClose);
    }

    private bool TryMergeLoopControlIf(List<StatementSyntax> statements, StatementSyntax ifStatement, out StatementSyntax mergedStatement)
    {
        mergedStatement = ifStatement;
        if (statements.Count == 0)
            return false;

        if (statements[^1] is not IfElseStatementSyntax controlIf)
            return false;

        if (!IsEmptyBlock(controlIf.Body))
            return false;

        if (!TryGetLoopControlStatement(controlIf.ElseBody, out StatementSyntax loopControl))
            return false;

        if (ifStatement is not IfStatementSyntax ifOnlyStatement)
            return false;

        if (!ReferenceEquals(controlIf.Condition, ifOnlyStatement.Condition))
            return false;

        mergedStatement = CreateIfElseStatement(ifOnlyStatement.Body.Statements, [loopControl], controlIf.Condition);
        return true;
    }

    private static bool IsEmptyBlock(BlockExpression blockExpression)
    {
        return blockExpression.Statements.Count == 0;
    }

    private bool TryGetLoopControlStatement(BlockExpression elseBody, out StatementSyntax loopControl)
    {
        loopControl = null!;
        if (elseBody.Statements.Count != 1)
            return false;

        return elseBody.Statements[0] switch
        {
            BreakStatementSyntax => TryCreateLoopControlStatement(LoopControlKind.Break, out loopControl),
            ContinueStatementSyntax => TryCreateLoopControlStatement(LoopControlKind.Continue, out loopControl),
            _ => false
        };
    }

    private bool TryCreateLoopControlStatement(LoopControlKind kind, out StatementSyntax loopControl)
    {
        loopControl = kind == LoopControlKind.Break ? CreateBreakStatement() : CreateContinueStatement();
        return true;
    }

    private void AddLoopControlStatements(List<StatementSyntax> target, byte terminalCommand, LoopControlKind controlKind, ExpressionSyntax? condition)
    {
        if (terminalCommand is 0x35)
        {
            target.Add(controlKind == LoopControlKind.Break ? CreateBreakStatement() : CreateContinueStatement());
            return;
        }

        if (condition is null)
            throw new InvalidOperationException("No condition for loop.");

        if (terminalCommand is 0x36)
        {
            target.Add(CreateIfStatement([controlKind == LoopControlKind.Break ? CreateBreakStatement() : CreateContinueStatement()], condition));
            return;
        }

        if (terminalCommand is 0x37)
        {
            StatementSyntax elseStatement = controlKind == LoopControlKind.Break ? CreateBreakStatement() : CreateContinueStatement();
            target.Add(CreateIfElseStatement([], [elseStatement], condition));
        }
    }

    private BreakStatementSyntax CreateBreakStatement()
    {
        SyntaxToken breakToken = syntaxFactory.Token(SyntaxTokenKind.BreakKeyword);
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new BreakStatementSyntax(breakToken, semicolon);
    }

    private ContinueStatementSyntax CreateContinueStatement()
    {
        SyntaxToken continueToken = syntaxFactory.Token(SyntaxTokenKind.ContinueKeyword);
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new ContinueStatementSyntax(continueToken, semicolon);
    }

    private sealed record LoopContext(int StartIndex, int EndIndex, int ExitIndex);

    private sealed record LoopBound(int EndIndex, LoopConditionKind ConditionKind);

    private enum LoopConditionKind
    {
        Normal,
        Not,
        True
    }

    private enum LoopControlKind
    {
        Break,
        Continue
    }
}