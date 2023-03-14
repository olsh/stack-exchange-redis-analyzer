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

            var outerLoop = FindOuterLoop(expressionSyntax);
            if (outerLoop == null)
            {
                return;
            }

            var overload = FindMethodWithArrayOverload(expressionSyntax, outerLoop, context.SemanticModel);
            if (overload == null)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), memberAccessExpressionSyntax.Name, overload.OriginalDefinition.ToString()));
        }

        private StatementSyntax FindOuterLoop(InvocationExpressionSyntax expressionSyntax)
        {
            return expressionSyntax.FirstAncestorOrSelf<ForStatementSyntax>()
                   ?? (StatementSyntax)expressionSyntax.FirstAncestorOrSelf<ForEachStatementSyntax>()
                   ?? expressionSyntax.FirstAncestorOrSelf<WhileStatementSyntax>();
        }

        // ReSharper disable once CognitiveComplexity
        private IMethodSymbol FindMethodWithArrayOverload(
            InvocationExpressionSyntax expressionSyntax,
            StatementSyntax outerLoopSyntax,
            SemanticModel contextSemanticModel)
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
                .Where(o => o.Equals(methodSymbol, SymbolEqualityComparer.Default) == false)
                .ToArray();

            // Get assignments inside the loop
            var assignments = outerLoopSyntax
                .DescendantNodes()
                .OfType<AssignmentExpressionSyntax>()
                .ToArray();

            foreach (var overload in overloads)
            {
                if (IsSuitableBatchOverload(methodSymbol, overload, expressionSyntax, outerLoopSyntax, assignments, contextSemanticModel))
                {
                    return overload;
                }
            }

            return null;
        }

        // ReSharper disable once CognitiveComplexity
        private bool IsSuitableBatchOverload(
            IMethodSymbol sourceMethod,
            IMethodSymbol overload,
            InvocationExpressionSyntax expressionSyntax,
            StatementSyntax outerLoopSyntax,
            AssignmentExpressionSyntax[] assignments,
            SemanticModel contextSemanticModel)
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

                if (IsParameterIsArrayOf(overloadParameter, parameter))
                {
                    var argumentExpression = expressionSyntax.ArgumentList.Arguments[parameter.Ordinal].Expression;
                    if (IsParameterIsModifiedInsideTheLoop(contextSemanticModel, assignments, outerLoopSyntax, argumentExpression))
                    {
                        hasArrayParameter = true;
                    }
                }
                else
                {
                    return false;
                }
            }

            return hasArrayParameter;
        }

        // ReSharper disable once CognitiveComplexity
        private bool IsParameterIsModifiedInsideTheLoop(
            SemanticModel contextSemanticModel,
            AssignmentExpressionSyntax[] assignments,
            StatementSyntax outerLoopSyntax,
            ExpressionSyntax parameter)
        {
            // Check if the parameter is an invocation expression
            if (parameter is InvocationExpressionSyntax)
            {
                return true;
            }

            var parameterSymbol = contextSemanticModel.GetSymbolInfo(parameter).Symbol;
            if (parameterSymbol == null)
            {
                return false;
            }

            // Check if parameter is a constant
            if (parameterSymbol is ILocalSymbol localSymbol && localSymbol.HasConstantValue)
            {
                return false;
            }

            var constantValue = contextSemanticModel.GetConstantValue(parameter);
            if (constantValue.HasValue && constantValue.Value != null)
            {
                return false;
            }

            // Check if parameter is modified inside the loop
            foreach (var declaringSyntaxReference in parameterSymbol.DeclaringSyntaxReferences)
            {
                if (outerLoopSyntax.Contains(declaringSyntaxReference.GetSyntax()))
                {
                    return true;
                }
            }

            foreach (var assignment in assignments)
            {
                var assignmentSymbol = contextSemanticModel.GetSymbolInfo(assignment.Left).Symbol;
                if (assignmentSymbol == null)
                {
                    continue;
                }

                if (assignmentSymbol.Equals(parameterSymbol, SymbolEqualityComparer.Default))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsParameterIsArrayOf(IParameterSymbol arrayParameter, IParameterSymbol baseParameter)
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
