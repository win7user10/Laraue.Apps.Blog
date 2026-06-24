---
title: Аутентификация Telegram Mini App в .NET от и до — валидация initData, выпуск JWT и фронтенд на Nuxt
description: Часть 8 цикла о разработке Telegram-таск-трекера в одиночку. Полный флоу аутентификации Telegram Mini App в реальном приложении на .NET и Nuxt — валидация подписи initData на сервере через HMAC-SHA256, выпуск и использование JWT-bearer, чтение пользователя из HttpContext и почему важен CORS.
type: article
createdAt: 2026-06-24 08:00
updatedAt: 2026-06-24 08:00
projects: [boards]
tags: [dotnet, aspnet-core, nuxt, telegram-mini-app, authentication, initdata, jwt, cors, devlog]
previousLink: deploy-nuxt-telegram-mini-app-https-nginx
---

> **Architecture First: как в одиночку с ИИ сделать альтернативу Jira** — Часть 8.
> [Предыдущая статья](deploy-nuxt-telegram-mini-app-https-nginx) добилась того, что Mini App открывается внутри Telegram и показывает сырой объект пользователя. Но этим данным он доверял вслепую. Эта статья делает всё по-настоящему: бэкенд, с которым приложение разговаривает, и аутентификация, которая действительно доказывает, кто перед нами.

В конце прошлой статьи Mini App открывался, читал Telegram init data и показывал объект пользователя — но он ни разу не проверял, настоящие ли эти данные. Кто угодно мог подсунуть приложению поддельную строку init data, и оно бы ей поверило. Как доказательство «труба работает» это годилось; как аутентификация — нет. Эта статья закрывает пробел, а для этого ей нужно то, чего фронтенду не хватало в принципе: бэкенд.

## Бэкенд: хост web API

