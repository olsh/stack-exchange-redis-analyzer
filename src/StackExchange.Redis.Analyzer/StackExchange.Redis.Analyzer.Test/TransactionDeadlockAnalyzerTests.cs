using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TestHelper;

namespace StackExchange.Redis.Analyzer.Test
{
    [TestClass]
    public class TransactionDeadlockAnalyzerTests : DiagnosticVerifier
    {
        protected override string TestDataFolder => "TransactionDeadlockAnalyzer";

        [TestMethod]
        public void Empty_NotTriggered()
        {
            const string test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void AwaitStringGetAsync_AnalyzerTriggered()
        {
            var code = ReadTestData("AwaitStringGetAsyncTestData.cs");

            var expected = new DiagnosticResult
                               {
                                   Id = TransactionDeadlockAnalyzer.DiagnosticId,
                                   Message = string.Format(TransactionDeadlockAnalyzer.MessageFormat, "StringGetAsync"),
                                   Severity = DiagnosticSeverity.Warning,
                                   Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 25) }
                               };

            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void ResultStringGetAsync_AnalyzerTriggered()
        {
            var code = ReadTestData("ResultStringGetAsyncTestData.cs");

            var expected = new DiagnosticResult
                               {
                                   Id = TransactionDeadlockAnalyzer.DiagnosticId,
                                   Message = string.Format(TransactionDeadlockAnalyzer.MessageFormat, "StringGetAsync"),
                                   Severity = DiagnosticSeverity.Warning,
                                   Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 21) }
                               };

            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void WaitStringGetAsync_AnalyzerTriggered()
        {
            var code = ReadTestData("WaitStringGetAsyncTestData.cs");

            var expected = new DiagnosticResult
                               {
                                   Id = TransactionDeadlockAnalyzer.DiagnosticId,
                                   Message = string.Format(TransactionDeadlockAnalyzer.MessageFormat, "StringGetAsync"),
                                   Severity = DiagnosticSeverity.Warning,
                                   Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 13) }
                               };

            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void TaskWaitAllStringGetAsync_AnalyzerTriggered()
        {
            var code = ReadTestData("TaskWaitAllStringGetAsyncTestData.cs");

            var expected = new DiagnosticResult
                               {
                                   Id = TransactionDeadlockAnalyzer.DiagnosticId,
                                   Message = string.Format(TransactionDeadlockAnalyzer.MessageFormat, "StringGetAsync"),
                                   Severity = DiagnosticSeverity.Warning,
                                   Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 26) }
                               };

            VerifyCSharpDiagnostic(code, expected);
        }

        [TestMethod]
        public void ContinueWithCallback_NotTriggered()
        {
            var code = ReadTestData("ContinueWithCallbackTestData.cs");

            VerifyCSharpDiagnostic(code);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new TransactionDeadlockAnalyzer();
        }
    }
}
