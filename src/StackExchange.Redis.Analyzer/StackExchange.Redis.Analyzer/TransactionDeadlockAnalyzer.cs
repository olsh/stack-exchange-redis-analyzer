using System;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StackExchange.Redis.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TransactionDeadlockAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SER001";

        public const string MessageFormat = "Method {0} is blocked";

        private const string Category = "API Guidance";

        private static readonly LocalizableString Description =
            "Async methods on ITransaction type shouldn't be blocked";

        private static readonly string
            Title = "Async method is blocked on transaction before the transaction execution";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            true,
            Description,
            "https://github.com/olsh/stack-exchange-redis-analyzer#ser001");

        private static readonly string[] BlockingTaskMethods = { "WaitAll", "WhenAll", "WhenAny", "WaitAny" };

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeAwaitExpression, SyntaxKind.AwaitExpression);
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeArgumentExpression, SyntaxKind.Argument);
        }

        private void AnalyzeArgumentExpression(SyntaxNodeAnalysisContext context)
        {
            var argumentSyntax = (ArgumentSyntax)context.Node;

            foreach (var accessExpressionSyntax in argumentSyntax
                .DescendantNodes(node => !node.IsKind(SyntaxKind.ArgumentList))
                .OfType<MemberAccessExpressionSyntax>())
            {
                if (IsDangerousMethod(context, accessExpressionSyntax)
                    && IsArgumentOfBlockedMethod(context, argumentSyntax))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(Rule, context.Node.GetLocation(), accessExpressionSyntax.Name.ToString()));

                    return;
                }
            }
        }

        private void AnalyzeAwaitExpression(SyntaxNodeAnalysisContext context)
        {
            var awaitExpressionSyntax = (AwaitExpressionSyntax)context.Node;

            foreach (var memberAccessExpressionSyntax in awaitExpressionSyntax
                .DescendantNodes(node => !node.IsKind(SyntaxKind.ArgumentList))
                .OfType<MemberAccessExpressionSyntax>())
            {
                if (IsDangerousMethod(context, memberAccessExpressionSyntax))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            Rule,
                            context.Node.GetLocation(),
                            memberAccessExpressionSyntax.Name.ToString()));

                    return;
                }
            }
        }

        private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var invocationExpressionSyntax = (InvocationExpressionSyntax)context.Node;

            var memberAccessExpressionSyntax = invocationExpressionSyntax.Expression as MemberAccessExpressionSyntax;
            if (memberAccessExpressionSyntax == null)
            {
                return;
            }

            if (IsDangerousMethod(context, memberAccessExpressionSyntax)
                && invocationExpressionSyntax.Parent is MemberAccessExpressionSyntax parentMemberAccessExpressionSyntax
                && IsBlockedMethod(parentMemberAccessExpressionSyntax))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rule, context.Node.GetLocation(), memberAccessExpressionSyntax.Name.ToString()));
            }
        }

        private bool IsArgumentOfBlockedMethod(SyntaxNodeAnalysisContext context, ArgumentSyntax argumentSyntax)
        {
            var taskType = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");

            bool BlockedTaskMethod(ISymbol symbol)
            {
                if (!(symbol is IMethodSymbol methodSymbol))
                {
                    return false;
                }

                if (methodSymbol.ContainingType.Equals(taskType) && BlockingTaskMethods.Contains(methodSymbol.MetadataName))
                {
                    return true;
                }

                return false;
            }

            var invocationExpressionSyntax = argumentSyntax.Parent?.Parent as InvocationExpressionSyntax;
            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocationExpressionSyntax);

            return BlockedTaskMethod(symbolInfo.Symbol) || (symbolInfo.CandidateSymbols.Length > 0 && symbolInfo.CandidateSymbols.All(BlockedTaskMethod));
        }

        private bool IsBlockedMethod(MemberAccessExpressionSyntax invocationExpressionSyntax)
        {
            var name = invocationExpressionSyntax.Name.ToString();

            return name.Equals("wait", StringComparison.InvariantCultureIgnoreCase) || name.Equals(
                       "result",
                       StringComparison.InvariantCultureIgnoreCase);
        }

        private bool IsDangerousMethod(
            SyntaxNodeAnalysisContext context,
            MemberAccessExpressionSyntax memberAccessExpressionSyntax)
        {
            var transactionType =
                context.SemanticModel.Compilation.GetTypeByMetadataName("StackExchange.Redis.ITransaction");
            var memberName = memberAccessExpressionSyntax.Expression;
            var symbolInfo = context.SemanticModel.GetSymbolInfo(memberName);
            var methodName = memberAccessExpressionSyntax.Name.ToString();

            return symbolInfo.Symbol is ILocalSymbol singleSymbol && singleSymbol.Type.Equals(transactionType)
                                                                  && methodName != "ExecuteAsync";
        }
    }
}
