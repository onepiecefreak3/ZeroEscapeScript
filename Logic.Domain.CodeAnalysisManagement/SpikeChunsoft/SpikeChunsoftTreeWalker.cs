using Logic.Domain.CodeAnalysisManagement.Contract;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses;
using Logic.Domain.CodeAnalysisManagement.Contract.DataClasses.SpikeChunsoft;

namespace Logic.Domain.CodeAnalysisManagement.SpikeChunsoft
{
    internal class SpikeChunsoftTreeWalker : ITreeWalker
    {
        public List<TSyntax> Collect<TSyntax>(SyntaxNode node, Func<TSyntax, bool>? filter = null)
            where TSyntax : SyntaxNode
        {
            var result = new List<TSyntax>();

            CollectInternal(result, node, filter);

            return result;
        }

        private void CollectInternal<TSyntax>(List<TSyntax> result, SyntaxNode node, Func<TSyntax, bool>? filter = null)
            where TSyntax : SyntaxNode
        {
            CollectNode(result, node, filter);

            switch (node)
            {
                case CodeUnitSyntax codeUnit:
                    CollectInternal(result, codeUnit.NameDeclaration, filter);
                    foreach (DeclarationSyntax member in codeUnit.Members)
                        CollectInternal(result, member, filter);
                    break;

                case NameDeclarationSyntax nameDeclaration:
                    CollectExpression(result, nameDeclaration.Name, filter);
                    break;

                case GlobalVariableDeclarationSyntax globalVariable:
                    CollectExpression(result, globalVariable.Identifier, filter);
                    break;

                case MethodDeclarationSyntax methodDeclaration:
                    CollectExpression(result, methodDeclaration.Name, filter);
                    CollectInternal(result, methodDeclaration.Parameters, filter);
                    CollectInternal(result, methodDeclaration.Body, filter);
                    break;

                case MethodDeclarationParametersSyntax methodParameters:
                    if (methodParameters.Parameters is not null)
                        CollectInternal(result, methodParameters.Parameters, filter);
                    break;

                case CommaSeparatedSyntaxList<LiteralExpressionSyntax> methodParameters:
                    foreach (LiteralExpressionSyntax element in methodParameters.Elements)
                        CollectExpression(result, element, filter);
                    break;

                case BlockExpression block:
                    foreach (StatementSyntax statement in block.Statements)
                        CollectStatement(result, statement, filter);
                    break;

                case MethodInvocationParametersSyntax methodParameters:
                    if (methodParameters.ParameterList is not null)
                        CollectInternal(result, methodParameters.ParameterList, filter);
                    break;

                case NativeMethodInvocationParametersSyntax methodParameters:
                    if (methodParameters.ParameterList is not null)
                        CollectInternal(result, methodParameters.ParameterList, filter);
                    break;

                case QualifiedNameSyntax qualifiedName:
                    CollectInternal(result, qualifiedName.Left, filter);
                    CollectInternal(result, qualifiedName.Right, filter);
                    break;

                case CaseStatementSyntax @case:
                    CollectExpression(result, @case.Label, filter);
                    foreach (StatementSyntax statement in @case.Statements)
                        CollectStatement(result, statement, filter);
                    break;
            }
        }

