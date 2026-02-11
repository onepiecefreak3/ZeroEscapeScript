using Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Conversion;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;
using Logic.Domain.CodeAnalysisManagement.Contract.SpikeChunsoft;
using Logic.Domain.SpikeChunsoftManagement.Contract.DataClasses.Script;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Logic.Business.SpikeChunsoftScriptManagement.Conversion;

internal class FsbCodeUnitConverter(ISpikeChunsoftSyntaxFactory syntaxFactory) : IFsbCodeUnitConverter
{
    private readonly Regex _subPattern = new("^sub[0-9]+$", RegexOptions.Compiled);
    private int _labelCounter;
    private int _switchTempCounter;
    private readonly Stack<LoopEmissionContext> _loopContextStack = new();
    private readonly Stack<SwitchEmissionContext> _switchContextStack = new();

    public Sir0Script CreateScriptFile(CodeUnitSyntax tree)
    {
        HashSet<string> exportedLabels = [];

        string name = GetStringLiteral(tree.NameDeclaration.Name);
        string[] variables = CreateGlobalVariables(tree.Members);
        Sir0Function[] functions = CreateFunctions(tree.Members, exportedLabels);

        return new Sir0Script
        {
            Name = name,
            Functions = functions,
            ExportedLabels = [.. exportedLabels],
            GlobalVariables = variables
        };
    }

    private string[] CreateGlobalVariables(IReadOnlyList<DeclarationSyntax> members)
    {
        var variables = new List<string>();

        foreach (GlobalVariableDeclarationSyntax member in members.Where(m => m is GlobalVariableDeclarationSyntax).Cast<GlobalVariableDeclarationSyntax>())
            variables.Add(GetStringLiteral(member.Identifier));

        return [.. variables];
    }

    private Sir0Function[] CreateFunctions(IReadOnlyList<DeclarationSyntax> members, HashSet<string> exportedLabels)
    {
        var functions = new List<Sir0Function>();

        foreach (MethodDeclarationSyntax member in members.Where(m => m is MethodDeclarationSyntax).Cast<MethodDeclarationSyntax>())
            functions.Add(CreateFunction(member, exportedLabels));

        return [.. functions];
    }

    private Sir0Function CreateFunction(MethodDeclarationSyntax method, HashSet<string> exportedLabels)
    {
        Sir0Operation[] operations = CreateOperations(method, exportedLabels);

        return new Sir0Function(GetStringLiteral(method.Name), operations);
    }

    private Sir0Operation[] CreateOperations(MethodDeclarationSyntax method, HashSet<string> exportedLabels)
    {
        if (method.Body.Statements.Count <= 0 || method.Body.Statements[^1] is not ExitStatementSyntax)
            method.Body.SetStatements(method.Body.Statements.Concat([CreateExitStatement()]).ToList());

        var operations = new List<Sir0Operation>();

        AddOperation(operations, 0x25);
        CreateOperations(operations, method.Body, exportedLabels);

        return [.. operations];
    }

    private ExitStatementSyntax CreateExitStatement()
    {
        SyntaxToken returnToken = syntaxFactory.Token(SyntaxTokenKind.ReturnKeyword);
        SyntaxToken semicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new ExitStatementSyntax(returnToken, semicolon);
    }

    private void CreateOperations(List<Sir0Operation> operations, BlockExpression block, HashSet<string> exportedLabels, string? leadingLabel = null)
    {
        string? jumpLabel = CreateOperationsInternal(operations, block, exportedLabels, leadingLabel);

        if (jumpLabel is not null)
            BackPropagateJumpLabel(operations, jumpLabel);
    }

    private string? CreateOperationsInternal(List<Sir0Operation> operations, BlockExpression block, HashSet<string> exportedLabels, string? leadingLabel)
    {
        return CreateOperationsInternal(operations, block.Statements, exportedLabels, leadingLabel);
    }

