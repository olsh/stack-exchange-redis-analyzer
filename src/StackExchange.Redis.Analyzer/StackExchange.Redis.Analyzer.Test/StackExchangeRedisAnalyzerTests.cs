using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using StackExchange.Redis.Analyzer.Test.Helpers;

using TestHelper;

namespace StackExchange.Redis.Analyzer.Test
{
    [TestClass]
    public class StackExchangeRedisAnalyzerTests : DiagnosticVerifier
    {
        [TestMethod]
        public void Empty_NotTriggered()
        {
            const string Test = @"";

            VerifyCSharpDiagnostic(Test);
        }

        [TestMethod]
        public void AwaitStringGetAsync_AnalyzerTriggered()
        {
            var code = EmbeddedResourceHelper.ReadTestData("AwaitStringGetAsyncTestData.cs");

            var expected = new DiagnosticResult
                               {
                                   Id = StackExchangeRedisAnalyzer.DiagnosticId,
                                   Message = string.Format(StackExchangeRedisAnalyzer.MessageFormat, "StringGetAsync"),
                                   Severity = DiagnosticSeverity.Warning,
                                   Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 25) }
                               };

            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void ResultStringGetAsync_AnalyzerTriggered()
        {
            var code = EmbeddedResourceHelper.ReadTestData("ResultStringGetAsyncTestData.cs");

            var expected = new DiagnosticResult
                               {
                                   Id = StackExchangeRedisAnalyzer.DiagnosticId,
                                   Message = string.Format(StackExchangeRedisAnalyzer.MessageFormat, "StringGetAsync"),
                                   Severity = DiagnosticSeverity.Warning,
                                   Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 21) }
                               };

            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void WaitStringGetAsync_AnalyzerTriggered()
        {
            var code = EmbeddedResourceHelper.ReadTestData("WaitStringGetAsyncTestData.cs");

            var expected = new DiagnosticResult
                               {
                                   Id = StackExchangeRedisAnalyzer.DiagnosticId,
                                   Message = string.Format(StackExchangeRedisAnalyzer.MessageFormat, "StringGetAsync"),
                                   Severity = DiagnosticSeverity.Warning,
                                   Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 13) }
                               };

            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void TaskWaitAllStringGetAsync_AnalyzerTriggered()
        {
            var code = EmbeddedResourceHelper.ReadTestData("TaskWaitAllStringGetAsyncTestData.cs");

            var expected = new DiagnosticResult
                               {
                                   Id = StackExchangeRedisAnalyzer.DiagnosticId,
                                   Message = string.Format(StackExchangeRedisAnalyzer.MessageFormat, "StringGetAsync"),
                                   Severity = DiagnosticSeverity.Warning,
                                   Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 26) }
                               };

            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void ContinueWithCallback_NotTriggered()
        {
            var code = EmbeddedResourceHelper.ReadTestData("ContinueWithCallbackTestData.cs");

            VerifyCSharpDiagnostic(code);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new StackExchangeRedisAnalyzer();
        }
    }
}
