using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StackExchange.Redis.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SendingCommandsInLoopAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SER002";

        public const string MessageFormat = "Method {0} is called in a loop, consider using a batch overload {1} instead";

        private const string Category = "API Guidance";

        private static readonly LocalizableString Description =
            "Sending commands in a loop can be slow, consider using a batch overload with array of keys instead.";

        private const string Title = "Sending commands in a loop can be slow, consider using a batch overload with array of keys instead";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
#pragma warning disable RS2008
            DiagnosticId,
#pragma warning restore RS2008
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

        // ReSharper disable once CognitiveComplexity
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
                .Where(o => o.Equals(methodSymbol) == false)
                .ToArray();

            foreach (var overload in overloads)
            {
                if (IsSuitableBatchOverload(methodSymbol, overload))
                {
                    return overload;
                }
            }

            return null;
        }

        private bool IsSuitableBatchOverload(IMethodSymbol sourceMethod, IMethodSymbol overload)
        {
            var hasArrayParameter = false;
            foreach (var parameter in sourceMethod.Parameters)
            {
                var overloadParameter = overload.Parameters.SingleOrDefault(p => p.Ordinal == parameter.Ordinal);
                if (overloadParameter == null)
                {
                    return false;
                }

                if (overloadParameter.Type.Equals(parameter.Type, SymbolEqualityComparer.Default))
                {
                    continue;
                }

                if (IsMethodIsArrayOf(overloadParameter, parameter))
                {
                    hasArrayParameter = true;
                }
                else
                {
                    return false;
                }
            }

            return hasArrayParameter;
        }

        private bool IsMethodIsArrayOf(IParameterSymbol arrayParameter, IParameterSymbol baseParameter)
        {
            if (!(arrayParameter.Type is IArrayTypeSymbol arrayElementType))
            {
                return false;
            }

            return arrayElementType.ElementType.Equals(baseParameter.Type);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
    }
}