    private string? CreateOperationsInternal(List<Sir0Operation> operations, IReadOnlyList<StatementSyntax> statements, HashSet<string> exportedLabels, string? leadingLabel)
    {
        string? jumpLabel = leadingLabel;
        foreach (StatementSyntax statement in statements)
        {
            string? nextLabel = null;
            switch (statement)
            {
                case ExportedGotoLabelStatementSyntax exportGotoLabelStatement:
                    AddOperations(operations, exportGotoLabelStatement, jumpLabel, exportedLabels);
                    break;

                case GotoLabelStatementSyntax gotoLabelStatement:
                    AddOperations(operations, gotoLabelStatement, jumpLabel);
                    break;

                case GotoStatementSyntax gotoStatement:
                    AddOperations(operations, gotoStatement, jumpLabel);
                    break;

                case MethodInvocationStatementSyntax methodInvocation:
                    AddOperations(operations, methodInvocation, jumpLabel);
                    break;

                case NativeMethodInvocationStatementSyntax methodInvocation:
                    AddOperations(operations, methodInvocation, jumpLabel);
                    break;

                case AssignmentStatementSyntax assignment:
                    AddOperations(operations, assignment, jumpLabel);
                    break;

                case PostfixStatementSyntax postfix:
                    AddOperations(operations, postfix, jumpLabel);
                    break;

                case AsyncBlockStatement asyncStatement:
                    AddAsyncOperations(operations, asyncStatement, exportedLabels, jumpLabel);
                    break;

                case IfStatementSyntax ifStatement:
                    nextLabel = AddIfOperations(operations, ifStatement, exportedLabels, jumpLabel);
                    break;

                case IfElseStatementSyntax ifElseStatement:
                    nextLabel = AddIfElseOperations(operations, ifElseStatement, exportedLabels, jumpLabel);
                    break;

                case IfNotStatementSyntax ifNotStatement:
                    nextLabel = AddIfNotOperations(operations, ifNotStatement, exportedLabels, jumpLabel);
                    break;

                case IfNotElseStatementSyntax ifNotElseStatement:
                    nextLabel = AddIfNotElseOperations(operations, ifNotElseStatement, exportedLabels, jumpLabel);
                    break;

                case SwitchStatementSyntax switchStatement:
                    nextLabel = AddSwitchOperations(operations, switchStatement, exportedLabels, jumpLabel);
                    break;

                case DoWhileStatementSyntax doWhileStatement:
                    nextLabel = AddDoWhileOperations(operations, doWhileStatement, exportedLabels, jumpLabel);
                    break;

                case DoWhileNotStatementSyntax doWhileNotStatement:
                    nextLabel = AddDoWhileNotOperations(operations, doWhileNotStatement, exportedLabels, jumpLabel);
                    break;

                case BreakStatementSyntax:
                    nextLabel = AddBreakOperation(operations, jumpLabel);
                    break;

                case ContinueStatementSyntax:
                    nextLabel = AddContinueOperation(operations, jumpLabel);
                    break;

                case ReturnStatementSyntax returnStatement:
                    AddReturnOperations(operations, returnStatement, jumpLabel);
                    break;

                case ExitStatementSyntax:
                    AddOperation(operations, 0x26, jumpLabel);
                    break;

                default:
                    throw CreateException($"Unknown statement {statement.GetType().Name}.", statement.Location);
            }

            jumpLabel = nextLabel;
        }

        return jumpLabel;
    }

    private void AddOperations(List<Sir0Operation> operations, ExportedGotoLabelStatementSyntax exportGotoLabelStatement, string? jumpLabel, HashSet<string> exportedLabels)
    {
        var label = GetStringLiteral(exportGotoLabelStatement.Label);
        exportedLabels.Add(label);

        AddGotoLabelOperation(operations, jumpLabel, label);
    }

    private void AddOperations(List<Sir0Operation> operations, GotoLabelStatementSyntax gotoLabelStatement, string? jumpLabel)
    {
        AddGotoLabelOperation(operations, jumpLabel, GetStringLiteral(gotoLabelStatement.Label));
    }

    private void AddOperations(List<Sir0Operation> operations, GotoStatementSyntax gotoStatement, string? jumpLabel)
    {
        AddGotoOperation(operations, jumpLabel, GetStringLiteral(gotoStatement.Label));
    }

    private void AddOperations(List<Sir0Operation> operations, MethodInvocationStatementSyntax methodInvocation, string? jumpLabel)
    {
        byte operation = GetOperation(methodInvocation.Method.Name);

        object[] arguments = [];
        if (methodInvocation.Method.Parameters.ParameterList != null)
        {
            var literals = methodInvocation.Method.Parameters.ParameterList.Elements;
            arguments = new object[literals.Count];

            for (var i = 0; i < literals.Count; i++)
                arguments[i] = GetArgument(literals[i]);
        }

        operations.Add(new Sir0Operation(jumpLabel, operation, arguments));
    }

    private void AddOperations(List<Sir0Operation> operations, NativeMethodInvocationStatementSyntax methodInvocation, string? jumpLabel)
    {
        AddNativeMethodInvocation(operations, methodInvocation.Method, jumpLabel);
        AddOperation(operations, 0x27);
    }

    private void AddOperations(List<Sir0Operation> operations, AssignmentStatementSyntax assignment, string? jumpLabel)
    {
        AddAssignmentExpression(operations, assignment.Assignment, jumpLabel);
        AddOperation(operations, 0x27);
    }

    private void AddOperations(List<Sir0Operation> operations, PostfixStatementSyntax postfix, string? jumpLabel)
    {
        AddPostfixExpression(operations, postfix.Postfix, jumpLabel);
        AddOperation(operations, 0x27);
    }

    private void AddAsyncOperations(List<Sir0Operation> operations, AsyncBlockStatement asyncStatement, HashSet<string> exportedLabels, string? jumpLabel)
    {
        AddAsyncStartOperation(operations, jumpLabel);
        CreateOperations(operations, asyncStatement.Body, exportedLabels);
        AddAsyncEndOperation(operations, null);
    }

