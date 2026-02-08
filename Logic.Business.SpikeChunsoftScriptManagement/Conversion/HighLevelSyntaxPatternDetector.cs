using System.Diagnostics.CodeAnalysis;
using Logic.Business.SpikeChunsoftScriptManagement.DataClasses.Conversion;
using Logic.Business.SpikeChunsoftScriptManagement.Enums.Conversion;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

namespace Logic.Business.SpikeChunsoftScriptManagement.Conversion;

internal delegate IReadOnlyList<StatementSyntax> BuildStatementsRangeDelegate(IReadOnlyList<StatementBlock> blocks,
    Dictionary<string, int> labelLookup, Dictionary<int, LoopBound> loopBounds, int startIndex, int endIndex, string[] exportedLabels,
    out ExpressionSyntax? condition, bool skipLoopStart, LoopContext? loopContext);

internal delegate IReadOnlyList<StatementSyntax> CreateStatementsFromBlockDelegate(IReadOnlyList<StatementBlock> blocks,
    IReadOnlyDictionary<string, int>? labelLookup, LoopContext? loopContext, int blockIndex, bool skipTerminalJump,
    string[] exportedLabels, out ExpressionSyntax? condition);

internal class HighLevelSyntaxPatternDetector
{
    private readonly HighLevelSyntaxFactory _factory;
    private readonly BuildStatementsRangeDelegate _buildStatementsRange;
    private readonly CreateStatementsFromBlockDelegate _createStatementsFromBlock;

    public HighLevelSyntaxPatternDetector(HighLevelSyntaxFactory factory, BuildStatementsRangeDelegate buildStatementsRange,
        CreateStatementsFromBlockDelegate createStatementsFromBlock)
    {
        _factory = factory;
        _buildStatementsRange = buildStatementsRange;
        _createStatementsFromBlock = createStatementsFromBlock;
    }

