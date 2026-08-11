---
title: EF Core Triggers in C# — How to Write Database Triggers With Fluent Syntax and Expression Trees
type: project
githubLink: https://github.com/win7user10/Laraue.EfCoreTriggers
tags: [ef-core, csharp, database-triggers, expression-trees, fluent-api, sql]
description: A deep dive into Laraue.EfCoreTriggers — how to define database triggers in C# using fluent syntax, how the library translates expression trees to SQL, and how to extend it for new database providers.
createdAt: 2025-11-01
updatedAt: 2026-06-12
---
**Database triggers in EF Core** have always been second-class citizens. The standard approach is a raw SQL string in a migration — invisible to your model, out of sync the moment a column renames, and impossible to validate at compile time. [Laraue.EfCoreTriggers](https://github.com/win7user10/Laraue.EfCoreTriggers) solves this by letting you define triggers using the same **fluent C# syntax** you already use for indexes and foreign keys, with **expression tree–based SQL generation** that ties trigger logic to your entity model.

|              |                                                                                |
|--------------|--------------------------------------------------------------------------------|
| Language     | C#                                                                             |
| Framework    | .NET Standard 2.1 / .NET 6 / .NET 8 / .NET 9 / .NET 10                         |
| Project type | Library                                                                        |
| Status       | Active                                                                         |
| License      | MIT                                                                            |
| NuGet        | ![latest version](https://img.shields.io/nuget/v/Laraue.EfCoreTriggers.Common) |
| Downloads    | ![downloads](https://img.shields.io/nuget/dt/Laraue.EfCoreTriggers.Common)     |
| GitHub       | [Laraue.EfCoreTriggers](https://github.com/win7user10/Laraue.EfCoreTriggers)   |

---

## When Triggers Are Worth Using

Most modern .NET applications rightly keep business logic in the application layer. Triggers add hidden behaviour at the database level, making systems harder to reason about.

That said, triggers are genuinely useful in three scenarios:

**Legacy systems without full test coverage.** Adding event-driven logic via application code means touching code paths you may not fully understand. A trigger on the database side can add behaviour (audit rows, balance updates, soft-delete cascades) without touching the application at all.

**Direct database access.** When users or other systems write to your database directly — bypassing your application — you can't rely on application-layer logic to maintain invariants. Triggers are the last line of defence.

**Cost-constrained infrastructure.** Running a message bus (Kafka, RabbitMQ) for internal events is expensive. For simple use cases, a trigger-driven event log in the same database is cheaper and easier to operate — especially for small teams.

---

## The Problem With Raw SQL Triggers in EF Core

The standard way to add a trigger in an EF Core migration:

```csharp
migrationBuilder.Sql("CREATE TRIGGER tr_after_update_transaction ...");
```

This works, but it breaks in two ways over time:

1. **Rename a column**, and the trigger silently stops working — or throws at runtime. EF Core has no idea the trigger references that column.
2. **Find triggers** requires digging through migration files or querying the database directly. There's no model-level visibility.

The library addresses both problems. Triggers are defined on the entity model, so they're co-located with the entities they reference. The expression-based syntax means a column rename surfaces as a C# compile error, not a runtime surprise.

---

## Quick Start

Install the package for your database provider:

```
dotnet add package Laraue.EfCoreTriggers.PostgreSql
dotnet add package Laraue.EfCoreTriggers.SqlServer
dotnet add package Laraue.EfCoreTriggers.MySql
dotnet add package Laraue.EfCoreTriggers.Sqlite
dotnet add package Laraue.EfCoreTriggers.Oracle
```

Define the trigger in your `OnModelCreating`:

```csharp
modelBuilder.Entity<Transaction>()
    .AfterUpdate(trigger => trigger
        .Action(action => action
            .Condition(tableRefs => tableRefs.Old.IsVerified && tableRefs.New.IsVerified)
            .Update<UserBalance>(
                (tableRefs, userBalances) => userBalances.UserId == tableRefs.Old.UserId,
                (tableRefs, oldBalance) => new UserBalance
                {
                    Balance = oldBalance.Balance + tableRefs.New.Value - tableRefs.Old.Value
                })));
```

Register the provider in your `DbContextOptionsBuilder`:

```csharp
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseNpgsql(connectionString)
    .UsePostgreSqlTriggers()
    .Options;
```

Run `dotnet ef migrations add AddTransactionTrigger`. The migration's `Up` method will contain the full `CREATE TRIGGER` SQL, and `Down` will contain the corresponding `DROP TRIGGER`. From this point on, if `Transaction.Value` or `UserBalance.Balance` are renamed, the trigger definition fails to compile.

---

## How It Works: From C# Expressions to SQL

This is where the library earns its complexity. The fluent `AfterUpdate(...).Action(...)` call doesn't execute anything — it builds a trigger description as an object graph. That graph is then walked by a visitor pipeline that emits SQL.

### Step 1: The Trigger Object

When you call `AfterUpdate(...)`, the extension method on `EntityTypeBuilderExtensions` creates a `Trigger<TTriggerEntity, TTriggerEntityRefs>` instance ([source](https://github.com/win7user10/Laraue.EfCoreTriggers/blob/master/src/Laraue.Linq2Triggers.Core/TriggerBuilders/Trigger.cs)) and stores it as an EF Core model annotation on the entity type.

```csharp
public sealed class Trigger<TTriggerEntity, TTriggerEntityRefs> : ITrigger
    where TTriggerEntity : class
    where TTriggerEntityRefs : ITableRef<TTriggerEntity>
{
    public TriggerEvent TriggerEvent { get; }    // Insert / Update / Delete
    public TriggerTime TriggerTime { get; }      // Before / After / InsteadOf
    public IList<TriggerActionsGroup> Actions { get; } = new List<TriggerActionsGroup>();
    public string Name { get; private set; }

    public Trigger<TTriggerEntity, TTriggerEntityRefs> Action(
        Action<TriggerActionsGroup<TTriggerEntity, TTriggerEntityRefs>> triggerAction)
    {
        var action = new TriggerActionsGroup<TTriggerEntity, TTriggerEntityRefs>();
        triggerAction(action);
        Actions.Add(action);
        return this;
    }
}
```

Each `.Action(...)` call adds a `TriggerActionsGroup` to the list. An action group holds the optional condition expression and a list of actions (Update, Delete, Insert, Upsert). The trigger itself is a plain C# object — no SQL, no EF Core internals at this stage.

### Step 2: Reading the EF Core Schema

The library needs to know **column names and table names** as EF Core has them — not as C# property names. A property called `IsVerified` might map to the column `is_verified` or `IsVerified` depending on naming conventions.

`EfCoreDbSchemaRetriever` ([source](https://github.com/win7user10/Laraue.EfCoreTriggers/blob/master/src/Laraue.EfCoreTriggers.Common/SqlGeneration/EfCoreDbSchemaRetriever.cs)) wraps EF Core's `IModel` to answer questions the SQL generator asks during visitor traversal: *what is the table name for this CLR type? what is the column name for this property?*

This is the **adapter between the library's core and EF Core** — it's also why the core (`Laraue.Linq2Triggers.Core`) can be used without EF Core at all, by implementing `IDbSchemaRetriever` against any schema source.

### Step 3: Migration Integration via TriggerModelDiffer

EF Core generates migrations by comparing two `IModel` snapshots and producing `MigrationOperation` objects. The library hooks into this via `TriggerModelDiffer` ([source](https://github.com/win7user10/Laraue.EfCoreTriggers/blob/master/src/Laraue.EfCoreTriggers.Common/Migrations/TriggerModelDiffer.cs)), which wraps EF Core's built-in `IMigrationsModelDiffer`.

`TriggerModelDiffer` inspects the annotations on each entity type in both snapshots. When a trigger annotation is added, changed, or removed, it injects the corresponding `SqlOperation` (containing the generated SQL) into the migration operations list. EF Core's migration scaffolding then writes this to the migration file.

This is **how trigger SQL ends up in migrations without you writing it** — the model differ detects the change and produces the SQL automatically.

---

## Expression Tree Visitor Architecture

The most technically interesting part of the library is the **visitor pipeline** that translates C# expression trees into SQL strings.

### The Provider Service Registration Pattern

Each database provider registers its services through an `IServiceCollection` extension. The MySQL provider's registration ([source](https://github.com/win7user10/Laraue.EfCoreTriggers/blob/master/src/Laraue.Linq2Triggers.Providers.MySql/Extensions/ServiceCollectionExtensions.cs)) shows the full shape:

```csharp
public static void AddBaseMySqlServices(this IServiceCollection serviceCollection)
{
    serviceCollection
        .AddDefaultServices()
        .AddScoped<SqlTypeMappings, MySqlTypeMappings>()
        .AddScoped<ITriggerVisitor, MySqlTriggerVisitor>()
        .AddTriggerActionVisitor<TriggerUpsertAction, MySqlTriggerUpsertActionVisitor>()
        .AddScoped<ISqlGenerator, MySqlSqlGenerator>()
        // Method call converters — each maps a C# method to its SQL equivalent
        .AddMethodCallConverter<ConcatStringViaConcatFuncVisitor>()
        .AddMethodCallConverter<StringToUpperViaUpperFuncVisitor>()
        .AddMethodCallConverter<StringContainsViaInstrFuncVisitor>()
        .AddMethodCallConverter<MathAbsVisitor>()
        // ... more converters
        // Member access converters — each maps a C# property/field to its SQL equivalent
        .AddMemberAccessConverter<Converters.MemberAccess.DateTimeOffset.NowVisitor>()
        .AddMemberAccessConverter<Converters.MemberAccess.DateTime.UtcNowVisitor>();
}
```

Each provider registers:
- Its own `ISqlGenerator` — handles table/column quoting, `OLD`/`NEW` row reference syntax (which differs between MySQL, PostgreSQL, SQLite, and SQL Server)
- Its own `ITriggerVisitor` — generates the outer `CREATE TRIGGER` block with provider-specific syntax
- A set of **method call converters** — translate C# methods like `string.Contains()` or `Math.Abs()` into the correct SQL function for that provider
- A set of **member access converters** — translate C# properties like `DateTime.UtcNow` into the equivalent SQL expression

This service collection pattern is what makes **adding a new database provider straightforward**: implement the required interfaces, register your services, done. The visitor pipeline and expression tree traversal are inherited from core.

### Expression Visitors: Translating C# to SQL

When the SQL generator encounters an expression like:

```csharp
tableRefs.Old.IsVerified && tableRefs.New.IsVerified
```

it needs to produce something like:

```sql
OLD.is_verified = TRUE AND NEW.is_verified = TRUE
```

The `MemberExpressionVisitor` ([source](https://github.com/win7user10/Laraue.EfCoreTriggers/blob/master/src/Laraue.Linq2Triggers.Core/Visitors/ExpressionVisitors/MemberExpressionVisitor.cs)) handles `MemberExpression` nodes — property accesses on objects. Its `Visit` method dispatches based on the shape of the expression:

```csharp
public override SqlBuilder Visit(MemberExpression expression, VisitedMembers visitedMembers)
{
    switch (memberExpression.Expression)
    {
        // Static member (e.g. DateTime.UtcNow)
        case null:
            return Visit(memberExpression); // delegates to IMemberAccessVisitor chain

        // Column access (e.g. tableRefs.Old.Balance)
        case MemberExpression nestedMemberExpression:
            return GetColumnSql(nestedMemberExpression, memberExpression.Member, visitedMembers);

        // Captured variable (closure)
        case ConstantExpression constantExpression when memberExpression.Member is FieldInfo fieldInfo:
            var value = fieldInfo.GetValue(constantExpression.Value);
            return _expressionVisitorFactory.Visit(Expression.Constant(value), visitedMembers);
    }

    // OLD/NEW row reference
    if (memberExpression.Member.TryGetNewTableRef(out _))
        return _generator.NewEntityPrefix;   // "NEW"

    return memberExpression.Member.TryGetOldTableRef(out _)
        ? _generator.OldEntityPrefix         // "OLD"
        : GetColumnSql(memberExpression.Expression.Type, memberExpression.Member, argumentType);
}
```

The visitor distinguishes between four cases: static member access (handled by pluggable `IMemberAccessVisitor` converters), column access (resolved via `IDbSchemaRetriever`), captured closure variables (evaluated at generation time), and `OLD`/`NEW` row references (mapped to the provider's actual syntax).

For static members like `DateTime.UtcNow`, the visitor iterates the registered `IMemberAccessVisitor` chain until one returns `IsApplicable = true`. The MySQL `DateTimeOffset.NowVisitor`, for example, maps `DateTimeOffset.Now` to `NOW()`. PostgreSQL may map the same property to `NOW() AT TIME ZONE 'UTC'`. The same expression tree produces different SQL per provider, without any provider-specific logic in the visitor core.

### Action Visitors: Generating the SQL Body

Each trigger action type has its own visitor. `TriggerDeleteActionVisitor` ([source](https://github.com/win7user10/Laraue.EfCoreTriggers/blob/master/src/Laraue.Linq2Triggers.Core/Visitors/TriggerVisitors/TriggerDeleteActionVisitor.cs)) is the simplest example:

```csharp
public sealed class TriggerDeleteActionVisitor : ITriggerActionVisitor<TriggerDeleteAction>
{
    public SqlBuilder Visit(TriggerDeleteAction triggerAction, VisitedMembers visitedMembers)
    {
        var tableType = triggerAction.Predicate.Parameters.Last().Type;
        var triggerCondition = new TriggerCondition(triggerAction.Predicate);
        var conditionStatement = _factory.Visit(triggerCondition, visitedMembers);

        return new SqlBuilder()
            .Append($"DELETE FROM {_sqlGenerator.GetTableSql(tableType)}")
            .AppendNewLine("WHERE ")
            .Append(conditionStatement)
            .Append(";");
    }
}
```

The `Predicate` is the C# lambda the user wrote in the trigger definition — e.g. `(tableRefs, balances) => tableRefs.Old.UserId == balances.UserId`. The visitor passes it to the expression visitor factory, which recursively visits each node (binary expression, member access, constant) and produces the `WHERE` clause SQL. The result is appended to a `SqlBuilder`.

### Provider-Specific Trigger Wrappers

The outer `CREATE TRIGGER` syntax varies significantly between databases. PostgreSQL requires a separate trigger function; MySQL uses `BEGIN...END` blocks; SQL Server uses `AS BEGIN...END`. Each provider implements `ITriggerVisitor` to wrap the generated action SQL in the correct shell. The PostgreSQL visitor ([source](https://github.com/win7user10/Laraue.EfCoreTriggers/blob/master/src/Laraue.Linq2Triggers.Providers.PostgreSql/PostgreSqlTriggerVisitor.cs)) generates both the function and the trigger declaration, while MySQL and SQLite produce inline bodies.

---

## Using the Library Without EF Core

Because `EfCoreDbSchemaRetriever` is just one implementation of `IDbSchemaRetriever`, the trigger generation core can be used independently of EF Core. If you implement `IDbSchemaRetriever` to return schema information from another source — Dapper's column maps, a custom attribute-based convention, or a hardcoded dictionary — you can generate trigger SQL without any EF Core dependency.

This makes the library useful as a **SQL trigger generator** in migration tools or schema management utilities that don't use EF Core.

---

## Extending the Library: Adding a New Database Provider

To add a new provider (the library added Oracle support in v10.4.0):

1. **Implement `ISqlGenerator`** — override table/column quoting, `OLD`/`NEW` row reference syntax, and type mappings
2. **Implement `ITriggerVisitor`** — wrap the action SQL in the correct `CREATE TRIGGER` syntax for your database
3. **Register services** — create a `ServiceCollectionExtensions` class following the MySQL pattern above; register your `ISqlGenerator`, `ITriggerVisitor`, and any provider-specific method/member converters
4. **Add method converters** for any built-in C# methods that translate differently in your database (e.g. `string.Contains` → `INSTR` in MySQL vs `CHARINDEX` in SQL Server vs `position()` in PostgreSQL)

The architecture is designed so that **adding a provider requires zero changes to the core library**. Each provider is a self-contained package.

---

## Supported Databases

| Provider | Package |
|---|---|
| PostgreSQL | `Laraue.EfCoreTriggers.PostgreSql` |
| SQL Server | `Laraue.EfCoreTriggers.SqlServer` |
| MySQL | `Laraue.EfCoreTriggers.MySql` |
| SQLite | `Laraue.EfCoreTriggers.Sqlite` |
| Oracle | `Laraue.EfCoreTriggers.Oracle` |

---

## Timeline

- **Nov 2020** Initial release with PostgreSQL support
- **Dec 2020** Added SQL Server, SQLite, and MySQL providers
- **Dec 2022** Stable release with full math and string function support
- **Dec 2025** Core trigger logic extracted to provider-agnostic packages (`Laraue.Linq2Triggers.Core`), enabling use without EF Core
- **Feb 2026** Oracle provider added (v10.4.0); shadow property support; integration tests on CI

---

## Further Improvements

The current method translation system requires explicit converter registration per provider. A planned improvement is to adopt the Linq2DB pattern: mark C# methods with attributes that declare their SQL translation, so the framework can discover converters automatically rather than requiring manual registration in each provider's service collection.

---

## Frequently Asked Questions

**How are triggers kept in sync when entity properties are renamed?**

Trigger definitions use C# lambda expressions referencing entity properties directly (e.g. `tableRefs.New.Balance`). If `Balance` is renamed in the entity class, the trigger definition fails to compile — the error surfaces at build time, not at runtime or in production.

**Can I use the library without EF Core?**

Yes. The trigger generation core (`Laraue.Linq2Triggers.Core`) is decoupled from EF Core. Implement `IDbSchemaRetriever` to provide table and column name resolution, and you can use the SQL generation pipeline in any .NET project.

**How do expression trees get translated to provider-specific SQL?**

Each database provider registers a set of method call converters and member access converters via `IServiceCollection`. When the expression visitor encounters a C# method or static property, it checks the registered converters in order and delegates to the first one that returns `IsApplicable = true`. The same expression tree produces different SQL output for each provider.

**What's the difference between a method call converter and a member access converter?**

Method call converters handle expressions like `string.ToUpper()` or `Math.Abs(x)` — C# methods with parentheses. Member access converters handle property/field accesses like `DateTime.UtcNow` or `DateTimeOffset.Now` — C# properties without invocation. Both implement separate interfaces and are registered via separate extension methods (`AddMethodCallConverter` / `AddMemberAccessConverter`).

**How does the trigger SQL get into the migration file?**

The library registers `TriggerModelDiffer` as a replacement for EF Core's built-in `IMigrationsModelDiffer`. When EF Core scaffolds a migration, `TriggerModelDiffer` compares trigger annotations on entity types between the old and new model snapshots. Added, changed, or removed triggers produce `SqlOperation` entries that EF Core writes into the migration's `Up` and `Down` methods.