    private void AddExpressionOperations(List<Sir0Operation> operations, ExpressionSyntax expression, string? jumpLabel)
    {
        switch (expression)
        {
            case ParenthesizedExpressionSyntax parenthesized:
                AddParenthesizedExpression(operations, parenthesized, jumpLabel);
                break;

            case BinaryExpressionSyntax binary:
                AddBinaryExpression(operations, binary, jumpLabel);
                break;

            case LogicalExpressionSyntax logical:
                AddLogicalExpression(operations, logical, jumpLabel);
                break;

            case UnaryExpressionSyntax unary:
                AddUnaryExpression(operations, unary, jumpLabel);
                break;

            case PostfixExpressionSyntax postfix:
                AddPostfixExpression(operations, postfix, jumpLabel);
                break;

            case ArrayIndexExpressionSyntax arrayIndex:
                AddArrayIndexExpression(operations, arrayIndex, jumpLabel);
                break;

            case LiteralExpressionSyntax literal:
                AddLiteralExpression(operations, literal, jumpLabel);
                break;

            case AssignmentExpressionSyntax assignment:
                AddAssignmentExpression(operations, assignment, jumpLabel);
                break;

            case SimpleMemberAccessExpressionSyntax memberAccess:
                AddSimpleMemberAccessExpression(operations, memberAccess, jumpLabel);
                break;

            case QualifiedMemberAccessExpressionSyntax memberAccess:
                AddQualifiedMemberAccessExpression(operations, memberAccess, jumpLabel);
                break;

            case CompoundMemberAccessExpressionSyntax memberAccess:
                AddCompoundMemberAccessExpression(operations, memberAccess, jumpLabel);
                break;

            case NativeMethodInvocationExpressionSyntax invocation:
                AddNativeMethodInvocation(operations, invocation, jumpLabel);
                break;
        }
    }

    private void AddParenthesizedExpression(List<Sir0Operation> operations, ParenthesizedExpressionSyntax expression, string? jumpLabel)
    {
        AddExpressionOperations(operations, expression.Expression, jumpLabel);
    }

    private void AddBinaryExpression(List<Sir0Operation> operations, BinaryExpressionSyntax expression, string? jumpLabel)
    {
        AddExpressionOperations(operations, expression.Left, jumpLabel);
        AddExpressionOperations(operations, expression.Right, null);

        switch (expression.Operation.RawKind)
        {
            case (int)SyntaxTokenKind.Ampersand:
                AddOperation(operations, 0x0E);
                break;

            case (int)SyntaxTokenKind.Caret:
                AddOperation(operations, 0x10);
                break;

            case (int)SyntaxTokenKind.Pipe:
                AddOperation(operations, 0x11);
                break;

            case (int)SyntaxTokenKind.ShiftRight:
                AddOperation(operations, 0x13);
                break;

            case (int)SyntaxTokenKind.ShiftLeft:
                AddOperation(operations, 0x14);
                break;

            case (int)SyntaxTokenKind.Plus:
                AddOperation(operations, 0x15);
                break;

            case (int)SyntaxTokenKind.Minus:
                AddOperation(operations, 0x16);
                break;

            case (int)SyntaxTokenKind.Asterisk:
                AddOperation(operations, 0x17);
                break;

            case (int)SyntaxTokenKind.Slash:
                AddOperation(operations, 0x18);
                break;

            case (int)SyntaxTokenKind.Percent:
                AddOperation(operations, 0x19);
                break;

            case (int)SyntaxTokenKind.EqualsEquals:
                AddOperation(operations, 0x1A);
                break;

            case (int)SyntaxTokenKind.NotEquals:
                AddOperation(operations, 0x1B);
                break;

            case (int)SyntaxTokenKind.SmallerEquals:
                AddOperation(operations, 0x1C);
                break;

            case (int)SyntaxTokenKind.GreaterEquals:
                AddOperation(operations, 0x1D);
                break;

            case (int)SyntaxTokenKind.SmallerThan:
                AddOperation(operations, 0x1E);
                break;

            case (int)SyntaxTokenKind.GreaterThan:
                AddOperation(operations, 0x1F);
                break;

            default:
                throw new InvalidOperationException($"Unknown binary expression {expression.Operation.RawKind}.");
        }
    }

    private void AddLogicalExpression(List<Sir0Operation> operations, LogicalExpressionSyntax expression, string? jumpLabel)
    {
        AddExpressionOperations(operations, expression.Left, jumpLabel);
        AddExpressionOperations(operations, expression.Right, null);

        switch (expression.Operation.RawKind)
        {
            case (int)SyntaxTokenKind.AndKeyword:
                AddOperation(operations, 0x0F);
                break;

            case (int)SyntaxTokenKind.OrKeyword:
                AddOperation(operations, 0x12);
                break;

            default:
                throw new InvalidOperationException($"Unknown logical expression {expression.Operation.RawKind}.");
        }
    }

    private void AddUnaryExpression(List<Sir0Operation> operations, UnaryExpressionSyntax expression, string? jumpLabel)
    {
        AddExpressionOperations(operations, expression.Expression, jumpLabel);

        switch (expression.Operation.RawKind)
        {
            case (int)SyntaxTokenKind.Minus:
                AddOperation(operations, 0x01);
                break;

            case (int)SyntaxTokenKind.ExclamationPoint:
                AddOperation(operations, 0x07);
                break;

            case (int)SyntaxTokenKind.PlusPlus:
                AddOperation(operations, 0x08);
                break;

            case (int)SyntaxTokenKind.MinusMinus:
                AddOperation(operations, 0x09);
                break;

            default:
                throw new InvalidOperationException($"Unknown unary expression {expression.Operation.RawKind}.");
        }
    }

