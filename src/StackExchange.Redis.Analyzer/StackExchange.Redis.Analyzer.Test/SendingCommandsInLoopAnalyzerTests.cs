using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace StackExchange.Redis.Analyzer.Test;

[TestClass]
public class SendingCommandsInLoopAnalyzerTests : DiagnosticVerifier
{
    protected override string TestDataFolder => "GettingDataInLoopAnalyzer";

    [TestMethod]
    public void Empty_NotTriggered()
    {
        const string Test = @"";

        VerifyCSharpDiagnostic(Test);
    }

    [TestMethod]
    public void GetStringInLoop_AnalyzerTriggered()
    {
        var code = ReadTestData("GetStringInLoop.cs");
        var expected = new DiagnosticResult
                           {
                               Id = SendingCommandsInLoopAnalyzer.DiagnosticId,
                               Message = string.Format(SendingCommandsInLoopAnalyzer.MessageFormat, "StringGetAsync", "StackExchange.Redis.IDatabaseAsync.StringGetAsync(StackExchange.Redis.RedisKey[], StackExchange.Redis.CommandFlags)"),
                               Severity = DiagnosticSeverity.Warning,
                               Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 35) }
                           };

        VerifyCSharpDiagnostic(code, expected);
    }

    [TestMethod]
    public void SetContainsInForeach_AnalyzerTriggered()
    {
        var code = ReadTestData("SetContainsInForeach.cs");
        var expected = new DiagnosticResult
        {
            Id = SendingCommandsInLoopAnalyzer.DiagnosticId,
            Message = string.Format(SendingCommandsInLoopAnalyzer.MessageFormat, "SetContains", "StackExchange.Redis.IDatabase.SetContains(StackExchange.Redis.RedisKey, StackExchange.Redis.RedisValue[], StackExchange.Redis.CommandFlags)"),
            Severity = DiagnosticSeverity.Warning,
            Locations = new[] { new DiagnosticResultLocation("Test0.cs", 17, 29) }
        };

        VerifyCSharpDiagnostic(code, expected);
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new SendingCommandsInLoopAnalyzer();
    }
}