        private void CollectStatement<TSyntax>(List<TSyntax> result, StatementSyntax statement, Func<TSyntax, bool>? filter = null)
            where TSyntax : SyntaxNode
        {
            CollectNode(result, statement, filter);

            switch (statement)
            {
                case AssignmentStatementSyntax assignment:
                    CollectExpression(result, assignment.Assignment, filter);
                    break;

                case AsyncBlockStatement asyncBlock:
                    CollectInternal(result, asyncBlock.Body, filter);
                    break;

                case DoWhileNotStatementSyntax doWhileNot:
                    CollectInternal(result, doWhileNot.Body, filter);
                    CollectExpression(result, doWhileNot.Condition, filter);
                    break;

                case DoWhileStatementSyntax doWhile:
                    CollectInternal(result, doWhile.Body, filter);
                    CollectExpression(result, doWhile.Condition, filter);
                    break;

                case ExportedGotoLabelStatementSyntax exportedGotoLabel:
                    CollectExpression(result, exportedGotoLabel.Label, filter);
                    break;

                case GotoLabelStatementSyntax gotoLabel:
                    CollectExpression(result, gotoLabel.Label, filter);
                    break;

                case GotoStatementSyntax @goto:
                    CollectExpression(result, @goto.Label, filter);
                    break;

                case IfElseStatementSyntax ifElse:
                    CollectExpression(result, ifElse.Condition, filter);
                    CollectInternal(result, ifElse.Body, filter);
                    CollectInternal(result, ifElse.ElseBody, filter);
                    break;

                case IfNotElseStatementSyntax ifNotElse:
                    CollectExpression(result, ifNotElse.Condition, filter);
                    CollectInternal(result, ifNotElse.Body, filter);
                    CollectInternal(result, ifNotElse.ElseBody, filter);
                    break;

                case IfNotStatementSyntax ifNot:
                    CollectExpression(result, ifNot.Condition, filter);
                    CollectInternal(result, ifNot.Body, filter);
                    break;

                case IfStatementSyntax @if:
                    CollectExpression(result, @if.Condition, filter);
                    CollectInternal(result, @if.Body, filter);
                    break;

                case NativeMethodInvocationStatementSyntax nativeMethod:
                    CollectExpression(result, nativeMethod.Method, filter);
                    break;

                case PostfixStatementSyntax postfix:
                    CollectExpression(result, postfix.Postfix, filter);
                    break;

                case SwitchStatementSyntax @switch:
                    CollectExpression(result, @switch.Expression, filter);
                    foreach (CaseStatementSyntax @case in @switch.Cases)
                        CollectInternal(result, @case, filter);
                    break;
            }
        }

        private void CollectExpression<TSyntax>(List<TSyntax> result, ExpressionSyntax expression, Func<TSyntax, bool>? filter = null)
            where TSyntax : SyntaxNode
        {
            CollectNode(result, expression, filter);

            switch (expression)
            {
                case ArrayIndexExpressionSyntax arrayIndex:
                    CollectExpression(result, arrayIndex.Value, filter);
                    foreach (ArrayIndexerExpressionSyntax arrayIndexer in arrayIndex.Indexer)
                        CollectExpression(result, arrayIndexer, filter);
                    break;

                case ArrayIndexerExpressionSyntax arrayIndexer:
                    CollectExpression(result, arrayIndexer.Index, filter);
                    break;

                case AssignmentExpressionSyntax assignment:
                    CollectExpression(result, assignment.Left, filter);
                    CollectExpression(result, assignment.Right, filter);
                    break;

                case BinaryExpressionSyntax binary:
                    CollectExpression(result, binary.Left, filter);
                    CollectExpression(result, binary.Right, filter);
                    break;

                case CompoundMemberAccessExpressionSyntax compoundMember:
                    CollectExpression(result, compoundMember.Eval, filter);
                    break;

                case LogicalExpressionSyntax logical:
                    CollectExpression(result, logical.Left, filter);
                    CollectExpression(result, logical.Right, filter);
                    break;

                case MethodInvocationExpressionSyntax methodInvocation:
                    CollectInternal(result, methodInvocation.Name, filter);
                    CollectInternal(result, methodInvocation.Parameters, filter);
                    break;

                case NativeMethodInvocationExpressionSyntax nativeMethodInvocation:
                    CollectExpression(result, nativeMethodInvocation.Name, filter);
                    CollectInternal(result, nativeMethodInvocation.Parameters, filter);
                    break;

                case ParenthesizedExpressionSyntax parenthesized:
                    CollectExpression(result, parenthesized.Expression, filter);
                    break;

                case PostfixExpressionSyntax postfix:
                    CollectExpression(result, postfix.Expression, filter);
                    break;

                case UnaryExpressionSyntax unary:
                    CollectExpression(result, unary.Expression, filter);
                    break;
            }
        }

        private void CollectNode<TSyntax>(List<TSyntax> result, SyntaxNode node, Func<TSyntax, bool>? filter = null)
        {
            if (node is not TSyntax castNode)
                return;

            if (filter is null || filter(castNode))
                result.Add(castNode);
        }
    }
}