    private void AddPostfixExpression(List<Sir0Operation> operations, PostfixExpressionSyntax expression, string? jumpLabel)
    {
        AddExpressionOperations(operations, expression.Expression, jumpLabel);

        switch (expression.Operation.RawKind)
        {
            case (int)SyntaxTokenKind.PlusPlus:
                AddOperation(operations, 0x0A);
                break;

            case (int)SyntaxTokenKind.MinusMinus:
                AddOperation(operations, 0x0B);
                break;

            default:
                throw new InvalidOperationException($"Unknown postfix expression {expression.Operation.RawKind}.");
        }
    }

    private void AddArrayIndexExpression(List<Sir0Operation> operations, ArrayIndexExpressionSyntax expression, string? jumpLabel)
    {
        AddExpressionOperations(operations, expression.Value, jumpLabel);

        foreach (var indexer in expression.Indexer)
            AddArrayIndexerExpression(operations, indexer, null);
    }

    private void AddArrayIndexerExpression(List<Sir0Operation> operations, ArrayIndexerExpressionSyntax expression, string? jumpLabel)
    {
        AddExpressionOperations(operations, expression.Index, jumpLabel);
        AddOperation(operations, 0x0C);
    }

    private void AddLiteralExpression(List<Sir0Operation> operations, LiteralExpressionSyntax expression, string? jumpLabel)
    {
        switch (expression.Literal.RawKind)
        {
            case (int)SyntaxTokenKind.NumericLiteral:
                AddNumericLiteralOperation(operations, GetNumericLiteral(expression), jumpLabel);
                break;

            case (int)SyntaxTokenKind.FloatingNumericLiteral:
                AddNumericLiteralOperation(operations, GetFloatingNumericLiteral(expression), jumpLabel);
                break;

            case (int)SyntaxTokenKind.StringLiteral:
                AddStringLiteralOperation(operations, GetStringLiteral(expression), jumpLabel);
                break;
        }
    }

    private void AddAssignmentExpression(List<Sir0Operation> operations, AssignmentExpressionSyntax expression, string? jumpLabel)
    {
        AddExpressionOperations(operations, expression.Left, jumpLabel);
        AddExpressionOperations(operations, expression.Right, null);

        switch (expression.Operator.RawKind)
        {
            case (int)SyntaxTokenKind.Equals:
                AddOperation(operations, 0x20);
                break;

            case (int)SyntaxTokenKind.PlusEquals:
                AddOperation(operations, 0x21);
                break;

            case (int)SyntaxTokenKind.MinusEquals:
                AddOperation(operations, 0x22);
                break;

            default:
                throw new InvalidOperationException($"Unknown assignment expression {expression.Operator.RawKind}.");
        }
    }

    private void AddSimpleMemberAccessExpression(List<Sir0Operation> operations, SimpleMemberAccessExpressionSyntax memberAccess, string? jumpLabel)
    {
        AddStringLiteralOperation(operations, "?" + memberAccess.Identifier.Text, jumpLabel);
    }

    private void AddQualifiedMemberAccessExpression(List<Sir0Operation> operations, QualifiedMemberAccessExpressionSyntax memberAccess, string? jumpLabel)
    {
        AddStringLiteralOperation(operations, "?" + memberAccess.NameSpace.Text, memberAccess.Identifier.Text, jumpLabel);
    }

    private void AddCompoundMemberAccessExpression(List<Sir0Operation> operations, CompoundMemberAccessExpressionSyntax memberAccess, string? jumpLabel)
    {
        AddExpressionOperations(operations, memberAccess.Eval, jumpLabel);
        AddStringLiteralOperation(operations, "?_eval_", memberAccess.Identifier.Text, null);
    }

    private void AddNativeMethodInvocation(List<Sir0Operation> operations, NativeMethodInvocationExpressionSyntax expression, string? jumpLabel)
    {
        AddNativeMethodInvocationName(operations, expression.Name, jumpLabel);
        AddOperation(operations, 0x23);

        if (expression.Parameters.ParameterList is not null)
            foreach (var parameter in expression.Parameters.ParameterList.Elements)
                AddExpressionOperations(operations, parameter, null);

        AddOperation(operations, 0x24);
    }

    private void AddNativeMethodInvocationName(List<Sir0Operation> operations, ExpressionSyntax name, string? jumpLabel)
    {
        switch (name)
        {
            case LiteralExpressionSyntax literal:
                AddLiteralExpression(operations, literal, jumpLabel);
                break;

            case SimpleMemberAccessExpressionSyntax memberAccess:
                AddSimpleMemberAccessExpression(operations, memberAccess, jumpLabel);
                break;

            case QualifiedMemberAccessExpressionSyntax memberAccess:
                AddQualifiedMemberAccessExpression(operations, memberAccess, jumpLabel);
                break;

            case CompoundMemberAccessExpressionSyntax memberAccess:
                AddCompoundMemberAccessExpression(operations, memberAccess, jumpLabel);
                break;

            default:
                throw CreateException("Could not process native method invocation.", name.Location,
                    SyntaxTokenKind.StringLiteral, SyntaxTokenKind.ParenOpen);
        }
    }

