---
title: Триггеры в EF Core на C# — fluent-синтаксис и деревья выражений для генерации SQL
type: project
githubLink: https://github.com/win7user10/Laraue.EfCoreTriggers
tags: [триггеры EF Core C#, триггеры Entity Framework, дерево выражений в SQL C#, расширение EF Core, fluent API EF Core, IModelDiffer EF Core, expression visitor C#]
description: Разбор библиотеки Laraue.EfCoreTriggers — как определять триггеры базы данных на C#, как деревья выражений транслируются в SQL, и как расширить библиотеку под новый провайдер БД.
createdAt: 2025-03-04
updatedAt: 2026-06-12
---
**Триггеры базы данных в EF Core** всегда были второсортным инструментом. Стандартный подход — сырая SQL-строка в миграции — невидима для модели, выходит из синхронизации при первом переименовании колонки и не поддаётся валидации на этапе компиляции. [Laraue.EfCoreTriggers](https://github.com/win7user10/Laraue.EfCoreTriggers) решает эту проблему: триггеры определяются через **fluent C# синтаксис**, аналогичный индексам и внешним ключам, а генерация SQL основана на **деревьях выражений**, привязанных к модели сущностей.

|             |                                                                                  |
|-------------|----------------------------------------------------------------------------------|
| Язык        | C#                                                                               |
| Фреймворк   | .NET Standard 2.1 / .NET 6 / .NET 8 / .NET 9 / .NET 10                           |
| Тип проекта | Библиотека                                                                       |
| Статус      | Активная разработка                                                              |
| Лицензия    | MIT                                                                              |
| NuGet       | ![последняя версия](https://img.shields.io/nuget/v/Laraue.EfCoreTriggers.Common) |
| Загрузки    | ![загрузки](https://img.shields.io/nuget/dt/Laraue.EfCoreTriggers.Common)        |
| GitHub      | [Laraue.EfCoreTriggers](https://github.com/win7user10/Laraue.EfCoreTriggers)     |

---

## Когда триггеры оправданы

Большинство современных .NET приложений обоснованно держат бизнес-логику в слое приложения. Триггеры добавляют скрытое поведение на уровне БД, усложняя понимание системы.

Тем не менее есть три сценария, где триггеры оправданы:

**Легаси-системы без полного покрытия тестами.** Добавление событийной логики через код приложения означает изменение путей, которые вы можете не до конца понимать. Триггер на стороне БД добавляет поведение — аудит строк, обновление балансов, каскадное мягкое удаление — без касания приложения.

**Прямой доступ к базе данных.** Когда пользователи или внешние системы пишут в БД напрямую, минуя приложение, нельзя полагаться на логику приложения для поддержания инвариантов. Триггеры — последняя линия защиты.

**Инфраструктура с ограниченным бюджетом.** Эксплуатация шины сообщений (Kafka, RabbitMQ) для внутренних событий дорога. Для простых случаев триггерный журнал событий в той же БД дешевле и проще в обслуживании, особенно для небольших команд.

---

## Проблема с сырыми SQL-триггерами в EF Core

Стандартный способ добавить триггер в миграции EF Core:

```csharp
migrationBuilder.Sql("CREATE TRIGGER tr_after_update_transaction ...");
```

Это работает, но со временем ломается двумя способами:

1. **Переименуйте колонку** — триггер молча перестаёт работать или падает в рантайме. EF Core не знает, что триггер ссылается на эту колонку.
2. **Найти триггеры** можно только перерывая файлы миграций или запрашивая БД напрямую. Никакой видимости на уровне модели.

Библиотека решает обе проблемы. Триггеры определяются на модели сущностей и располагаются рядом с теми сущностями, на которые ссылаются. Синтаксис на основе выражений означает, что переименование колонки проявляется как ошибка компиляции C#, а не сюрприз в рантайме.

---

## Быстрый старт

Установите пакет для вашего провайдера БД:

```
dotnet add package Laraue.EfCoreTriggers.PostgreSql
dotnet add package Laraue.EfCoreTriggers.SqlServer
dotnet add package Laraue.EfCoreTriggers.MySql
dotnet add package Laraue.EfCoreTriggers.Sqlite
dotnet add package Laraue.EfCoreTriggers.Oracle
```

Определите триггер в `OnModelCreating`:

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

Зарегистрируйте провайдер в `DbContextOptionsBuilder`:

```csharp
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseNpgsql(connectionString)
    .UsePostgreSqlTriggers()
    .Options;
```

Запустите `dotnet ef migrations add AddTransactionTrigger`. Метод `Up` миграции будет содержать полный SQL `CREATE TRIGGER`, а `Down` — соответствующий `DROP TRIGGER`. С этого момента переименование `Transaction.Value` или `UserBalance.Balance` приведёт к ошибке компиляции в определении триггера.

---

## Как это работает: от C# выражений к SQL

Здесь библиотека заслуживает своей сложности. Вызов `.AfterUpdate(...).Action(...)` ничего не выполняет — он строит описание триггера как граф объектов. Этот граф затем обходится конвейером посетителей (visitor pipeline), который генерирует SQL.

### Шаг 1: Объект триггера

При вызове `AfterUpdate(...)` метод расширения на `EntityTypeBuilderExtensions` создаёт экземпляр `Trigger<TTriggerEntity, TTriggerEntityRefs>` ([исходник](https://github.com/win7user10/Laraue.EfCoreTriggers/blob/master/src/Laraue.Linq2Triggers.Core/TriggerBuilders/Trigger.cs)) и сохраняет его как аннотацию модели EF Core на типе сущности.

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

Каждый вызов `.Action(...)` добавляет `TriggerActionsGroup` в список. Группа действий хранит опциональное выражение условия и список действий (Update, Delete, Insert, Upsert). Сам триггер — простой C# объект, без SQL и внутренностей EF Core.

### Шаг 2: Чтение схемы EF Core

Библиотеке нужно знать **имена колонок и таблиц** так, как их видит EF Core — не как имена C# свойств. Свойство `IsVerified` может маппиться на колонку `is_verified` или `IsVerified` в зависимости от соглашений об именовании.

`EfCoreDbSchemaRetriever` ([исходник](https://github.com/win7user10/Laraue.EfCoreTriggers/blob/master/src/Laraue.EfCoreTriggers.Common/SqlGeneration/EfCoreDbSchemaRetriever.cs)) оборачивает `IModel` EF Core и отвечает на вопросы, которые задаёт генератор SQL при обходе посетителями: *какое имя таблицы у этого CLR-типа? какое имя колонки у этого свойства?*

Это **адаптер между ядром библиотеки и EF Core** — и именно поэтому ядро (`Laraue.Linq2Triggers.Core`) можно использовать без EF Core вообще, реализовав `IDbSchemaRetriever` для любого источника схемы.

### Шаг 3: Интеграция с миграциями через TriggerModelDiffer

EF Core генерирует миграции, сравнивая два снапшота `IModel` и производя объекты `MigrationOperation`. Библиотека встраивается в этот процесс через `TriggerModelDiffer` ([исходник](https://github.com/win7user10/Laraue.EfCoreTriggers/blob/master/src/Laraue.EfCoreTriggers.Common/Migrations/TriggerModelDiffer.cs)), который оборачивает встроенный `IMigrationsModelDiffer` EF Core.

`TriggerModelDiffer` инспектирует аннотации на каждом типе сущности в обоих снапшотах. Когда аннотация триггера добавляется, изменяется или удаляется, он вставляет соответствующую `SqlOperation` (содержащую сгенерированный SQL) в список операций миграции. Scaffolding миграций EF Core затем записывает это в файл миграции.

Именно **так SQL триггеров попадает в миграции без ручного написания** — differ обнаруживает изменение и автоматически производит SQL.

---

## Архитектура посетителей деревьев выражений

Технически наиболее интересная часть библиотеки — **конвейер посетителей**, транслирующий деревья выражений C# в SQL-строки.

### Паттерн регистрации сервисов провайдера

Каждый провайдер БД регистрирует свои сервисы через расширение `IServiceCollection`. Регистрация MySQL провайдера ([исходник](https://github.com/win7user10/Laraue.EfCoreTriggers/blob/master/src/Laraue.Linq2Triggers.Providers.MySql/Extensions/ServiceCollectionExtensions.cs)) показывает полную структуру:

```csharp
public static void AddBaseMySqlServices(this IServiceCollection serviceCollection)
{
    serviceCollection
        .AddDefaultServices()
        .AddScoped<SqlTypeMappings, MySqlTypeMappings>()
        .AddScoped<ITriggerVisitor, MySqlTriggerVisitor>()
        .AddTriggerActionVisitor<TriggerUpsertAction, MySqlTriggerUpsertActionVisitor>()
        .AddScoped<ISqlGenerator, MySqlSqlGenerator>()
        // Конвертеры вызовов методов — каждый маппит C# метод на SQL-эквивалент
        .AddMethodCallConverter<ConcatStringViaConcatFuncVisitor>()
        .AddMethodCallConverter<StringToUpperViaUpperFuncVisitor>()
        .AddMethodCallConverter<StringContainsViaInstrFuncVisitor>()
        .AddMethodCallConverter<MathAbsVisitor>()
        // ... остальные конвертеры
        // Конвертеры доступа к членам — маппят C# свойства/поля на SQL-эквиваленты
        .AddMemberAccessConverter<Converters.MemberAccess.DateTimeOffset.NowVisitor>()
        .AddMemberAccessConverter<Converters.MemberAccess.DateTime.UtcNowVisitor>();
}
```

Каждый провайдер регистрирует:
- Собственный `ISqlGenerator` — отвечает за экранирование таблиц/колонок и синтаксис ссылок на `OLD`/`NEW` строки (различается между MySQL, PostgreSQL, SQLite и SQL Server)
- Собственный `ITriggerVisitor` — генерирует внешний блок `CREATE TRIGGER` с синтаксисом провайдера
- Набор **конвертеров вызовов методов** — транслируют C# методы вроде `string.Contains()` или `Math.Abs()` в корректную SQL-функцию для данного провайдера
- Набор **конвертеров доступа к членам** — транслируют C# свойства вроде `DateTime.UtcNow` в соответствующее SQL-выражение

Этот паттерн с коллекцией сервисов делает **добавление нового провайдера БД простым**: реализуйте нужные интерфейсы, зарегистрируйте сервисы — готово. Конвейер посетителей и обход деревьев выражений наследуются из ядра.

### Посетители выражений: трансляция C# в SQL

Когда генератор SQL встречает выражение вроде:

```csharp
tableRefs.Old.IsVerified && tableRefs.New.IsVerified
```

ему нужно произвести что-то вроде:

```sql
OLD.is_verified = TRUE AND NEW.is_verified = TRUE
```

`MemberExpressionVisitor` ([исходник](https://github.com/win7user10/Laraue.EfCoreTriggers/blob/master/src/Laraue.Linq2Triggers.Core/Visitors/ExpressionVisitors/MemberExpressionVisitor.cs)) обрабатывает узлы `MemberExpression` — обращения к свойствам объектов. Его метод `Visit` диспетчеризирует по форме выражения:

```csharp
public override SqlBuilder Visit(MemberExpression expression, VisitedMembers visitedMembers)
{
    switch (memberExpression.Expression)
    {
        // Статический член (например, DateTime.UtcNow)
        case null:
            return Visit(memberExpression); // делегирует в цепочку IMemberAccessVisitor

        // Доступ к колонке (например, tableRefs.Old.Balance)
        case MemberExpression nestedMemberExpression:
            return GetColumnSql(nestedMemberExpression, memberExpression.Member, visitedMembers);

        // Захваченная переменная (замыкание)
        case ConstantExpression constantExpression when memberExpression.Member is FieldInfo fieldInfo:
            var value = fieldInfo.GetValue(constantExpression.Value);
            return _expressionVisitorFactory.Visit(Expression.Constant(value), visitedMembers);
    }

    // Ссылка на строку OLD/NEW
    if (memberExpression.Member.TryGetNewTableRef(out _))
        return _generator.NewEntityPrefix;   // "NEW"

    return memberExpression.Member.TryGetOldTableRef(out _)
        ? _generator.OldEntityPrefix         // "OLD"
        : GetColumnSql(memberExpression.Expression.Type, memberExpression.Member, argumentType);
}
```

Посетитель различает четыре случая: доступ к статическому члену (обрабатывается подключаемыми конвертерами `IMemberAccessVisitor`), доступ к колонке (разрешается через `IDbSchemaRetriever`), захваченные переменные замыкания (вычисляются в момент генерации) и ссылки на строки `OLD`/`NEW` (маппятся на реальный синтаксис провайдера).

Для статических членов вроде `DateTime.UtcNow` посетитель итерирует зарегистрированную цепочку `IMemberAccessVisitor` до первого совпадения `IsApplicable = true`. MySQL-конвертер `DateTimeOffset.NowVisitor` маппит `DateTimeOffset.Now` на `NOW()`. PostgreSQL может маппить то же свойство на `NOW() AT TIME ZONE 'UTC'`. Одно и то же дерево выражений производит разный SQL для разных провайдеров — без провайдерно-специфичной логики в ядре посетителя.

### Посетители действий: генерация тела SQL

Каждый тип действия триггера имеет собственного посетителя. `TriggerDeleteActionVisitor` ([исходник](https://github.com/win7user10/Laraue.EfCoreTriggers/blob/master/src/Laraue.Linq2Triggers.Core/Visitors/TriggerVisitors/TriggerDeleteActionVisitor.cs)) — простейший пример:

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

`Predicate` — это C# лямбда, написанная пользователем в определении триггера, например `(tableRefs, balances) => tableRefs.Old.UserId == balances.UserId`. Посетитель передаёт её фабрике посетителей выражений, которая рекурсивно обходит каждый узел (бинарное выражение, доступ к членам, константа) и производит SQL для `WHERE`-клаузы. Результат добавляется в `SqlBuilder`.

### Провайдерно-специфичные обёртки триггеров

Внешний синтаксис `CREATE TRIGGER` существенно различается между БД. PostgreSQL требует отдельную trigger function; MySQL использует блоки `BEGIN...END`; SQL Server — `AS BEGIN...END`. Каждый провайдер реализует `ITriggerVisitor`, чтобы обернуть сгенерированный SQL действий в корректную оболочку. PostgreSQL-посетитель генерирует и функцию, и объявление триггера, тогда как MySQL и SQLite производят встроенные тела.

---

## Использование без EF Core

Поскольку `EfCoreDbSchemaRetriever` — лишь одна из реализаций `IDbSchemaRetriever`, ядро генерации триггеров можно использовать независимо от EF Core. Реализуйте `IDbSchemaRetriever`, возвращающий информацию о схеме из любого источника — маппинги колонок Dapper, атрибутный подход, хардкоженный словарь — и можно генерировать SQL триггеров без зависимости от EF Core.

Это делает библиотеку полезной как **генератор SQL триггеров** в инструментах миграций или утилитах управления схемой, не использующих EF Core.

---

## Расширение библиотеки: добавление нового провайдера БД

Для добавления нового провайдера (библиотека добавила поддержку Oracle в v10.4.0):

1. **Реализуйте `ISqlGenerator`** — переопределите экранирование таблиц/колонок, синтаксис ссылок на строки `OLD`/`NEW` и маппинги типов
2. **Реализуйте `ITriggerVisitor`** — оберните SQL действий в корректный синтаксис `CREATE TRIGGER` для вашей БД
3. **Зарегистрируйте сервисы** — создайте класс `ServiceCollectionExtensions` по образцу MySQL; зарегистрируйте `ISqlGenerator`, `ITriggerVisitor` и провайдерно-специфичные конвертеры методов/членов
4. **Добавьте конвертеры методов** для встроенных C# методов, транслирующихся иначе в вашей БД (например, `string.Contains` → `INSTR` в MySQL vs `CHARINDEX` в SQL Server vs `position()` в PostgreSQL)

Архитектура спроектирована так, что **добавление провайдера не требует изменений в ядре библиотеки**. Каждый провайдер — самодостаточный пакет.

---

## Поддерживаемые базы данных

| Провайдер | Пакет |
|---|---|
| PostgreSQL | `Laraue.EfCoreTriggers.PostgreSql` |
| SQL Server | `Laraue.EfCoreTriggers.SqlServer` |
| MySQL | `Laraue.EfCoreTriggers.MySql` |
| SQLite | `Laraue.EfCoreTriggers.Sqlite` |
| Oracle | `Laraue.EfCoreTriggers.Oracle` |

---

## История версий

- **Ноябрь 2020** Первый релиз с поддержкой PostgreSQL
- **Декабрь 2020** Добавлены провайдеры SQL Server, SQLite и MySQL
- **Декабрь 2022** Стабильный релиз с полной поддержкой математических и строковых функций
- **Декабрь 2025** Ядро логики триггеров вынесено в провайдер-агностичные пакеты (`Laraue.Linq2Triggers.Core`), что позволяет использовать библиотеку без EF Core
- **Февраль 2026** Добавлен провайдер Oracle (v10.4.0); поддержка shadow properties; интеграционные тесты на CI

---

## Планируемые улучшения

Текущая система трансляции методов требует явной регистрации конвертеров для каждого провайдера. Планируемое улучшение — принять подход Linq2DB: помечать C# методы атрибутами, объявляющими их SQL-трансляцию, чтобы фреймворк мог обнаруживать конвертеры автоматически, не требуя ручной регистрации в коллекции сервисов каждого провайдера.

---

## Часто задаваемые вопросы

**Как триггеры остаются в синхронизации при переименовании свойств сущностей?**

Определения триггеров используют C# лямбда-выражения, напрямую ссылающиеся на свойства сущностей (например, `tableRefs.New.Balance`). Если `Balance` переименовано в классе сущности, определение триггера не компилируется — ошибка появляется на этапе сборки, а не в рантайме или в продакшне.

**Можно ли использовать библиотеку без EF Core?**

Да. Ядро генерации триггеров (`Laraue.Linq2Triggers.Core`) отвязано от EF Core. Реализуйте `IDbSchemaRetriever` для предоставления разрешения имён таблиц и колонок — и можно использовать конвейер генерации SQL в любом .NET проекте.

**Как деревья выражений транслируются в провайдерно-специфичный SQL?**

Каждый провайдер БД регистрирует набор конвертеров вызовов методов и конвертеров доступа к членам через `IServiceCollection`. Когда посетитель выражений встречает C# метод или статическое свойство, он проверяет зарегистрированные конвертеры по порядку и делегирует первому, чей `IsApplicable` возвращает `true`. Одно и то же дерево выражений производит разный SQL для каждого провайдера.

**В чём разница между конвертером вызова метода и конвертером доступа к члену?**

Конвертеры вызовов методов обрабатывают выражения вроде `string.ToUpper()` или `Math.Abs(x)` — C# методы со скобками. Конвертеры доступа к членам обрабатывают обращения к свойствам/полям вроде `DateTime.UtcNow` или `DateTimeOffset.Now` — C# свойства без вызова. Оба реализуют разные интерфейсы и регистрируются через разные методы расширения (`AddMethodCallConverter` / `AddMemberAccessConverter`).

**Как SQL триггеров попадает в файл миграции?**

Библиотека регистрирует `TriggerModelDiffer` как замену встроенного `IMigrationsModelDiffer` EF Core. При скаффолдинге миграции `TriggerModelDiffer` сравнивает аннотации триггеров на типах сущностей между старым и новым снапшотами модели. Добавленные, изменённые или удалённые триггеры производят записи `SqlOperation`, которые EF Core записывает в методы `Up` и `Down` миграции.