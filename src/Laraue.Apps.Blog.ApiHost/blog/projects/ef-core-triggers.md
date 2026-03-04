---
title: EF Core triggers
type: project
githubLink: https://github.com/win7user10/Laraue.EfCoreTriggers
tags: [Triggers,SQL,.NET,C#]
description: The library to write triggers using fluent syntax in C# and EF Core.
createdAt: 2025-11-01
updatedAt: 2025-12-08
---
## Key Features
|              |                                                                                 |
|--------------|---------------------------------------------------------------------------------|
| Language     | C#                                                                              |
| Framework    | .NET Standard 2.1 / .NET 6 / .NET 8 / .NET 9 / .NET 10                          |
| Project type | Library                                                                         |
| Status       | Completed                                                                       |
| License      | MIT                                                                             |
| Nuget        | ![latest version](https://img.shields.io/nuget/v/Laraue.EfCoreTriggers.Common)  |
| Downloads    | ![latest version](https://img.shields.io/nuget/dt/Laraue.EfCoreTriggers.Common) |
| Github       | [Laraue.EfCoreTriggers](https://github.com/win7user10/Laraue.EfCoreTriggers)    |

## What Are Database Triggers?
Database triggers are special stored procedures that automatically execute in response to specific database events,
like inserts, updates, or deletes. They're used to enforce business rules, maintain data integrity, and automate
tasks within a database system.

## When Trigger Usage Is Justified
In modern application development, developers often try to avoid database triggers, preferring to implement logic within
the application layer. This approach has advantages:
- **Improved Application Scalability:** The database is often a bottleneck in applications. Additional logic executed 
within the database consumes resources. Database scaling is typically vertical (increasing the power of a single server),
whereas application hosts and the infrastructure between them often scale more effectively.
- **Centralized Application Logic:** Keeping logic in one place makes it easier to understand, maintain, and debug.

However, triggers still have benefits that justify their use in certain situations:
- **Simplified Legacy System Modifications:** Adding replication to legacy systems can require extensive code modifications.
When a codebase is poorly structured or lacks comprehensive tests, such changes can easily introduce errors
and unintended consequences.
- **Data Integrity Enforcement:** In some environments, users may have direct access to the database. This can compromise
data integrity if changes aren't tracked within the application layer. Triggers can provide an extra layer of protection.
- **Cost-Effective Infrastructure:** Smaller organizations often seek to minimize infrastructure costs. For simpler use
cases, it can be more economical to leverage an existing database and build an event system within it, rather than managing
a dedicated message bus system like Kafka or RabbitMQ.

## The Challenge with Triggers in EF Core
When working with triggers and EF Core, the standard approach is to manually execute SQL to create the trigger:
```csharp
migrationBuilder.Sql("CREATE TRIGGER ...")
```
While this works, managing triggers becomes difficult:
- **Lack of Visibility:** It's challenging to determine if a trigger exists without searching migration files or
inspecting the database directly.
- **Maintenance Difficulties:** When the entity model associated with a trigger changes, the trigger must also be updated.
Failure to do so will result in runtime exceptions.

## Library vision
This library aims to address these challenges. By allowing triggers to be defined using fluent syntax, similar to how
indexes and foreign keys are defined, the library will:
- Provide compile-time validation to detect errors when models change.
- Automatically recreate triggers when necessary.
- Clearly associate triggers with their corresponding entities within the model definition.

## How to use
Define the required trigger within the model builder:
```csharp
modelBuilder.Entity<Transaction>()
    .AfterUpdate(trigger => trigger
        .Action(action => action
            .Condition(tableRefs => tableRefs.Old.IsVeryfied && tableRefs.New.IsVeryfied) // Executes only if condition met 
            .Update<UserBalance>(
                (tableRefs, userBalances) => userBalances.UserId == tableRefs.Old.UserId, // Will be updated entities with matched condition
                (tableRefs, oldBalance) => new UserBalance { Balance = oldBalance.Balance + tableRefs.New.Value - tableRefs.Old.Value }))); // New values for matched entities.

```
Add the trigger functionality and the appropriate database provider to the `DbContextOptionsBuilder`
```csharp
var options = new DbContextOptionsBuilder<TestDbContext>()
    .UseNpgsql("User ID=test;Password=test;Host=localhost;Port=5432;Database=test;")
    .UsePostgreSqlTriggers() // Select the package for your database, Postgres provider in the example
    .Options;

var dbContext = new TestDbContext(options);
```
Then, generate a migration. The trigger SQL will be added to the `Up` and `Down` sections. 

## Challenges
The specific problems and solutions will be described in separate articles:
- Expression tree parsing
- Support for different database providers

## Timeline
- **Nov 2020** Initial version with Postgres support.
- **Dec 2020** Added support for MS SQL, SQLite, MySQL.
- **~ Dec 2022** Stable version with extensive built-in math and string function support.
- **Dec 2025** Extracted triggers core functionality to the separated packages. It allows generating triggers without
EF Core and simplifies versions updating.

## Further improvements
- Refactor the methods translation plugin system to align with the approach used in Linq2DB: mark methods with attributes
to define how they should be translated into SQL.