    private string? AddIfOperations(List<Sir0Operation> operations, IfStatementSyntax ifStatement, HashSet<string> exportedLabels, string? jumpLabel)
    {
        if (TryGetLoopControlTarget(ifStatement.Body, out string loopTargetLabel, out bool isBreak))
        {
            AddExpressionOperations(operations, ifStatement.Condition, jumpLabel);
            AddConditionalJumpOnTrue(operations, null, loopTargetLabel);

            if (isBreak)
                _loopContextStack.Peek().BreakUsed = true;

            return null;
        }

        string endLabel = CreateLabel();

        AddExpressionOperations(operations, ifStatement.Condition, jumpLabel);

        int conditionIndex = operations.Count;
        AddConditionalJumpOnFalse(operations, null, endLabel);

        string? danglingLabel = CreateOperationsInternal(operations, ifStatement.Body, exportedLabels, null);

        if (danglingLabel is not null)
        {
            UpdateJumpTarget(operations, conditionIndex, danglingLabel);
            endLabel = danglingLabel;
        }

        return endLabel;
    }

    private string? AddIfElseOperations(List<Sir0Operation> operations, IfElseStatementSyntax ifElseStatement, HashSet<string> exportedLabels, string? jumpLabel)
    {
        bool hasThenControl = TryGetLoopControlTarget(ifElseStatement.Body, out string thenTargetLabel, out bool thenBreak);
        bool hasElseControl = TryGetLoopControlTarget(ifElseStatement.ElseBody, out string elseTargetLabel, out bool elseBreak);
        if (hasThenControl || hasElseControl)
        {
            AddExpressionOperations(operations, ifElseStatement.Condition, jumpLabel);

            if (hasThenControl && hasElseControl)
            {
                AddConditionalJumpOnTrue(operations, null, thenTargetLabel);
                AddUnconditionalJump(operations, null, elseTargetLabel);

                if (thenBreak || elseBreak)
                    _loopContextStack.Peek().BreakUsed = true;

                return null;
            }

            if (hasThenControl)
            {
                AddConditionalJumpOnTrue(operations, null, thenTargetLabel);

                if (thenBreak)
                    _loopContextStack.Peek().BreakUsed = true;

                return CreateOperationsInternal(operations, ifElseStatement.ElseBody, exportedLabels, null);
            }

            AddConditionalJumpOnFalse(operations, null, elseTargetLabel);

            if (elseBreak)
                _loopContextStack.Peek().BreakUsed = true;

            return CreateOperationsInternal(operations, ifElseStatement.Body, exportedLabels, null);
        }

        string elseLabel = CreateLabel();
        string endLabel = CreateLabel();

        AddExpressionOperations(operations, ifElseStatement.Condition, jumpLabel);
        AddConditionalJumpOnFalse(operations, null, elseLabel);

        string? danglingThenLabel = CreateOperationsInternal(operations, ifElseStatement.Body, exportedLabels, null);

        int gotoIndex = operations.Count;
        AddUnconditionalJump(operations, danglingThenLabel, endLabel);

        string? danglingElseLabel = CreateOperationsInternal(operations, ifElseStatement.ElseBody, exportedLabels, elseLabel);
        if (danglingElseLabel is not null)
        {
            UpdateJumpTarget(operations, gotoIndex, danglingElseLabel);
            endLabel = danglingElseLabel;
        }

        return endLabel;
    }

    private string? AddIfNotOperations(List<Sir0Operation> operations, IfNotStatementSyntax ifNotStatement, HashSet<string> exportedLabels, string? jumpLabel)
    {
        if (TryGetLoopControlTarget(ifNotStatement.Body, out string loopTargetLabel, out bool isBreak))
        {
            AddExpressionOperations(operations, ifNotStatement.Condition, jumpLabel);
            AddConditionalJumpOnFalse(operations, null, loopTargetLabel);

            if (isBreak)
                _loopContextStack.Peek().BreakUsed = true;

            return null;
        }

        string endLabel = CreateLabel();

        AddExpressionOperations(operations, ifNotStatement.Condition, jumpLabel);

        int conditionIndex = operations.Count;
        AddConditionalJumpOnTrue(operations, null, endLabel);

        string? danglingLabel = CreateOperationsInternal(operations, ifNotStatement.Body, exportedLabels, null);

        if (danglingLabel is not null)
        {
            UpdateJumpTarget(operations, conditionIndex, danglingLabel);
            endLabel = danglingLabel;
        }

        return endLabel;
    }

