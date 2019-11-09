# StackExchange.Redis.Analyzer

[![Build status](https://ci.appveyor.com/api/projects/status/jyrrv262f1h9ipfn?svg=true)](https://ci.appveyor.com/project/olsh/stack-exchange-redis-analyzer)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=stack-exchange-redis-analyzer&metric=alert_status)](https://sonarcloud.io/dashboard?id=stack-exchange-redis-analyzer)
[![codecov](https://codecov.io/gh/olsh/stack-exchange-redis-analyzer/branch/master/graph/badge.svg)](https://codecov.io/gh/olsh/stack-exchange-redis-analyzer)
[![NuGet](https://img.shields.io/nuget/v/StackExchange.Redis.Analyzer.svg)](https://www.nuget.org/packages/StackExchange.Redis.Analyzer/)
[![Visual Studio Marketplace](https://img.shields.io/vscode-marketplace/v/olsh.sqlanalyzer.svg)](https://marketplace.visualstudio.com/items?itemName=olsh.StackExchangeRedisAnalyzer)

Roslyn-based analyzer for StackExchange.Redis library


## SER001

Async methods on ITransaction type shouldn't be blocked

Noncompliant Code Example:  
```csharp
var transaction = db.CreateTransaction();
await transaction.StringSetAsync("key", "value").ConfigureAwait(false);
		
await transaction.ExecuteAsync().ConfigureAwait(false);
```

Compliant Solution:  
```csharp
var transaction = db.CreateTransaction();
transaction.StringSetAsync("key", "value").ConfigureAwait(false);
		
await transaction.ExecuteAsync().ConfigureAwait(false);
```
