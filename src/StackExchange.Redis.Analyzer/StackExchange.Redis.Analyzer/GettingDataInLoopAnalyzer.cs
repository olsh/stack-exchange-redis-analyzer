using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StackExchange.Redis.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class GettingDataInLoopAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SER002";

        public const string MessageFormat = "Method {0} is called in a loop, consider using a batch overload {1} instead";

        private const string Category = "API Guidance";

        private static readonly LocalizableString Description =
            "Getting data in a loop can be slow, consider using a batch instead and overload with array of keys.";

        private const string Title = "Getting data in a loop can be slow, consider using a batch instead and overload with array of keys";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            true,
            Description,
            "https://github.com/olsh/stack-exchange-redis-analyzer#ser002");

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var expressionSyntax = (InvocationExpressionSyntax)context.Node;
            var memberAccessExpressionSyntax = expressionSyntax.Expression as MemberAccessExpressionSyntax;
            if (memberAccessExpressionSyntax == null)
            {
                return;
            }

            var overload = FindMethodWithArrayOverload(expressionSyntax, context.SemanticModel);
            if (overload == null)
            {
                return;
            }

            if (!IsInLoop(expressionSyntax))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), memberAccessExpressionSyntax.Name, overload.OriginalDefinition.ToString()));
        }

        private bool IsInLoop(InvocationExpressionSyntax expressionSyntax)
        {
            if (expressionSyntax.FirstAncestorOrSelf<ForStatementSyntax>() != null)
            {
                return true;
            }

            if (expressionSyntax.FirstAncestorOrSelf<ForEachStatementSyntax>() != null)
            {
                return true;
            }

            if (expressionSyntax.FirstAncestorOrSelf<WhileStatementSyntax>() != null)
            {
                return true;
            }

            return false;
        }

        private IMethodSymbol FindMethodWithArrayOverload(InvocationExpressionSyntax expressionSyntax, SemanticModel contextSemanticModel)
        {
            var methodSymbol = contextSemanticModel.GetSymbolInfo(expressionSyntax).Symbol as IMethodSymbol;
            var containingType = methodSymbol?.ContainingType;
            if (containingType == null)
            {
                return null;
            }

            if (containingType.Name != "IDatabase" && containingType.Name != "IDatabaseAsync")
            {
                return null;
            }

            var containingNamespace = containingType.ContainingNamespace;
            if (containingNamespace == null)
            {
                return null;
            }

            if (containingNamespace.Name != "Redis" || containingNamespace.ContainingNamespace?.Name != "StackExchange")
            {
                return null;
            }

            var parameters = methodSymbol.Parameters;
            if (parameters.Length == 0)
            {
                return null;
            }

            // Check method overloads
            var overloads = containingType
                .GetMembers(methodSymbol.Name)
                .OfType<IMethodSymbol>()
                .Where(o => o.Equals(methodSymbol, SymbolEqualityComparer.Default) == false);
            foreach (var parameter in parameters)
            {
                if (parameter.Type is IArrayTypeSymbol)
                {
                    continue;
                }

                foreach (var overload in overloads)
                {
                    var matchedParameter = overload.Parameters.SingleOrDefault(p => p.Ordinal == parameter.Ordinal);
                    if (matchedParameter != null && IsMethodIsArrayOf(matchedParameter, parameter))
                    {
                        return overload;
                    }
                }
            }

            return null;
        }

        private bool IsMethodIsArrayOf(IParameterSymbol arrayParameter, IParameterSymbol baseParameter)
        {
            if (!(arrayParameter.Type is IArrayTypeSymbol arrayElementType))
            {
                return false;
            }

            return arrayElementType.ElementType.Equals(baseParameter.Type, SymbolEqualityComparer.Default);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
    }
}