    private string? AddIfNotElseOperations(List<Sir0Operation> operations, IfNotElseStatementSyntax ifNotElseStatement, HashSet<string> exportedLabels, string? jumpLabel)
    {
        bool hasThenControl = TryGetLoopControlTarget(ifNotElseStatement.Body, out string thenTargetLabel, out bool thenBreak);
        bool hasElseControl = TryGetLoopControlTarget(ifNotElseStatement.ElseBody, out string elseTargetLabel, out bool elseBreak);
        if (hasThenControl || hasElseControl)
        {
            AddExpressionOperations(operations, ifNotElseStatement.Condition, jumpLabel);

            if (hasThenControl && hasElseControl)
            {
                AddConditionalJumpOnFalse(operations, null, thenTargetLabel);
                AddUnconditionalJump(operations, null, elseTargetLabel);

                if (thenBreak || elseBreak)
                    _loopContextStack.Peek().BreakUsed = true;

                return null;
            }

            if (hasThenControl)
            {
                AddConditionalJumpOnFalse(operations, null, thenTargetLabel);

                if (thenBreak)
                    _loopContextStack.Peek().BreakUsed = true;

                return CreateOperationsInternal(operations, ifNotElseStatement.ElseBody, exportedLabels, null);
            }

            AddConditionalJumpOnTrue(operations, null, elseTargetLabel);

            if (elseBreak)
                _loopContextStack.Peek().BreakUsed = true;

            return CreateOperationsInternal(operations, ifNotElseStatement.Body, exportedLabels, null);
        }

        string elseLabel = CreateLabel();
        string endLabel = CreateLabel();

        AddExpressionOperations(operations, ifNotElseStatement.Condition, jumpLabel);
        AddConditionalJumpOnTrue(operations, null, elseLabel);

        string? danglingThenLabel = CreateOperationsInternal(operations, ifNotElseStatement.Body, exportedLabels, null);

        int gotoIndex = operations.Count;
        AddUnconditionalJump(operations, danglingThenLabel, endLabel);

        string? danglingElseLabel = CreateOperationsInternal(operations, ifNotElseStatement.ElseBody, exportedLabels, elseLabel);
        if (danglingElseLabel is not null)
        {
            UpdateJumpTarget(operations, gotoIndex, danglingElseLabel);
            endLabel = danglingElseLabel;
        }

        return endLabel;
    }

    private string AddSwitchOperations(List<Sir0Operation> operations, SwitchStatementSyntax switchStatement, HashSet<string> exportedLabels, string? jumpLabel)
    {
        string endLabel = CreateLabel();
        _switchContextStack.Push(new SwitchEmissionContext(endLabel));

        string tempName = CreateSwitchTempName();
        LiteralExpressionSyntax switchVariable = CreateStringLiteralExpression(tempName);
        AddExpressionOperations(operations, switchVariable, jumpLabel);
        AddExpressionOperations(operations, switchStatement.Expression, null);
        AddOperation(operations, 0x20);
        AddOperation(operations, 0x27);

        var cases = new List<(CaseStatementSyntax Case, string Label)>();
        foreach (CaseStatementSyntax @case in switchStatement.Cases)
            cases.Add((@case, CreateLabel()));

        foreach ((CaseStatementSyntax @case, string label) in cases)
        {
            AddExpressionOperations(operations, switchVariable, null);
            AddExpressionOperations(operations, @case.Label, null);
            AddOperation(operations, 0x1A);
            AddConditionalJumpOnTrue(operations, null, label);
        }

        AddUnconditionalJump(operations, null, endLabel);

        foreach ((CaseStatementSyntax @case, string label) in cases)
        {
            bool endsWithBreak = @case.Statements.Count > 0 && @case.Statements[^1] is BreakStatementSyntax;
            string? danglingLabel = CreateOperationsInternal(operations, @case.Statements, exportedLabels, label);
            if (!endsWithBreak)
                AddUnconditionalJump(operations, danglingLabel, endLabel);
        }

        _switchContextStack.Pop();

        return endLabel;
    }

    private LiteralExpressionSyntax CreateStringLiteralExpression(string value)
    {
        SyntaxToken literal = syntaxFactory.StringLiteral(value);
        return new LiteralExpressionSyntax(literal);
    }

    private string CreateSwitchTempName()
    {
        return $"?t_{_switchTempCounter++}_";
    }

    private bool TryGetLoopControlTarget(BlockExpression body, out string targetLabel, out bool isBreak)
    {
        targetLabel = string.Empty;
        isBreak = false;

        if (_loopContextStack.Count == 0)
            return false;

        if (body.Statements.Count != 1)
            return false;

        LoopEmissionContext loopContext = _loopContextStack.Peek();
        switch (body.Statements[0])
        {
            case BreakStatementSyntax:
                targetLabel = loopContext.BreakLabel;
                isBreak = true;
                return true;

            case ContinueStatementSyntax:
                targetLabel = loopContext.StartLabel;
                return true;

            default:
                return false;
        }
    }

    private string? AddDoWhileOperations(List<Sir0Operation> operations, DoWhileStatementSyntax doWhileStatement, HashSet<string> exportedLabels, string? jumpLabel)
    {
        string startLabel = jumpLabel ?? CreateLabel();
        string breakLabel = CreateLabel();

        var loopContext = new LoopEmissionContext(startLabel, breakLabel);
        _loopContextStack.Push(loopContext);

        string? danglingLabel = CreateOperationsInternal(operations, doWhileStatement.Body, exportedLabels, startLabel);

        // HINT: "while (false)" is emitted as no additional operations
        if (doWhileStatement.Condition is LiteralExpressionSyntax { Literal.RawKind: (int)SyntaxTokenKind.TrueKeyword })
            AddUnconditionalJump(operations, danglingLabel, startLabel);
        else if (doWhileStatement.Condition is not LiteralExpressionSyntax { Literal.RawKind: (int)SyntaxTokenKind.FalseKeyword })
            AddConditionalJumpOnTrue(operations, danglingLabel, startLabel);

        _loopContextStack.Pop();

        return loopContext.BreakUsed ? breakLabel : null;
    }