    public bool TryBuildSwitchStatement(IReadOnlyList<StatementBlock> blocks, Dictionary<string, int> labelLookup,
        Dictionary<int, LoopBound> loopBounds, int index, int endIndex, LoopContext? loopContext, IReadOnlyList<StatementSyntax> leadingStatements,
        ExpressionSyntax? condition, string[] exportedLabels, [NotNullWhen(true)] out StatementSyntax? statement, out int nextIndex, out int removeLeadingStatements)
    {
        statement = null;
        nextIndex = index + 1;
        removeLeadingStatements = 0;

        StatementBlock block = blocks[index];
        if (block.TerminalCommand is not 0x36 || block.JumpLabel is null)
            return false;

        if (condition is null)
            return false;

        if (!TryGetSwitchAssignment(leadingStatements, out LiteralExpressionSyntax switchVariable, out ExpressionSyntax switchValue))
            return false;

        if (!TryGetSwitchCaseLabel(condition, switchVariable, out ExpressionSyntax firstCaseLabel))
            return false;

        if (!labelLookup.TryGetValue(block.JumpLabel, out int firstCaseTarget))
            return false;

        var caseMatches = new List<(ExpressionSyntax Label, int TargetIndex)>
        {
            (firstCaseLabel, firstCaseTarget)
        };

        int currentIndex = index + 1;
        for (; currentIndex < endIndex; currentIndex++)
        {
            StatementBlock nextBlock = blocks[currentIndex];
            if (nextBlock.TerminalCommand is not 0x36 || nextBlock.JumpLabel is null)
                break;

            IReadOnlyList<StatementSyntax> blockStatements = _createStatementsFromBlock(blocks, labelLookup, loopContext, currentIndex, true,
                exportedLabels, out ExpressionSyntax? blockCondition);
            if (blockStatements.Count > 0)
                return false;

            if (blockCondition is null)
                return false;

            if (!TryGetSwitchCaseLabel(blockCondition, switchVariable, out ExpressionSyntax caseLabel))
                return false;

            if (!labelLookup.TryGetValue(nextBlock.JumpLabel, out int targetIndex))
                return false;

            caseMatches.Add((caseLabel, targetIndex));
        }

        if (caseMatches.Count <= 0)
            return false;

        if (caseMatches.Select(match => match.TargetIndex).Distinct().Count() != caseMatches.Count)
            return false;

        if (currentIndex >= endIndex)
            return false;

        StatementBlock endJumpBlock = blocks[currentIndex];
        if (endJumpBlock.TerminalCommand is not 0x35 || endJumpBlock.JumpLabel is null)
            return false;

        if (!labelLookup.TryGetValue(endJumpBlock.JumpLabel, out int endLabelIndex))
            return false;

        if (endLabelIndex <= currentIndex || endLabelIndex > endIndex)
            return false;

        var caseStartIndices = caseMatches.Select(match => match.TargetIndex).Distinct().OrderBy(target => target).ToList();
        if (caseStartIndices.Count <= 0)
            return false;

        if (caseStartIndices[0] <= currentIndex || caseStartIndices[^1] >= endLabelIndex)
            return false;

        var caseBodies = new Dictionary<int, IReadOnlyList<StatementSyntax>>();
        for (var caseIndex = 0; caseIndex < caseStartIndices.Count; caseIndex++)
        {
            int caseStart = caseStartIndices[caseIndex];
            int caseEnd = caseIndex + 1 < caseStartIndices.Count ? caseStartIndices[caseIndex + 1] : endLabelIndex;
            IReadOnlyList<StatementSyntax> bodyStatements = _buildStatementsRange(blocks, labelLookup, loopBounds, caseStart, caseEnd, exportedLabels, out _,
                false, loopContext);
            caseBodies[caseStart] = bodyStatements;
        }

        var cases = new List<CaseStatementSyntax>();
        foreach ((ExpressionSyntax caseLabel, int targetIndex) in caseMatches)
        {
            IReadOnlyList<StatementSyntax> bodyStatements = caseBodies[targetIndex];
            var caseStatements = new List<StatementSyntax>(bodyStatements);
            if (caseStatements.Count <= 0 || caseStatements[^1] is not BreakStatementSyntax)
                caseStatements.Add(_factory.CreateBreakStatement());

            cases.Add(_factory.CreateCaseStatement(caseLabel, caseStatements));
        }

        statement = _factory.CreateSwitchStatement(switchValue, cases);
        nextIndex = endLabelIndex;
        removeLeadingStatements = leadingStatements.Count;
        return true;
    }

    public bool TryBuildIfStatement(IReadOnlyList<StatementBlock> blocks, Dictionary<string, int> labelLookup,
        Dictionary<int, LoopBound> loopBounds, int index, int endIndex, LoopContext? loopContext, ExpressionSyntax? condition,
        string[] exportedLabels, [NotNullWhen(true)] out StatementSyntax? statement, out int nextIndex)
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
            if (TryBuildIfElseOnFalse(blocks, labelLookup, loopBounds, thenStart, thenEnd, targetIndex, loopContext, condition, exportedLabels, out statement,
                    out nextIndex))
                return true;