Этот бэкенд — новый хост. Ещё в [статье про архитектуру бэкенда](clean-dotnet-telegram-bot-architecture) хосты назывались по функции — `TelegramHost` для бота и `WebApiHost`, который упоминался, но ещё не существовал. Теперь он существует. [`WebApiHost`](https://github.com/win7user10/Laraue.Apps.Boards/tree/main/src/Laraue.Apps.Boards.WebApiHost) — это собственный хост, собственный запускаемый проект, который деплоится отдельным контейнером, — ровно то разделение по хостам, за которое мы агитировали раньше, и теперь второй хост наконец занимает свою роль.

Всё, что лежит ниже уровня хоста, он делит с ботом. Те же модели `DataAccess`, те же core-сервисы `Services` — создание issue из web API запускает ту же самую базовую логику, которой уже пользуется бот. Новое — это слой сервисов, специфичных для хоста, под веб-поверхность, и сам хост.

`Program.cs` хоста короткий и читается как вполне обычный web API на ASP.NET Core — с двумя надстройками, которые важны для этой статьи: аутентификацией и CORS.

```csharp
using Laraue.Apps.Boards.DataAccess;
using Laraue.Apps.Boards.Services;
using Laraue.Apps.Boards.WebApiHost;
using Laraue.Core.DataAccess.Linq2DB.Extensions;
using Laraue.Core.Exceptions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<TelegramOptions>();
builder.Services.Configure<TelegramOptions>(
    builder.Configuration.GetSection("Telegram"));

const string dbConnectionStringName = "Postgre";

builder.Services.AddAuthorization();
builder
    .AddAuthentication()
    .AddApplicationServices()
    .AddDatabaseServices(dbConnectionStringName);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Services.UseLinq2Db();
app.UseMiddleware<ExceptionHandleMiddleware>();

using (var scope = app.Services.CreateScope())
{
    await using var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    await db.Database.MigrateAsync();
}

var origins = builder
    .Configuration
    .GetSection("Cors:Hosts")
    .Get<string[]>();

if (origins is not null)
{
    app.UseCors(corsPolicyBuilder =>
        corsPolicyBuilder.WithOrigins(origins)
            .AllowCredentials()
            .AllowAnyMethod()
            .AllowAnyHeader());
}

app.MapHealthChecks("/_health");

app.Run();
```

Бóльшая часть этого общая с ботом и разбиралась раньше в цикле. `TelegramOptions` несут токен бота (web API он тоже нужен — чтобы валидировать init data, об этом ниже). `AddDatabaseServices` регистрирует тот же слой данных, что и у бота; блок миграции на старте и `MapHealthChecks("/_health")` — те же паттерны, что и в хосте бота, а `app.Services.UseLinq2Db()` — та же связка EF Core и linq2db, описанная в [статье про архитектуру бэкенда](clean-dotnet-telegram-bot-architecture).

По-настоящему новое здесь — пара строк, обрамляющих всё остальное: `AddAuthentication()` / `UseAuthentication()` и блок CORS в конце. Эти две вещи и есть тема всей статьи — доказать, кто пользователь, и контролировать, каким origin разрешено обращаться к API. Настройка CORS, которая берёт список разрешённых origin из секции конфигурации `Cors:Hosts`, подробно разбирается в финальной части, включая то, почему она важна в локальной разработке, но не в проде.

### Делим core, надстраиваем хост сверху

`AddApplicationServices` заслуживает отдельного взгляда, потому что именно здесь конкретно проявляется разделение «core против специфики хоста» из той ранней статьи. Это *не* один общий метод — у каждого хоста свой [`AddApplicationServices`](https://github.com/win7user10/Laraue.Apps.Boards/blob/main/src/Laraue.Apps.Boards.WebApiHost/WebApplicationBuilderExtensions.cs), и версия web API делает две вещи: регистрирует сервисы, специфичные для этого хоста, и вызывает регистрацию общего core. Его первая строка — [`builder.AddCoreServices()`](https://github.com/win7user10/Laraue.Apps.Boards/blob/main/src/Laraue.Apps.Boards.Services/WebApplicationBuilderExtensions.cs), которая регистрирует core-сервисы `Services` — логику, не зависящую от поверхности: создаёт issue, двигает их и так далее, ту самую логику, которую вызывает бот. Всё после этой строки специфично для хоста: веб-сервисы, оборачивающие core, `TelegramAuthService`, который валидирует init data, `ITelegramBotClient`, middleware обработки исключений и `AddControllers()` для HTTP-поверхности.

То есть бот и web API регистрируют каждый *свои* специфичные для хоста сервисы и делят *одну и ту же* регистрацию core под капотом — ровно то расслоение, которое описывала статья про структуру, теперь видимое в одном методе. По мере того как дальше в цикле появляются новые фичи, их сервисы добавляются сюда, но форма остаётся прежней: core-сервисы общие, специфичные для хоста — надстроены сверху.

`ExceptionHandleMiddleware`, зарегистрированный здесь, — небольшое новое поведение под веб-поверхность: это кастомный middleware из нашей общей библиотеки [Laraue.Core](https://github.com/win7user10/Laraue.Core), который автоматически отображает собственные веб-исключения библиотеки на HTTP-коды. Кинь `BadRequestException` откуда угодно внутри запроса — и клиенту вернётся `400`, причём вызывающему коду не пришлось собирать ответ руками; `ForbiddenException` превращается в `403` и так далее. Это значит, что слой сервисов может выражать ошибки обычными исключениями, а middleware переведёт их в корректные HTTP-ответы — забота, которой у бота никогда не было.

### Два хоста, одна база: миграции остаются безопасными

Одна деталь, на которую стоит обратить внимание: этот хост *тоже* запускает `MigrateAsync()` на старте, как и бот. Теперь, когда два хоста потенциально поднимаются одновременно и оба смотрят в одну базу, очевидное опасение — гонка: два процесса пытаются применить одни и те же миграции разом. EF Core решает это за нас: применение миграций берёт блокировку на уровне базы, так что шаг миграции сериализуется даже между разными процессами. Если оба хоста стартуют вместе, один берёт блокировку и применяет накопившиеся миграции, а другой просто ждёт; как только первый закончил и отпустил блокировку, второй видит, что схема уже актуальна, и продолжает старт. Так что миграция-на-старте остаётся безопасной и с несколькими хостами — сама база гарантирует, что миграции идут по одной за раз.

## Флоу аутентификации от и до

Прежде чем нырять в код — вот вся последовательность, через которую проходит логин, чтобы у кусочков ниже было, на что опереться. Когда Mini App стартует:

1. Приложение проверяет, запущено ли оно внутри Telegram — есть ли у него init data? (плагин [`auth.init.ts`](https://github.com/win7user10/laraue-boards/blob/master/app/plugins/auth.init.ts) из прошлой статьи).
2. Если есть — отправляет эти init data на эндпоинт аутентификации web API.
3. Бэкенд валидирует подпись init data по токену бота и возвращает **bearer-токен**.
4. Фронтенд сохраняет bearer в local storage.
5. Затем фронтенд делает второй вызов — «дай мне текущего пользователя» — уже аутентифицированный этим bearer.
6. Бэкенд читает пользователя из базы и возвращает его данные.
7. Фронтенд кладёт пользователя в общий composable со стейтом приложения.

С этого момента каждый запрос приложения несёт сохранённый bearer, а любой компонент может прочитать объект пользователя из стейта. Дальше эта часть проходит по тому же пути — триггер на фронте, валидация на бэке, bearer и круг за пользователем — по порядку.

### Шаги 1–2: фронтенд отправляет init data

Триггер — стартовый плагин из прошлой статьи, [`auth.init.ts`](https://github.com/win7user10/laraue-boards/blob/master/app/plugins/auth.init.ts), плагин Nuxt, который запускается автоматически при загрузке приложения. (Nuxt запускает любой файл из директории `plugins/` на старте; вот так этот код и выполняется, хотя его никто явно не вызывает.) В первой версии плагин делал `setUser(WebApp.initData)` и доверял данным вслепую. Теперь он вместо этого отдаёт init data на бэкенд.

Запросы идут через небольшой стек composable'ов. Верхний слой — `userApi.ts`, который определяет сами эндпоинты как маленькие типизированные функции — например `loadUser`, который представляет собой просто `GET /user`, возвращающий `UserDto`:

```ts
export const useUserApi = () => {
    const client = useUserClient();

    const loadUser = () => {
        return client<UserDto>('/user', {
            method: 'GET'
        });
    }
    // ...
}
```

Свой `client` он получает из `userClient.ts`, слоя под ним, который собирает аутентифицированный HTTP-клиент, нацеленный на базовый адрес web API:

```ts
export const useUserClient = () => {
    const configuration = useRuntimeConfig();
    const { createClient } = useUserAuthApi();
    return createClient(configuration.public.messagesBaseAddress);
}
```

`useUserAuthApi` — это то, что собственно создаёт клиент и задаёт его поведение. Именно здесь подцепляется bearer, так что на это стоит взглянуть:

```ts
export const useUserAuthApi = () => {
    const { getUserToken } = useLocalStorageUtils();

    const createClient = (baseURL: string) => $fetch.create({
        baseURL: baseURL,
        headers: {
            Authorization: `Bearer ${getUserToken()}`
        },
        // ...
    })

    return { createClient }
}
```

Строка, которая здесь главная, — заголовок: `Authorization: Bearer ${getUserToken()}`. Каждый собранный здесь клиент читает сохранённый bearer из local storage и подставляет его, так что после логина ничему ниже по течению уже не надо думать про аутентификацию — `userApi` просто дёргает эндпоинты, а токен едет с ними сам собой.

То есть расслоение на фронте зеркалит бэкендовое: `userApi` знает, *что* вызывать (эндпоинты и их формы), а `userClient`/`useUserAuthApi` знают, *как* общаться с API (базовый адрес и заголовок с bearer).

Сам базовый адрес, `messagesBaseAddress`, берётся из рантайм-конфигурации Nuxt в [`nuxt.config.ts`](https://github.com/win7user10/laraue-boards/blob/master/nuxt.config.ts), где он читается из переменной окружения, чтобы отличаться от среды к среде без правок кода:

```ts
runtimeConfig: {
    public: {
        messagesBaseAddress: process.env.NUXT_PUBLIC_MESSAGES_BASE_ADDRESS,
        // ...
    }
}
```

В проде это значение — тот же origin, с которого раздаётся приложение; в локальной разработке — ngrok-URL бэкенда. Именно эта одна точка конфигурации и порождает всю историю с CORS в конце статьи — от неё зависит, живёт ли API на том же origin, что и приложение, или на другом.

### Шаг 3: бэкенд валидирует init data

Теперь настоящая работа на сервере. Когда Mini App запускается внутри Telegram, Telegram отдаёт `initData` — строку с идентичностью пользователя и прочими параметрами запуска. Важно, что Telegram **подписывает** эти данные хешем, выведенным из токена бота. Эта подпись и делает возможной доверенную аутентификацию: сервер, знающий токен бота, может проверить подпись и быть уверенным, что данные действительно пришли от Telegram и не были подделаны.

(Этот флоу с `initData` — тот, что специфичен для Mini Apps, приложений, работающих *внутри* Telegram. Telegram также поддерживает вход с обычного внешнего сайта — через виджет «Log In with Telegram», который веб-приложение получит позже в цикле; это отдельный механизм, разберём его, когда дойдём. Пока приложение живёт внутри Telegram, так что `initData` здесь — верный путь.)

Первая версия фронтенда пропускала это целиком — брала `initData` и доверяла им. Web API делает это как положено: фронтенд отправляет сырые `initData` на эндпоинт аутентификации, сервер валидирует подпись, и только если та сходится, считает запрос настоящим пользователем.

Этот эндпоинт — тонкий контроллер [`TelegramAuthController`](https://github.com/win7user10/Laraue.Apps.Boards/blob/main/src/Laraue.Apps.Boards.WebApiHost/Controllers/TelegramAuthController.cs), который просто принимает init data и отдаёт их сервису:

```csharp
[ApiController]
[Route("api/auth")]
public class TelegramAuthController(ITelegramAuthService authService) : ControllerBase
{
    [HttpPost("mini-app")]
    public Task<string> AuthenticateViaMiniApp(
        [FromBody] AuthenticateViaStringInitDataRequest request,
        CancellationToken cancellationToken)
    {
        return authService.Authenticate(request, cancellationToken);
    }
}
```

Примечательно, что у этого эндпоинта нет атрибута `[Authorize]` — и не может быть, потому что это тот самый вызов, который *устанавливает* аутентификацию; bearer'а ещё нет. Он принимает сырые init data в теле запроса и возвращает bearer-токен. Всё остальное в API требует bearer, но эта одна дверь должна быть открыта, чтобы рукопожатие вообще началось.

Контроллер передаёт дело [`TelegramAuthService`](https://github.com/win7user10/Laraue.Apps.Boards/blob/main/src/Laraue.Apps.Boards.WebApiHost/TelegramAuthService.cs), чей метод `Authenticate` — точка входа, и читается он как две половины всего флоу: провалидировать init data, затем превратить проверенного пользователя в токен.

```csharp
public Task<string> Authenticate(
    AuthenticateViaStringInitDataRequest request,
    CancellationToken cancellationToken)
{
    var userData = ValidateInitData(request.InitData);
    return CreateBearerToken(userData, cancellationToken);
}
```

Сначала идёт валидация. Эту работу делают два метода: `ValidateInitData`, который проверяет подпись, и `BuildHash`, который пересчитывает её так же, как это делает Telegram.

```csharp
private MiniAppUser ValidateInitData(string initData)
{
    var parsedData = HttpUtility.ParseQueryString(initData);
    var receivedHash = parsedData["hash"];
    parsedData.Remove("hash");

    if (string.IsNullOrEmpty(receivedHash))
        throw new ForbiddenException("Hash is missing");

    var generatedHash = BuildHash(parsedData);

    var result = generatedHash.Equals(receivedHash, StringComparison.OrdinalIgnoreCase);
    if (!result)
        throw new ForbiddenException("Hash mismatch");

    var user = parsedData["user"];
    return JsonSerializer.Deserialize<MiniAppUser>(user!, JsonBotAPI.Options)!;
}

public string BuildHash(NameValueCollection collection)
{
    var sortedKeys = collection.AllKeys.OrderBy(key => key, StringComparer.Ordinal).ToList();
    var dataCheckStrings = sortedKeys.Select(key => $"{key}={collection[key]}");
    var dataCheckString = string.Join("\n", dataCheckStrings);

    var secretKey = HMACSHA256.HashData(
        "WebAppData"u8.ToArray(),
        Encoding.UTF8.GetBytes(options.Value.Token));

    var generatedHashBytes = HMACSHA256.HashData(
        secretKey,
        Encoding.UTF8.GetBytes(dataCheckString));

    var generatedHash = Convert.ToHexString(generatedHashBytes).ToLower();
    return generatedHash;
}
```

Init data приходят как URL-строка запроса — `user=...&auth_date=...&hash=...` и так далее. `ValidateInitData` разбирает её, вынимает `hash`, который положил Telegram, и убирает его из коллекции, потому что хеш считается по *всему, кроме самого себя*. Если хеша нет вовсе, запрос отклоняется сразу.

`BuildHash` — сердце всего, и он в точности следует задокументированному алгоритму Telegram. Тут три шага:

1. **Собрать data-check-string.** Взять каждое оставшееся поле, отсортировать ключи в ординальном порядке, оформить каждое как `key=value` и склеить через перевод строки. Сортировка важна — Telegram считает свой хеш по полям в определённом порядке, так что сервер обязан воспроизвести этот порядок точь-в-точь, иначе хеши никогда не сойдутся.
2. **Вывести секретный ключ.** Это тот шаг, что привязывает подпись к *этому конкретному боту*. Секретный ключ — это `HMAC-SHA256` от строкового литерала `"WebAppData"`, ключом к которому выступает токен бота. Произвести этот ключ может только тот, у кого есть токен бота, — и это делает всю схему доверенной.
3. **Вычислить подпись.** Прогнать `HMAC-SHA256` по data-check-string с этим секретным ключом и захексить результат.

Если хеш, который вычислил сервер, совпадает с хешем, который прислал Telegram, данные подлинные — они пришли от Telegram, подписаны токеном этого бота, и ничего в них не было изменено по пути. Только тогда сервис десериализует поле `user` в `MiniAppUser` и считает запрос этим пользователем. Любое расхождение кидает исключение, и запрос отклоняется.

Это ровно та проверка, которой первая версия фронтенда не делала. Там `setUser(WebApp.initData)` доверял данным как есть; здесь сервер доказывает их, прежде чем довериться. И валидация обязана быть на сервере — она зависит от токена бота, который нельзя отдавать в браузер, так что это по своей природе бэкендовая работа.

Одного эта реализация *не* делает, хотя более строгая должна бы: не проверяет свежесть. В init data есть поле `auth_date` — метка времени, когда Telegram их сгенерировал, — и закалённая реализация отклоняет данные старше нескольких минут, чтобы перехваченную строку init data нельзя было переигрывать бесконечно. Проверка подписи доказывает, что данные *подлинные*; проверка `auth_date` доказывала бы, что они *свежие*. Для проекта на одного пользователя окно для replay-атаки — небольшой риск, и его опустили, но для чего угодно с реальными пользователями его стоит добавить — это пара строк сравнения `auth_date` с текущим временем, и оно закрывает реальный пробел, который одна лишь проверка подписи не закрывает.

### Шаг 4: бэкенд выпускает bearer, фронтенд его сохраняет

Валидировать `initData` на каждом запросе было бы расточительно — проверка подписи это реальная работа, а init data — параметр запуска, а не то, что нужно пересылать на каждый вызов API. Поэтому валидация происходит один раз, при логине, а в обмен сервер выпускает **bearer-токен**. Дальше приложение шлёт этот токен с каждым запросом, и сервер доверяет ему, не перепроверяя исходные init data.

#### Выпуск токена (и регистрация при первом запуске)

Как только `ValidateInitData` подтвердил, что пользователь настоящий, `Authenticate` вызывает `CreateBearerToken`, чтобы превратить этого пользователя в токен:

```csharp
private async Task<string> CreateBearerToken(
    MiniAppUser userData, CancellationToken cancellationToken)
{
    var data = await context.Users
        .Where(x => x.TelegramId == userData.Id)
        .Select(x => new { x.Id })
        .FirstOrDefaultAsyncEF(cancellationToken);

    if (data is not null)
        return authService.CreateUserToken(data.Id);

    var newUserId = await RegisterUser(userData, cancellationToken);
    return authService.CreateUserToken(newUserId);
}
```

`CreateBearerToken` заодно служит точкой регистрации. Он ищет пользователя по его Telegram ID; если такой есть — выпускает токен для него, а если нет — сначала регистрирует нового пользователя, а затем выпускает токен. То есть самый первый запуск Mini App новым пользователем молча создаёт его аккаунт — отдельного шага регистрации нет.

Сам токен создаётся общим `AuthService`, подписанный секретом `Auth__Key` из конфигурации контейнера. Это стандартный JWT:

```csharp
public string CreateUserToken(Guid userId)
{
    var claims = new List<Claim>
    {
        new("id", userId.ToString())
    };

    var jwt = new JwtSecurityToken(
        issuer: Issuer,
        audience: UserAudience,
        claims: claims,
        signingCredentials: new SigningCredentials(
            GetSymmetricSecurityKey(options.Value.Key),
            SecurityAlgorithms.HmacSha256));

    return new JwtSecurityTokenHandler().WriteToken(jwt);
}

public static SymmetricSecurityKey GetSymmetricSecurityKey(string key)
{
    return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
}
```

Это JWT с единственным claim — внутренним ID пользователя, — подписанный HMAC-SHA256 с секретом `Auth__Key`. Выбор JWT здесь, а не непрозрачного токена с серверным хранилищем сессий, — осознанное попадание в масштаб: токен самодостаточен, сервер валидирует его тем же симметричным ключом, которым подписал, и нет таблицы сессий, в которую надо лезть на каждом запросе.

Это и есть bearer, который фронтенд сохраняет и шлёт на последующих запросах. Дорогая проверка init data случается один раз, при логине; всё после — обычная JWT-bearer-аутентификация в стандартном заголовке `Authorization`.

#### Как bearer аутентифицирует каждый следующий запрос

Строки `AddAuthentication()` и `UseAuthentication()` в `Program.cs` — это то, что наделяет bearer смыслом. JWT выпускается под именованной *схемой* аутентификации, которая говорит, как валидировать токен такого рода — какой issuer и audience ожидать и каким ключом проверять подпись (тем же `Auth__Key`, которым подписывали). `AddAuthentication()` регистрирует схему; middleware `UseAuthentication()` запускается рано в конвейере запроса, и когда приходит запрос с `Authorization: Bearer <token>`, он проверяет подпись, сверяет issuer/audience/срок и — если всё сходится — собирает `ClaimsPrincipal` из claim'ов токена и присваивает его `HttpContext.User`. Отсутствующий, просроченный или подписанный не тем ключом токен отклоняется с `401` ещё до того, как выполнится код любого контроллера.

Выигрыш в том, что контроллер никогда не парсит токены сам — он просто читает пользователя из `HttpContext.User`. `UserController`, в который попадает вызов `loadUser` с фронтенда, — это всё целиком:

```csharp
[HttpGet]
public Task<UserDto> GetAsync(CancellationToken ct)
{
    return service.GetUser(HttpContext.User.GetId(), ct);
}
```

`HttpContext.User` — это `ClaimsPrincipal`, который middleware собрал из bearer, а `GetId()` — небольшое расширение, которое достаёт ID пользователя обратно из claim `id`, зашитого в токен при логине:

```csharp
public static Guid GetId(this ClaimsPrincipal claimsPrincipal)
{
    var id = claimsPrincipal.FindFirstValue("id");
    return Guid.Parse(id!);
}
```

То есть всё аутентифицированное чтение целиком: middleware провалидировал bearer и заполнил `HttpContext.User`, контроллер читает оттуда ID пользователя через `GetId()`, а сервис загружает и возвращает этого пользователя. Claim `id`, записанный в `CreateUserToken`, — тот же, что читается обратно здесь; это и есть тот круг, ради которого JWT и существует.

Bearer здесь — один подписанный JWT, намеренно без машинерии «короткоживущий access плюс refresh», которую завёл бы кто-то покрупнее, — компромисс, который стоит рассмотреть, когда токен уже на руках у фронтенда, чем следующий шаг и займётся.

### Шаги 5–7: загрузить пользователя и положить его в стейт приложения

Ещё в прошлой статье первая версия фронтенда просто делала `setUser(WebApp.initData)` — брала Telegram init data и трактовала их как пользователя напрямую, без сервера. Теперь bearer на руках, и приложение делает всё по-настоящему: сохраняет токен, спрашивает бэкенд, кто пользователь, и кладёт его в общий стейт.

Сохранение токена — тонкая обёртка над local storage браузера:

```ts
const userTokenKey = 'bearer'

const getUserToken = () => localStorage.getItem(userTokenKey)
const setUserToken = async (bearerToken: string) =>
    localStorage.setItem(userTokenKey, bearerToken)
```

Local storage здесь к месту: bearer должен переживать закрытие и повторное открытие Mini App, чтобы следующий запуск мог переиспользовать его, а не прогонять всё рукопожатие с Telegram заново.

Здесь самое место честно сказать про срезанный угол. В аутентификации продакшен-уровня вы бы не опирались на один долгоживущий bearer вот так — стандарт это короткоживущий access-токен в паре с более долгим refresh-токеном, чтобы утёкший токен был полезен лишь несколько минут, а сессии можно было отзывать. Это более правильная схема, и у реального продукта, работающего с данными многих пользователей, она должна быть. Она же требует реального времени, чтобы сделать её как надо: эндпоинт обновления, логика ротации, хранение и отзыв refresh-токенов, перехватчик на фронте, который прозрачно обновляет токен на `401`. Для проекта масштаба «один пользователь» за аутентификацией самого Telegram эта работа сейчас почти ничего не даёт, так что её намеренно опустили в пользу одного подписанного JWT. Это осознанный размен — тот угол, который нормально срезать на раннем этапе и к которому стоит вернуться по мере роста продукта и его рисков, — а не то, что нужно слепо копировать в приложение с реальными пользователями и реальными данными.

С сохранённым токеном приложение загружает пользователя — тот самый вызов `loadUser`, тот `GET /user`, на который `UserController` выше отвечает, читая `HttpContext.User.GetId()`. Результат приходит как `UserDto` (имя, язык, цвет, аватар, инициалы, настройки), и последний шаг — положить его туда, где остальное приложение его увидит.

Две функции в composable `auth.ts` связывают сохранение и загрузку вместе:

```ts
const initUserWithBearer = async (bearerToken: string) => {
    await setUserToken(bearerToken)
    const { loadUser } = useUserApi();
    const user = await loadUser();
    await initUserWithUserData(user);

    if (redirectPath.value)
        return navigateTo(redirectPath.value)
}

const tryAuthWithStoredBearer = async () => {
    const { getUserToken } = useLocalStorageUtils()
    const bearer = getUserToken()

    if (!bearer)
        return false

    const { loadUser } = useUserApi();
    try {
        const user = await loadUser();
        await initUserWithUserData(user);
        return true;
    }
    catch (error) {
        console.error(error);
        return false;
    }
}
```

`initUserWithBearer` — это шаг после логина (шаги 4–7 флоу): сохранить свежевыпущенный bearer, вызвать `loadUser`, чтобы забрать `UserDto`, и передать его в `initUserWithUserData`, который кладёт пользователя в общий composable `appState`. Как только пользователь там, любой компонент приложения может прочитать текущего пользователя из стейта — имя, аватар, настройки — не запрашивая его заново.

`tryAuthWithStoredBearer` — оптимизация для повторных заходов. На старте, прежде чем вообще делать рукопожатие с Telegram, приложение проверяет, нет ли у него уже сохранённого bearer из прошлой сессии. Если есть — сразу переходит к загрузке пользователя с ним, и если это удаётся, пользователь залогинен мгновенно. И только если сохранённого токена нет или он протух (вызов `loadUser` падает с `401`), приложение откатывается к полному пути «провалидировать init data и выпустить новый bearer». Мелочь, но за счёт неё повторное открытие Mini App обычно мгновенно, а не прогоняет обмен подписями каждый раз.

То есть стартовый плагин из прошлой статьи обретает свою настоящую форму, и она совпадает с флоу, изложенным в начале этой части: сначала попробовать сохранённый bearer; если не вышло — взять Telegram init data, отправить на бэкенд для валидации, получить свежий bearer, сохранить его, загрузить пользователя и положить пользователя в стейт приложения. Пустой экран, который когда-то печатал сырой, непроверенный объект init data, теперь показывает настоящего пользователя, аутентифицированного от и до.

## Добавляем в Docker Compose

Раз web API написан и флоу аутентификации работает, остаётся деплой: поднять этот второй хост на сервере рядом с ботом и направить к нему API-вызовы приложения. Web API становится третьим контейнером в стеке, рядом с ботом и PostgreSQL:

```yaml
structuredmessageswebapihost:
  build:
    context: .
    dockerfile: "StructuredMessagesWebapiHostDockerfile"
  expose:
    - "5007"
  ports:
    - "8087:5007"
  restart: always
  environment:
    ASPNETCORE_ENVIRONMENT: "Production"
    Kestrel__EndPoints__Http__Url: "http://+:5007"
    Telegram__Token: "TokenHere"
    ConnectionStrings__Postgre: "User ID=PostgresUser;Password=PostgresPass;Host=postgres;Port=5432;Database=laraue_messages_board;Command Timeout=0;"
    Auth__Key: "SecretKeyHere"
    Cors__Hosts__0: "https://web.telegram.org"
    Cors__Hosts__1: "https://telegram.org"
    Cors__Hosts__2: "https://t.me"
    Logging__LogLevel__Default: "Warning"
  depends_on:
    postgres:
      condition: service_healthy
  networks:
    - dockerapi-dev
  deploy:
    resources:
      limits:
        memory: 256M
  healthcheck:
    test: ["CMD-SHELL", "curl -fsS http://localhost:5007/_health || exit 1"]
    interval: 5s
    timeout: 3s
    retries: 10
    start_period: 10s
```

Dockerfile, из которого он собирается, по сути такой же, как у бота, только нацелен на вывод web API:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY structured-messages-webapi-host app
WORKDIR app
ENTRYPOINT ["dotnet", "Laraue.Apps.StructuredMessages.WebApiHost.dll"]
```

Это тот же паттерн, что заложен в [статье про деплой](deploying-dotnet-postgres-vps-docker-compose): runtime-образ .NET, установленный `curl`, чтобы healthcheck контейнера вообще мог запускаться (runtime-образы идут без него), скопированный внутрь готовый вывод сборки и точка входа. Единственные отличия от Dockerfile бота — папка, которую он копирует, и DLL, которую запускает. Имя `StructuredMessages` и в пути `COPY`, и в DLL — тот же артефакт «до переименования», что всплывает по всем файлам деплоя.

Помимо Dockerfile, сервис повторяет форму ботовского — лимит памяти, `curl`-healthcheck на `/_health` (тот же эндпоинт, та же причина, по которой `curl` ставится в образ) и `depends_on`, ждущий, пока Postgres станет healthy, прежде чем стартовать.

Три значения окружения специфичны для этого хоста. `Telegram__Token` — токен бота, который web API нужен, чтобы валидировать init data. `Auth__Key` — секрет для подписи JWT-bearer-токенов, которые он выпускает. А `Cors__Hosts__0/1/2` — разрешённые CORS-origin. К ним статья вернётся в конце — они не так просты, как выглядят, и история о том, когда CORS здесь реально важен, не очевидна.

Это ещё и тот самый третий контейнер, который предвосхищало обсуждение лимитов ресурсов в [статье про деплой](deploying-dotnet-postgres-vps-docker-compose). С ботом, Postgres и теперь web API на одноядерном VPS фиксированные доли CPU переподписали бы ядро и перестали бы защищать друг друга — именно поэтому от лимитов CPU отказались в пользу лимитов только по памяти и честного планировщика ядра. Web API получает потолок в 256 МБ памяти, как и бот, и никакого ограничения по CPU. Решение, принятое раньше, окупается теперь, когда третий контейнер стал реальностью.

## Маршрутизируем /api на web API в nginx

В прошлой статье конфиг nginx раздавал статический фронтенд и намеренно оставлял за бортом роуты `/api`, потому что направлять их было некуда. Теперь есть куда. В server-блок Mini App добавляются блоки `location`, которые проксируют API-вызовы на контейнер web API:

```nginx
location ^~ /api/notes-board {
    set $upstream http://structuredmessageswebapihost:5007;
    rewrite ^/api/notes-board/(.*)$ /api/$1 break;
    proxy_pass         $upstream;
    proxy_next_upstream error timeout http_502 http_503 http_504;
    proxy_next_upstream_tries 5;
    proxy_next_upstream_timeout 30s;
}
```

Фронтенд вызывает `/api/notes-board/...` на своём же origin (`msgboard.laraue.com`), а nginx переписывает это в `/api/...` и форвардит на контейнер web API во внутренней сети Docker. Приложение и его API делят origin с точки зрения браузера — приложение раздаётся с `msgboard.laraue.com`, и его API-вызовы идут на `msgboard.laraue.com/api/notes-board/...`, — что держит обращённую к браузеру схему простой. (Именно эта деталь про общий origin и делает CORS не-проблемой в проде, о чём — финальная часть.)

## CORS: почему он важен в локальной разработке, но не в проде

Сначала — что вообще такое CORS, потому что от этого зависит весь остаток части. CORS — Cross-Origin Resource Sharing — это механизм безопасности браузера. По умолчанию браузер отказывается давать странице с одного origin читать ответы с другого origin, где «origin» — это сочетание схемы, хоста и порта (`https://msgboard.laraue.com` — один origin; `https://abc123.ngrok-free.app` — другой). Так браузер защищает пользователей: без этого любой сайт, который вы посетили, мог бы втихую слать аутентифицированные запросы к API вашего банка, пользуясь куками, что браузер уже держит, и читать результаты. Дефолт «тот же origin» блокирует весь этот класс атак.

CORS — это контролируемое исключение из дефолта. Когда сервер *хочет* разрешить конкретным другим origin обращаться к нему, он заявляет об этом, возвращая заголовки — `Access-Control-Allow-Origin` и компанию, — называющие origin, которым он доверяет. Ровно это и делает блок `UseCors(...)` в `Program.cs`: читает список разрешённых origin из конфигурации и сообщает браузеру «запросы с этих origin приветствуются». Принуждает по-прежнему браузер; сервер лишь декларирует, кого готов принять. То есть CORS здесь настроен ради одного — чтобы фронтенд, когда он раздаётся с *другого* origin, нежели API, мог делать вызовы, которые браузер иначе заблокировал бы.

Важно это или нет — зависит от среды. В проде не важно; в локальной разработке важно. Разница в origin'ах.

В проде фронтенд и API делят один. Приложение раздаётся с `msgboard.laraue.com`, и его API-вызовы идут на `msgboard.laraue.com/api/notes-board/...` — тот же origin, так что с точки зрения браузера ничего кросс-доменного нет вовсе, и CORS вообще не вступает в игру. Ровно это и устроила маршрутизация nginx выше: приложение и его API живут за одним хостнеймом.

Локальная разработка ломает это единство. Как описано в [прошлой статье](deploy-nuxt-telegram-mini-app-https-nginx), тестировать Mini App внутри Telegram со своей машины — значит туннелировать через ngrok, а ngrok выдаёт фронтенду и бэкенду *два разных* публичных URL. Теперь приложение на одном origin, а его API — на другом, и это уже настоящий кросс-доменный запрос, который браузер контролирует через CORS. Если бэкенд явно не разрешит ngrok-origin фронтенда, каждый API-вызов блокируется. Внутри Mini App это проявляется не дружелюбной ошибкой, а **белым экраном**, потому что приложение грузится, шлёт первый API-запрос, ничего не получает в ответ и так и не рендерится.

Фикс — добавить текущий ngrok-адрес фронтенда в разрешённые origin бэкенда, в список `Cors__Hosts`, который web API читает на старте. Это тот самый шаг из настройки ngrok в прошлой статье («добавьте ngrok-URL фронтенда в CORS-allowlist бэкенда»), и вот *почему* он там: без него локальное тестирование Mini App — это белый экран.

К этому есть честная сноска. В закоммиченном конфиге среди разрешённых origin также перечислены `https://web.telegram.org`, `https://telegram.org` и `https://t.me`. Это, откровенно говоря, из разряда значений, которые копируешь из ответа на форуме, когда воюешь с CORS и пробуешь всё подряд, — и в этой схеме с общим origin в проде они на самом деле не несут никакой нагрузки. Origin, который реально важен, — ngrok'овый во время разработки; продакшен-приложению не нужен ни один из них, потому что оно вообще не делает кросс-доменный запрос. Это маленький, но настоящий пример того, как в конфигурации накапливаются строки, которые выглядят осмысленно, но по большей части там лишь потому, что когда-то были в сниппете, который «починил». Полезный урок под этим стоит запомнить: CORS зависит от *origin, с которого приходит запрос*, так что он вступает в игру только тогда, когда что-то разводит ваши origin'ы — здесь это локальная разработка через ngrok, — а не в продовой схеме с общим origin.

## К чему мы пришли

На экране результат выглядит почти так же, как в конце прошлой статьи: приложение открывается и показывает данные пользователя. Но за этим экраном теперь совсем другое. Раньше приложение брало Telegram init data и показывало их на доверии — объект пользователя мог быть каким угодно. Теперь те же данные сделали полный круг: провалидированы на сервере по токену бота, обменяны на JWT-bearer, и объект пользователя на экране — тот, что вернул бэкенд, а не тот, что предположил фронтенд. Картинка та же; фундамент под ней — настоящий, и настолько настоящий, что на нём уже можно строить реальные фичи, чем и займётся следующий шаг.

## Что дальше

Раз аутентификация настоящая и приложение разговаривает с бэкендом, следующая статья строит первую реальную фичу в веб-приложении: слой issue — превращение прототипа из начала цикла в настоящие Vue-компоненты, подкреплённые API. Это ещё и место, где всплывёт продуктовая ошибка, обнаруженная, когда приложение впервые попробовал друг.