    private string? AddDoWhileNotOperations(List<Sir0Operation> operations, DoWhileNotStatementSyntax doWhileNotStatement, HashSet<string> exportedLabels, string? jumpLabel)
    {
        string startLabel = jumpLabel ?? CreateLabel();
        string breakLabel = CreateLabel();
        var loopContext = new LoopEmissionContext(startLabel, breakLabel);
        _loopContextStack.Push(loopContext);

        string? danglingLabel = CreateOperationsInternal(operations, doWhileNotStatement.Body, exportedLabels, startLabel);

        // HINT: "while not (true)" is emitted as no additional operations
        if (doWhileNotStatement.Condition is LiteralExpressionSyntax { Literal.RawKind: (int)SyntaxTokenKind.FalseKeyword })
            AddUnconditionalJump(operations, danglingLabel, startLabel);
        else if (doWhileNotStatement.Condition is not LiteralExpressionSyntax { Literal.RawKind: (int)SyntaxTokenKind.TrueKeyword })
            AddConditionalJumpOnFalse(operations, danglingLabel, startLabel);

        _loopContextStack.Pop();

        return loopContext.BreakUsed ? breakLabel : null;
    }

    private string? AddBreakOperation(List<Sir0Operation> operations, string? jumpLabel)
    {
        if (_switchContextStack.Count > 0)
        {
            AddUnconditionalJump(operations, jumpLabel, _switchContextStack.Peek().EndLabel);
            return null;
        }

        if (_loopContextStack.Count == 0)
            throw new InvalidOperationException("Break statement is only valid within a loop.");

        LoopEmissionContext loopContext = _loopContextStack.Peek();
        AddUnconditionalJump(operations, jumpLabel, loopContext.BreakLabel);
        loopContext.BreakUsed = true;

        return null;
    }

    private string? AddContinueOperation(List<Sir0Operation> operations, string? jumpLabel)
    {
        if (_loopContextStack.Count == 0)
            throw new InvalidOperationException("Continue statement is only valid within a loop.");

        LoopEmissionContext loopContext = _loopContextStack.Peek();
        AddUnconditionalJump(operations, jumpLabel, loopContext.StartLabel);

        return null;
    }

    private void AddReturnOperations(List<Sir0Operation> operations, ReturnStatementSyntax returnStatement, string? jumpLabel)
    {
        if (returnStatement.Expression is not null)
        {
            AddStringLiteralOperation(operations, "?4", jumpLabel);
            AddExpressionOperations(operations, returnStatement.Expression, null);
            AddOperation(operations, 0x20);
            AddOperation(operations, 0x27);

            jumpLabel = null;
        }

        AddOperation(operations, 0x30, jumpLabel);
    }

    private static void AddNumericLiteralOperation(List<Sir0Operation> operations, float number, string? jumpLabel)
    {
        operations.Add(new Sir0Operation(jumpLabel, 0xF0, [number]));
    }

    private static void AddStringLiteralOperation(List<Sir0Operation> operations, string text1, string text2, string? jumpLabel)
    {
        if (text1[0] is '?' or '@' or ':' or '~' or '^' or '$' or '&')
        {
            operations.Add(new Sir0Operation(jumpLabel, 0xF4, [text1, text2]));
            return;
        }

        operations.Add(new Sir0Operation(jumpLabel, 0xF1, [text1]));
    }

    private static void AddStringLiteralOperation(List<Sir0Operation> operations, string text, string? jumpLabel)
    {
        if (text[0] is '?' or '@' or ':' or '~' or '^' or '$' or '&')
        {
            var splitIndex = text.IndexOf("::", StringComparison.OrdinalIgnoreCase);
            if (splitIndex >= 0)
            {
                operations.Add(new Sir0Operation(jumpLabel, 0xF4, [text[..splitIndex], text[(splitIndex + 2)..]]));
                return;
            }

            operations.Add(new Sir0Operation(jumpLabel, 0xF4, [text]));
            return;
        }

        operations.Add(new Sir0Operation(jumpLabel, 0xF1, [text]));
    }

    private static void AddOperation(List<Sir0Operation> operations, byte operation, string? jumpLabel = null)
    {
        operations.Add(new Sir0Operation(jumpLabel, operation, []));
    }

    private static void AddAsyncStartOperation(List<Sir0Operation> operations, string? jumpLabel)
    {
        operations.Add(new Sir0Operation(jumpLabel, 0x2B, [2]));
    }

    private static void AddAsyncEndOperation(List<Sir0Operation> operations, string? jumpLabel)
    {
        operations.Add(new Sir0Operation(jumpLabel, 0x2C, [2]));
    }

    private static void AddGotoOperation(List<Sir0Operation> operations, string? jumpLabel, string targetLabel)
    {
        operations.Add(new Sir0Operation(jumpLabel, 0x33, [targetLabel]));
    }

    private static void AddGotoLabelOperation(List<Sir0Operation> operations, string? jumpLabel, string targetLabel)
    {
        operations.Add(new Sir0Operation(jumpLabel, 0x34, [targetLabel]));
    }

    private static void AddUnconditionalJump(List<Sir0Operation> operations, string? jumpLabel, string targetLabel)
    {
        operations.Add(new Sir0Operation(jumpLabel, 0x35, [targetLabel]));
    }

