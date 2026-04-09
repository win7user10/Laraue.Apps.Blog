---
title: Библиотека Telegram.NET
type: project
tags: [Telegram,.NET,C#]
description: Библиотека позволяет писать ASP NET подобные контроллеры telegram, а также middleware и аутентификацию для них.
createdAt: 2025-03-04
updatedAt: 2025-03-04
---
## Ключевые особенности
|              |                                                                             |
|--------------|-----------------------------------------------------------------------------|
| Language     | C#                                                                          |
| Framework    | NET10                                                                       |
| Project type | Библиотека                                                                  |
| Status       | Готова к использованию                                                      |
| License      | MIT                                                                         |
| Nuget        | ![latest version](https://img.shields.io/nuget/v/Laraue.Telegram.NET.Core)  |
| Downloads    | ![latest version](https://img.shields.io/nuget/dt/Laraue.Telegram.NET.Core) |
| Github       | [Laraue.Telegram.NET](https://github.com/win7user10/Laraue.Telegram.NET)    |

## Пара слов о телеграм ботах
Telegram предлагает универсальную платформу, позволяющую создавать ботов для автоматизации задач и
интерактивных инструментов. Это легкая точка входа для обучения разработке ботов, автоматизации
рабочих процессов, без необходимости проработки интерфейса, дизайна и т.д.

## Основные проблемы, которые решает библиотека
Telegram API разработан для легкого создания ботов. Но все примеры ботов часто далеки от хороших архитектурных паттернов.
Это приводит к созданию единых точек входа, наполненных цепочками `if-else`, которые трудно поддерживать.
```
if (request.Message?.Text == "/Start")
    Start();
else if (request.Message?.Text == "/Settings")
    OpenSettings()
else if (request.Callback?.Data.StartsWith("/ChangeSettings"))
    ChangeSettings();
// И так далее
```

Эта библиотека предназначена для помощи разработчикам в написании чистого, поддерживаемого кода Telegram-ботов,
избегая написания подобного спагетти-кода.

## Видение библиотеки
Нет необходимости изобретать велосипед, когда уже существуют устоявшиеся паттерны, такие как MVC. Библиотека позволяет
разработчикам писать контроллеры для Telegram-ботов, подобные ASP.NET, используя атрибуты, такие как:
- `[TelegramMessageRoute("/new")]` для обработки сообщений
- `[TelegramCallbackRoute("/answer")]` для обработки callback'ов

Таким образом, все существующие в приложении методы легко можно увидеть в контроллерах приложения.

## Как использовать библиотеку

### Контроллеры
Для начала нужно определить контроллер в коде:
```csharp
public class MenuController : TelegramController
{
    private readonly IMenuService _service;

    public SettingsController(IMenuService service)
        _service = service;
    
    [TelegramMessageRoute("/start")]
    public Task ShowMenuAsync(TelegramRequestContext requestContext)
        return _service.HandleStartAsync(requestContext.Update.Message!);
}
```

Services requested in the constructor are resolved from the Microsoft DI container. The attribute
`TelegramMessageRoute("/start")` means the user message `"/start"` will be processed by the method above. 
The parameter `TelegramRequestContext` will contain the object of the request and allows to directly get the `Message` object. 

Сервисы, запрашиваемые в конструкторе, достаются из Microsoft DI контейнера. Атрибут `TelegramMessageRoute("/start")`
определяет, что сообщение пользователя `"/start"` будет обработано указанным ниже методом.
Параметр `TelegramRequestContext` будет содержать объект запроса и позволяет напрямую работать с объектом `Message`.

### Регистрация библиотеки
Разработчик должен решить, как обрабатывать Telegram-запросы. Есть два способа

#### Webhooks
Установите URL вебхука в Telegram:
`https://api.telegram.org/bot(токен)/setWebhook?url=https://site/адрес-который-никто-не-знает`.
Затем укажите адрес, запросы с которого должны слушаться приложением:
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTelegramCore(new TelegramBotClientOptions("51182636:AAHiPDQ8kVcbs2WZWG4Z..."));
var app = builder.Build();
app.MapTelegramRequests("адрес-который-никто-не-знает");
app.Run();
```

#### Long Polling
Этот способ означает, что приложение будет само вызывать Telegram для получения новых обновлений для бота.
Полезно как для локальных сред, так и сред, где необходимо горизонтальное масштабирование:
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTelegramCore(new TelegramBotClientOptions("51182636:AAHiPDQ8kVcbs2WZWG4Z..."));
builder.Services.AddTelegramLongPoolingService();
var app = builder.Build();
app.Run();
```

### Аутентификация
Чтобы включить аутентификацию, определите объект `User<UserKey>` и зарегистрируйте сервисы:
```csharp
services.AddTelegramCore()
    .AddTelegramAuthentication<User, Guid, TelegramUserQueryService, RequestContext>();
```
- `TelegramUserQueryService` должен реализовать 
[ITelegramUserQueryService](https://github.com/win7user10/Laraue.Telegram.NET/blob/master/src/Laraue.Telegram.NET.Authentication/Services/ITelegramUserQueryService.cs),
для обработки логики поиска/сохранения пользователя.
- `RequestContext : TelegramRequestContext<Guid>` просто класс обертка, чтобы не запрашивать всегда `TelegramRequestContext<Guid>` из контейнера.

Теперь в контроллерах вы можете получить доступ к информации о пользователе:

### Авторизация
Для ограничения доступа к методу контроллера, можно воспользоваться атрибутом `RequiresUserRole`:
```csharp
public static class Roles
{
    public const string Admin = "Admin";
}
```
```csharp
public class AdminController : TelegramController
{
    [RequiresUserRole(Roles.Admin)]
    [TelegramMessageRoute("/stat")]
    public Task SendStatAsync(RequestContext request, CancellationToken ct)
    {
        // write a logic
    }
}
```
Чтобы это заработало, необходимо реализовать класс
[IUserRoleProvider](https://github.com/win7user10/Laraue.Telegram.NET/blob/master/src/Laraue.Telegram.NET.Authentication/Services/IUserRoleProvider.cs)
который должен определить, какой пользователь какую роль имеет:
```csharp
.AddScoped<IUserRoleProvider, UserRoleProvider>()
```
Можно использовать уже реализованный [StaticUserRoleProvider](https://github.com/win7user10/Laraue.Telegram.NET/blob/master/src/Laraue.Telegram.NET.Authentication/Services/StaticUserRoleProvider.cs),
например, для определения предустановленного списка ролей пользователей.

#### Middlewares
Расширьте обработку запросов или перехватывайте запросы до того, как они достигнут уровня приложения, аналогично ASP.NET:

Зарегистрируйте middleware:
```csharp
public class LogExceptionsMiddleware : ITelegramMiddleware
{
    private readonly ITelegramMiddleware _next;
    private readonly TelegramRequestContext _telegramRequestContext;

    public LogExceptionsMiddleware(
        ITelegramMiddleware next,
        TelegramRequestContext telegramRequestContext)
    {
        _next = next;
        _telegramRequestContext = telegramRequestContext;
    }
    
    public async Task<object?> InvokeAsync(CancellationToken ct = default)
    {
        try
            return await _next.InvokeAsync(ct);
        catch (BadTelegramRequestException ex)
            _logger.LogError(ex, "Error occured");

        return null;
    }
}
```

#### Локализация
Включите локализацию, реализовав
[BaseCultureInfoProvider](https://github.com/win7user10/Laraue.Telegram.NET/blob/master/src/Laraue.Telegram.NET.Localization/BaseCultureInfoProvider.cs):
```csharp
public class LocalizationProvider : BaseCultureInfoProvider
{
    private readonly RequestContext _context;
    private readonly IUserRepository _userRepository;

    public LocalizationProvider(
        RequestContext context,
        IOptions<TelegramRequestLocalizationOptions> options,
        ILogger<BaseCultureInfoProvider> logger,
        IUserRepository userRepository)
        : base(context, options, logger)
    {
        _context = context;
        _userRepository = userRepository;
    }

    protected override async Task<TelegramProviderCultureResult> DetermineProviderCultureResultAsync(
        CultureInfo userInterfaceCulture,
        CancellationToken cancellationToken = default)
    {
        var settings = await _userRepository
            .GetSettingsAsync(_context.UserId, cancellationToken);

        return new TelegramProviderCultureResult(
            new CultureInfo(settings.Code),
            new CultureInfo(settings.Code));
    }
}
```

Настройте локализацию:
```csharp
.AddTelegramRequestLocalization<LocalizationProvider>()
.Configure<TelegramRequestLocalizationOptions>(opt =>
{
    opt.AvailableLanguages = ["en", "fr"];
    opt.DefaultLanguage = ["en"];
})
```

Теперь используйте стандартные возможности локализации Microsoft с файлами `resx`:
```
Resources/Buttons.resx
Resources/Buttons.fr.resx
```
Получайте доступ к локализованным строкам, например `Resources.Buttons.Menu` — правильный язык выбирется
автоматически на основе определенного языка пользователя.

## Проблемы при создании библиотеки
Основные решенные проблемы будут описаны в отдельных статьях
- Как сделать архитектуру модульной, чтобы пользователи включали только то, что им нужно
- Как спроектировать расширяемую систему, позволяющую добавлять новые функции обработки запросов

## Хронология
- **Jan 2023** Базовая версия с core-функциональностью, построенная на вебхуках
- **Feb 2023** Добавлена авторизация
- **Jan 2024** Добавлена локализация
- **Aug 2025** Добавлен режим Long pooling
- **Feb 2026** Добавлен пакет для интеграционного тестирования кода, написанного с библиотекой 

## Использование в реальных проектах
Библиотека повсеместно используется в проекте [Learn Language](learn-language) и отвечает там за все взаимодействие с Telegram.
Также, проект [SPB Real Estate](real-estate) использует данный проект для работы сервиса, взаимодействующего с пользователем через тг.