            IReadOnlyList<StatementSyntax> thenStatements = _buildStatementsRange(blocks, labelLookup, loopBounds, thenStart, thenEnd, exportedLabels, out _, false,
                loopContext);
            statement = _factory.CreateIfStatement(thenStatements, condition);
            nextIndex = targetIndex;
            return true;
        }

        int elseStart = index + 1;
        int elseEnd = targetIndex;
        if (TryBuildIfElseOnTrue(blocks, labelLookup, loopBounds, elseStart, elseEnd, targetIndex, loopContext, condition, exportedLabels, out statement,
                out nextIndex))
            return true;

        IReadOnlyList<StatementSyntax> notStatements = _buildStatementsRange(blocks, labelLookup, loopBounds, elseStart, elseEnd, exportedLabels, out _, false,
            loopContext);
        statement = _factory.CreateIfNotStatement(notStatements, condition);
        nextIndex = targetIndex;
        return true;
    }

    public bool TryMergeLoopControlIf(List<StatementSyntax> statements, StatementSyntax ifStatement, out StatementSyntax mergedStatement)
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

        mergedStatement = _factory.CreateIfElseStatement(ifOnlyStatement.Body.Statements, [loopControl], controlIf.Condition);
        return true;
    }

    private bool TryBuildIfElseOnFalse(IReadOnlyList<StatementBlock> blocks, Dictionary<string, int> labelLookup,
        Dictionary<int, LoopBound> loopBounds, int thenStart, int thenEnd, int targetIndex, LoopContext? loopContext, ExpressionSyntax condition,
        string[] exportedLabels, [NotNullWhen(true)] out StatementSyntax? statement, out int nextIndex)
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

        IReadOnlyList<StatementSyntax> thenStatements = _buildStatementsRange(blocks, labelLookup, loopBounds, thenStart, thenEnd, exportedLabels, out _, false,
            loopContext);
        IReadOnlyList<StatementSyntax> elseStatements = _buildStatementsRange(blocks, labelLookup, loopBounds, targetIndex, endIndex, exportedLabels, out _, false,
            loopContext);

        if (elseStatements.Count <= 0)
            return false;

        statement = _factory.CreateIfElseStatement(thenStatements, elseStatements, condition);
        nextIndex = endIndex;
        return true;
    }

    private bool TryBuildIfElseOnTrue(IReadOnlyList<StatementBlock> blocks, Dictionary<string, int> labelLookup,
        Dictionary<int, LoopBound> loopBounds, int elseStart, int elseEnd, int targetIndex, LoopContext? loopContext, ExpressionSyntax condition,
        string[] exportedLabels, [NotNullWhen(true)] out StatementSyntax? statement, out int nextIndex)
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

        IReadOnlyList<StatementSyntax> thenStatements = _buildStatementsRange(blocks, labelLookup, loopBounds, elseStart, elseEnd, exportedLabels, out _, false,
            loopContext);
        IReadOnlyList<StatementSyntax> elseStatements = _buildStatementsRange(blocks, labelLookup, loopBounds, targetIndex, endIndex, exportedLabels, out _, false,
            loopContext);

        if (elseStatements.Count <= 0)
            return false;

        statement = _factory.CreateIfNotElseStatement(thenStatements, elseStatements, condition);
        nextIndex = endIndex;
        return true;
    }

    private static bool TryGetSwitchAssignment(IReadOnlyList<StatementSyntax> statements, out LiteralExpressionSyntax switchVariable,
        out ExpressionSyntax switchValue)
    {
        switchVariable = null!;
        switchValue = null!;

        if (statements.Count != 1)
            return false;

        if (statements[0] is not AssignmentStatementSyntax assignment)
            return false;

        if (assignment.Assignment.Left is not LiteralExpressionSyntax literal)
            return false;

        switchVariable = literal;
        switchValue = assignment.Assignment.Right;
        return true;
    }

    private static bool TryGetSwitchCaseLabel(ExpressionSyntax condition, LiteralExpressionSyntax switchVariable, out ExpressionSyntax caseLabel)
    {
        caseLabel = null!;

        if (condition is not BinaryExpressionSyntax binary ||
            binary.Operation.RawKind != (int)SyntaxTokenKind.EqualsEquals)
            return false;

        if (IsMatchingSwitchVariable(binary.Left, switchVariable))
        {
            caseLabel = binary.Right;
            return true;
        }

        if (IsMatchingSwitchVariable(binary.Right, switchVariable))
        {
            caseLabel = binary.Left;
            return true;
        }

        return false;
    }

    private static bool IsMatchingSwitchVariable(ExpressionSyntax expression, LiteralExpressionSyntax switchVariable)
    {
        return expression is LiteralExpressionSyntax literal &&
               literal.Literal.Text.Equals(switchVariable.Literal.Text, StringComparison.Ordinal);
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
        loopControl = kind == LoopControlKind.Break ? _factory.CreateBreakStatement() : _factory.CreateContinueStatement();
        return true;
    }
}