    private static void AddConditionalJumpOnTrue(List<Sir0Operation> operations, string? jumpLabel, string targetLabel)
    {
        operations.Add(new Sir0Operation(jumpLabel, 0x36, [targetLabel]));
    }

    private static void AddConditionalJumpOnFalse(List<Sir0Operation> operations, string? jumpLabel, string targetLabel)
    {
        operations.Add(new Sir0Operation(jumpLabel, 0x37, [targetLabel]));
    }

    private static void BackPropagateJumpLabel(List<Sir0Operation> operations, string jumpLabel)
    {
        for (int i = operations.Count - 1; i >= 0; i--)
        {
            Sir0Operation operation = operations[i];
            if (operation.Label is not null)
                continue;

            operations[i] = operation with { Label = jumpLabel };
            return;
        }

        throw new InvalidOperationException($"Could not back propagate jump label {jumpLabel}.");
    }

    private static void UpdateJumpTarget(List<Sir0Operation> operations, int operationIndex, string targetLabel)
    {
        Sir0Operation operation = operations[operationIndex];
        operations[operationIndex] = operation with { Arguments = [targetLabel] };
    }

    private object GetArgument(LiteralExpressionSyntax literal)
    {
        switch (literal.Literal.RawKind)
        {
            case (int)SyntaxTokenKind.NumericLiteral:
                return GetNumericLiteral(literal);

            case (int)SyntaxTokenKind.FloatingNumericLiteral:
                return GetFloatingNumericLiteral(literal);

            case (int)SyntaxTokenKind.StringLiteral:
                return GetStringLiteral(literal);

            default:
                throw CreateException($"Invalid operation argument {(SyntaxTokenKind)literal.Literal.RawKind}.", literal.Location);
        }
    }

    private byte GetOperation(NameSyntax name)
    {
        string composedName = GetName(name);

        if (_subPattern.IsMatch(composedName))
            return (byte)GetNumberFromStringEnd(composedName);

        throw CreateException($"Could not determine operation from {composedName}.", name.Location);
    }

    private string GetName(NameSyntax name)
    {
        switch (name)
        {
            case SimpleNameSyntax simpleName:
                return simpleName.Identifier.Text;

            case QualifiedNameSyntax qualifiedName:
                return GetName(qualifiedName.Left) + "." + GetName(qualifiedName.Right);

            default:
                throw CreateException("Invalid name syntax.", name.Location);
        }
    }

    private int GetNumericLiteral(LiteralExpressionSyntax literal)
    {
        if (literal.Literal.RawKind != (int)SyntaxTokenKind.NumericLiteral)
            throw CreateException($"Invalid literal {(SyntaxTokenKind)literal.Literal.RawKind}.", literal.Location, SyntaxTokenKind.NumericLiteral);

        return literal.Literal.Text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ?
            int.Parse(literal.Literal.Text[2..], NumberStyles.HexNumber) :
            int.Parse(literal.Literal.Text);
    }

    private float GetFloatingNumericLiteral(LiteralExpressionSyntax literal)
    {
        if (literal.Literal.RawKind != (int)SyntaxTokenKind.FloatingNumericLiteral)
            throw CreateException($"Invalid literal {(SyntaxTokenKind)literal.Literal.RawKind}.", literal.Location, SyntaxTokenKind.FloatingNumericLiteral);

        return float.Parse(literal.Literal.Text[..^1], CultureInfo.GetCultureInfo("en-gb"));
    }

    private string GetStringLiteral(LiteralExpressionSyntax literal)
    {
        if (literal.Literal.RawKind != (int)SyntaxTokenKind.StringLiteral)
            throw CreateException($"Invalid literal {(SyntaxTokenKind)literal.Literal.RawKind}.", literal.Location, SyntaxTokenKind.StringLiteral);

        return literal.Literal.Text[1..^1].Replace("\\\"", "\"");
    }

    private int GetNumberFromStringEnd(string text)
    {
        int startIndex = text.Length;
        while (text[startIndex - 1] >= '0' && text[startIndex - 1] <= '9')
            startIndex--;

        return int.Parse(text[startIndex..]);
    }

    private string CreateLabel()
    {
        return $"@{_labelCounter++:000}@";
    }

    private sealed class LoopEmissionContext
    {
        public string StartLabel { get; }
        public string BreakLabel { get; }
        public bool BreakUsed { get; set; }

        public LoopEmissionContext(string startLabel, string breakLabel)
        {
            StartLabel = startLabel;
            BreakLabel = breakLabel;
        }
    }

    private sealed class SwitchEmissionContext
    {
        public string EndLabel { get; }

        public SwitchEmissionContext(string endLabel)
        {
            EndLabel = endLabel;
        }
    }

    private Exception CreateException(string message, SyntaxLocation location, params SyntaxTokenKind[] expected)
    {
        message = $"{message} (Line {location.Line}, Column {location.Column})";

        if (expected.Length > 0)
        {
            message = expected.Length == 1 ?
                $"{message} (Expected {expected[0]})" :
                $"{message} (Expected any of {string.Join(", ", expected)})";
        }

        return new InvalidOperationException(message);
    }
}