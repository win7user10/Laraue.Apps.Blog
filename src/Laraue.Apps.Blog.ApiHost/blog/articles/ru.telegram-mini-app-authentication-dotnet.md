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
> В [Предыдущей статье](deploy-nuxt-telegram-mini-app-https-nginx) мы добились того, что Mini App открывается внутри Telegram и отображает JSON с объектом пользователя. Но этим данным пока нельзя доверять. В этой статье мы делаем настоящую аутентификацию — добавляем бэкенд для приложения и проверку на корректность данных на его стороне.

В конце прошлой статьи прототип Mini App начал открываться и показывать объект пользователя из init data, который Telegram инжектит в приложение. Однако эти данные пока нельзя было использовать. Кто угодно мог подсунуть приложению поддельную строку init data, и приложению необходимо убедиться в её достоверности. Чтобы сделать её валидацию, приложению необходимо для начала добавить отсутствующий ранее бэкенд.

## Бэкенд: Dotnet Web API Host

Бэкенд — это новый хост, который будет работать с API-запросами, приходящими со стороны фронтенда Laraue Boards. Его название — `WebApiHost` — коррелирует с выполняемой функцией. Хост уже упоминался в [статье про архитектуру бэкенда](clean-dotnet-telegram-bot-architecture), но не существовал на момент её написания. Теперь же [`WebApiHost`](https://github.com/win7user10/Laraue.Apps.Boards/tree/main/src/Laraue.Apps.Boards.WebApiHost) добавлен в репозиторий.

Новый бэкенд делит с `TelegramHost` общий код — здесь используются те же модели из `DataAccess`, те же core-сервисы из `Services`. База, соответственно, тоже используется одна на два сервиса — это не совсем микросервисный подход, но смысла переусложнять архитектуру на данном этапе — нет. Создание issue из web API содержит ту же базовую логику, что и создание из Telegram. Новым здесь является слой сервисов для web API и сам хост.

Класс [`Program.cs`](https://github.com/win7user10/Laraue.Apps.Boards/blob/main/src/Laraue.Apps.Boards.WebApiHost/Program.cs) хоста небольшой и по большей части повторяет шаблон web API на ASP.NET Core, с двумя добавлениями, важными для этой статьи: аутентификацией и CORS.

```csharp
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

Большая часть этого кода такая же, как и в `TelegramHost`, и объяснялась ранее. `TelegramOptions` содержат токен бота — web API он понадобится для валидации init data. `AddDatabaseServices` регистрирует тот же слой данных, что и у бота; блок миграции на старте и `MapHealthChecks("/_health")` — те же паттерны, что в хосте бота; `app.Services.UseLinq2Db()` — та же связка EF Core и linq2db из [статьи про архитектуру бэкенда](clean-dotnet-telegram-bot-architecture).

Новыми здесь являются пара строк: `AddAuthentication()` / `UseAuthentication()` и блок с `UseCors()`. Аутентификация понадобится для создания Bearer токена для приложения, после того как init data от Telegram будет проверена, а настройка CORS необходима для локальной разработки — подробное описание можно будет найти в конце статьи.

### Деление сервисов на Core и Host специфичные

`AddApplicationServices` не является общим методом для всех хостов — у web API [`AddApplicationServices`](https://github.com/win7user10/Laraue.Apps.Boards/blob/main/src/Laraue.Apps.Boards.WebApiHost/WebApplicationBuilderExtensions.cs) свой. Предназначение такого метода - зарегистрировать специфичные для хоста сервисы и вызвать регистрацию общих (core) сервисов. 

`ExceptionHandleMiddleware` — кастомный middleware из нашей общей сборки [Laraue.Core](https://github.com/win7user10/Laraue.Core), который автоматически маппит веб-исключения библиотеки на HTTP-коды. Если в коде выбрасывается необработанное исключение `BadRequestException` — клиенту возвращается ошибка `400`, `ForbiddenException` превращается в `403`, и так далее.

## Аутентификация пользователя по init data из Telegram Mini App

Прежде чем переходить к коду — определим последовательность шагов при логине из Mini App:

1. Приложение проверяет, запущено ли оно внутри Telegram. Для этого нужно удостовериться, что init data доступен (плагин [`auth.init.ts`](https://github.com/win7user10/laraue-boards/blob/master/app/plugins/auth.init.ts) из прошлой статьи).
2. Отправляем init data на эндпоинт аутентификации web API.
3. Бэкенд валидирует подпись init data по токену бота и возвращает авторизационный **bearer-токен**.
4. Фронтенд сохраняет bearer в local storage.
5. Фронтенд запрашивает у бэкенда информацию о пользователе с полученным ранее bearer.
6. Бэкенд находит пользователя в базе и возвращает его данные.
7. Фронтенд кладёт информацию о пользователя в общий composable `appState.ts`.

После этого каждый вызов бэкенда происходит с добавлением авторизационного заголовка с bearer, и любой компонент может читать поля пользователя из общего стейта. Теперь разберём каждый шаг по порядку.

### Шаги 1–2: фронтенд отправляет init data

Триггером является стартовый плагин из прошлой статьи [`auth.init.ts`](https://github.com/win7user10/laraue-boards/blob/master/app/plugins/auth.init.ts). Это плагин Nuxt из директории `/app/plugins`, который запускается автоматически при загрузке приложения. В первой версии плагин просто устанавливал объект пользователя в `appState` из доступной init data: `setUser(WebApp.initData)`. Теперь же init data отправляется на бэкенд для валидации.

Каждому контроллеру бэкенда соответствует composable на фронтенде, которые определяют вызовы эндпоинтов как типизированные функции. Например, так выглядит `loadUser` в `userApi.ts`, вызывающий метод бэкенда `GET /user` и возвращающий `UserDto`:

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

Сам `client` конфигурируется в отдельном классе `userClient.ts`. Здесь происходит установка базового адреса из настроек приложения и используется клиент, работающий с авторизационными заголовками:

```ts
export const useUserClient = () => {
    const configuration = useRuntimeConfig();
    const { createClient } = useUserAuthApi();
    return createClient(configuration.public.messagesBaseAddress);
}
```

`useUserAuthApi` собственно и создаёт клиент и задаёт его поведение. Именно здесь из localStorage в запросы автоматически добавляется bearer:

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

То есть архитектура такая: `userApi` знает, *что* вызывать (эндпоинты и их формы), а `userClient`/`useUserAuthApi` знают, *как* общаться с API (базовый адрес и заголовок с bearer). Адрес `messagesBaseAddress` берётся из рантайм-конфигурации Nuxt в [`nuxt.config.ts`](https://github.com/win7user10/laraue-boards/blob/master/nuxt.config.ts), куда он подставляется из переменной окружения:

```ts
runtimeConfig: {
    public: {
        messagesBaseAddress: process.env.NUXT_PUBLIC_MESSAGES_BASE_ADDRESS
    }
}
```

### Шаг 3: бэкенд валидирует init data

Перейдем к серверной части. Фронтенд отправил на нее строку `initData` — это Encoded строка с данными пользователя с подписью Telegram. Задача бэкенда — используя ключ от бота проверить, правильная ли установлена подпись и вернуть bearer токен для авторизации.

> Подход с init data работает только с авторизацией через Telegram Mini App. Telegram также поддерживает вход с обычного сайта через виджет «Log In with Telegram» — но это отдельный механизм, разбирающийся в стате о [логине через виджет](telegram-login-widget-dotnet-auth).

Запрос приходит в [`TelegramAuthController`](https://github.com/win7user10/Laraue.Apps.Boards/blob/main/src/Laraue.Apps.Boards.WebApiHost/Controllers/TelegramAuthController.cs), который принимает request с init data и проксирует его во внутренний сервис:

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

[`TelegramAuthService`](https://github.com/win7user10/Laraue.Apps.Boards/blob/main/src/Laraue.Apps.Boards.WebApiHost/TelegramAuthService.cs) выполняет две операции: валидирует init data и выпускает токен для проверенного пользователя.

```csharp
public Task<string> Authenticate(
    AuthenticateViaStringInitDataRequest request,
    CancellationToken cancellationToken)
{
    var userData = ValidateInitData(request.InitData);
    return CreateBearerToken(userData, cancellationToken);
}
```

Задача метода валидации — получить хэш от всех пользовательских данных, используя секретный ключ от бота, и сравнить его с тем хэшем, что использовался для подписи в объекте init data. Если хэши совпадают - данные настоящие и им можно доверять.

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

`BuildHash` следует алгоритму Telegram из документации:

1. **Собрать data-check-string.** Взять каждое поле из init data, кроме hash, отсортировать ключи по алфавиту, сделать из каждого поля строку в формате `key=value`, соединить строки в одну с разделителем `\n`.
2. **Получить секретный ключ.** Секретный ключ — это `HMAC-SHA256` от строкового литерала `"WebAppData"`, ключом к которому выступает токен бота.
3. **Вычислить подпись.** Прогнать `HMAC-SHA256` по data-check-string с полученным в пункте 2 секретным ключом и перевести результат в hex-строку.

Если хеш, вычисленный сервером, совпадает с хешем от Telegram — данные подлинные. Тогда сервис десериализует поле `user` в `MiniAppUser` и считает запрос корректным. Любые отличия приведут к возникновению исключению `ForbiddenException` и клиенту вернется код `403`.

### Шаг 4: бэкенд выпускает bearer, фронтенд его сохраняет

Валидировать `initData` на каждом запросе было бы расточительно — проверка подписи это реальная работа, а init data — параметр запуска, а не то, что нужно слать на каждый вызов API. Поэтому валидация происходит один раз, при логине, а в обмен сервер выпускает **bearer-токен**. Дальше приложение шлёт этот токен с каждым запросом, и сервер доверяет ему, не перепроверяя исходные init data.

#### Выпуск токена (и регистрация при первом запуске)

Как только `ValidateInitData` подтвердил, что пользователь настоящий, `Authenticate` вызывает `CreateBearerToken`, чтобы превратить его в токен:

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

`CreateBearerToken` заодно служит точкой регистрации. Он ищет пользователя по его Telegram ID; если такой есть — выпускает токен для него, если нет — сначала регистрирует нового пользователя, затем выпускает токен. То есть самый первый запуск Mini App новым пользователем молча создаёт ему аккаунт — отдельного шага регистрации нет.

Сам токен создаёт общий `AuthService`, подписывая его секретом `Auth__Key` из конфигурации контейнера. Это стандартный JWT:

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

Это JWT с единственным claim — внутренним ID пользователя, — подписанный HMAC-SHA256 с секретом `Auth__Key`. JWT здесь, а не непрозрачный токен с серверным хранилищем сессий, — чтобы не переусложнять: токен самодостаточен, сервер валидирует его тем же симметричным ключом, которым подписал, и нет таблицы сессий, в которую надо лезть на каждом запросе.

Это и есть bearer, который фронтенд сохраняет и шлёт на последующих запросах. Дорогая проверка init data случается один раз, при логине; всё после — обычная JWT-bearer-аутентификация в стандартном заголовке `Authorization`.

#### Как bearer аутентифицирует каждый следующий запрос

Строки `AddAuthentication()` и `UseAuthentication()` в `Program.cs` отвечают за проверку bearer. JWT выпускается под именованной *схемой* аутентификации, которая говорит, как валидировать токен такого рода — какой issuer и audience ожидать и каким ключом проверять подпись (тем же `Auth__Key`, которым подписывали). `AddAuthentication()` регистрирует схему; middleware `UseAuthentication()` запускается рано в конвейере запроса, и когда приходит запрос с `Authorization: Bearer <token>`, он проверяет подпись, сверяет issuer/audience/срок и — если всё сходится — собирает `ClaimsPrincipal` из claim'ов токена и присваивает его `HttpContext.User`. Отсутствующий, просроченный или подписанный не тем ключом токен отклоняется с `401` ещё до того, как выполнится код любого контроллера.

Выигрыш в том, что контроллер никогда не парсит токены сам — он читает пользователя из `HttpContext.User`. `UserController`, в который попадает вызов `loadUser` с фронтенда, — это всё целиком:

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

То есть всё аутентифицированное чтение целиком: middleware провалидировал bearer и заполнил `HttpContext.User`, контроллер читает оттуда ID пользователя через `GetId()`, сервис загружает и возвращает пользователя. Claim `id`, записанный в `CreateUserToken`, — тот же, что читается обратно здесь.

Bearer здесь — один подписанный JWT, намеренно без машинерии «короткоживущий access плюс refresh», которую завёл бы кто-то покрупнее. Этот размен разберём на следующем шаге, когда токен уже на руках у фронтенда.

### Шаги 5–7: загрузить пользователя и положить его в стейт приложения

В прошлой статье первая версия фронтенда делала `setUser(WebApp.initData)` — брала Telegram init data и трактовала их как пользователя напрямую, без сервера. Теперь bearer на руках, и приложение делает всё по-настоящему: сохраняет токен, спрашивает бэкенд, кто пользователь, и кладёт его в общий стейт.

Сохранение токена — тонкая обёртка над local storage браузера:

```ts
const userTokenKey = 'bearer'

const getUserToken = () => localStorage.getItem(userTokenKey)
const setUserToken = async (bearerToken: string) =>
    localStorage.setItem(userTokenKey, bearerToken)
```

Local storage здесь к месту: bearer должен переживать закрытие и повторное открытие Mini App, чтобы следующий запуск переиспользовал его, а не прогонял всю процедуру авторизации заново.

Здесь снова стоит честно отметить срезанный угол. В продакшен-аутентификации не опираются на один долгоживущий bearer вот так — стандарт это короткоживущий access-токен в паре с более долгим refresh-токеном, чтобы утёкший токен был полезен лишь несколько минут, а сессии можно было отзывать. Это более правильная схема, и у реального продукта она должна быть. Она же требует реального времени: эндпоинт обновления, логика ротации, хранение и отзыв refresh-токенов, перехватчик на фронте, обновляющий токен на `401`. Для проекта масштаба «один пользователь» за аутентификацией самого Telegram эта работа сейчас почти ничего не даёт, так что её опустили в пользу одного подписанного JWT. Это осознанный размен, к которому стоит вернуться по мере роста продукта и его рисков.

С сохранённым токеном приложение загружает пользователя — тот самый вызов `loadUser`, тот `GET /user`, на который `UserController` отвечает, читая `HttpContext.User.GetId()`. Результат приходит как `UserDto` (имя, язык, цвет, аватар, инициалы, настройки), и последний шаг — положить его туда, где остальное приложение его увидит.

Две функции в composable `auth.ts` связывают сохранение и загрузку:

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

`initUserWithBearer` — это шаг после логина (шаги 4–7 флоу): сохранить свежевыпущенный bearer, вызвать `loadUser`, чтобы забрать `UserDto`, и передать его в `initUserWithUserData`, который кладёт пользователя в общий composable `appState`. Как только пользователь там, любой компонент читает текущего пользователя из стейта — имя, аватар, настройки — не запрашивая заново.

`tryAuthWithStoredBearer` — оптимизация для повторных заходов. На старте приложение проверяет, нет ли уже сохранённого bearer из прошлой сессии. Если есть — сразу переходит к загрузке пользователя с ним, и если это удаётся, пользователь залогинен мгновенно. И только если сохранённого токена нет или он протух (вызов `loadUser` падает с `401`), приложение откатывается к полному пути «провалидировать init data и выпустить новый bearer». За счёт этого повторное открытие Mini App обычно мгновенно, а не прогоняет валидацию init data каждый раз.

То есть стартовый плагин из прошлой статьи принимает финальную форму, совпадающую с флоу из начала статьи: сначала попробовать сохранённый bearer; если не вышло — взять Telegram init data, отправить на бэкенд для валидации, получить свежий bearer, сохранить его, загрузить пользователя и положить его в стейт приложения. Экран, который когда-то печатал сырой непроверенный объект init data, теперь показывает настоящего пользователя, аутентифицированного от и до.

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

Это тот же паттерн, что заложен в [статье про деплой](deploying-dotnet-postgres-vps-docker-compose): runtime-образ .NET, установленный `curl`, чтобы healthcheck контейнера мог запускаться (runtime-образы идут без него), скопированный внутрь готовый вывод сборки и точка входа. Единственные отличия от Dockerfile бота — папка, которую он копирует, и DLL, которую запускает. Имя `StructuredMessages` и в пути `COPY`, и в DLL — тот же артефакт «до переименования», что всплывает по всем файлам деплоя.

Помимо Dockerfile, сервис повторяет форму ботовского — лимит памяти, `curl`-healthcheck на `/_health` (тот же эндпоинт, та же причина ставить `curl` в образ) и `depends_on`, ждущий, пока Postgres станет healthy, прежде чем стартовать.

Три значения окружения специфичны для этого хоста. `Telegram__Token` — токен бота, который web API нужен, чтобы валидировать init data. `Auth__Key` — секрет для подписи JWT-bearer-токенов, которые он выпускает. `Cors__Hosts__0/1/2` — разрешённые CORS-origin, к которым статья вернётся в конце: они не так просты, как выглядят.

Это ещё и тот третий контейнер, который предвосхищало обсуждение лимитов ресурсов в [статье про деплой](deploying-dotnet-postgres-vps-docker-compose). С ботом, Postgres и теперь web API на одноядерном VPS фиксированные доли CPU переподписали бы ядро и перестали бы защищать друг друга — поэтому от лимитов CPU отказались в пользу лимитов только по памяти и честного планировщика ядра. Web API получает потолок в 256 МБ памяти, как и бот, и никакого ограничения по CPU.

## Маршрутизируем /api на web API в nginx

В прошлой статье конфиг nginx раздавал статический фронтенд и не содержал роутов `/api` — направлять их было некуда. Теперь есть куда. В server-блок Mini App добавляются блоки `location`, которые проксируют API-вызовы на контейнер web API:

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

Фронтенд вызывает `/api/notes-board/...` на своём же origin (`msgboard.laraue.com`), а nginx переписывает это в `/api/...` и форвардит на контейнер web API во внутренней сети Docker. Приложение и его API делят origin с точки зрения браузера — приложение раздаётся с `msgboard.laraue.com`, и его API-вызовы идут на `msgboard.laraue.com/api/notes-board/...`. Именно эта деталь про общий origin и делает CORS не-проблемой в проде, о чём — финальная часть.

## CORS: почему он важен в локальной разработке, но не в проде

Сначала — что такое CORS, потому что от этого зависит остаток раздела. CORS — Cross-Origin Resource Sharing — это механизм безопасности браузера. По умолчанию браузер отказывается давать странице с одного origin читать ответы с другого origin, где «origin» — это сочетание схемы, хоста и порта (`https://msgboard.laraue.com` — один origin; `https://abc123.ngrok-free.app` — другой). Так браузер защищает пользователей: без этого любой сайт, который вы посетили, мог бы втихую слать аутентифицированные запросы к API вашего банка, пользуясь куками, что браузер уже держит, и читать результаты. Дефолт «тот же origin» блокирует весь этот класс атак.

CORS — это контролируемое исключение из дефолта. Когда сервер *хочет* разрешить конкретным другим origin обращаться к нему, он заявляет об этом, возвращая заголовки — `Access-Control-Allow-Origin` и связанные с ним, — называющие origin, которым он доверяет. Ровно это и делает блок `UseCors(...)` в `Program.cs`: читает список разрешённых origin из конфигурации и сообщает браузеру «запросы с этих origin приветствуются». Принуждает по-прежнему браузер; сервер лишь декларирует, кого готов принять. То есть CORS здесь настроен ради одного — чтобы фронтенд, когда он раздаётся с *другого* origin, нежели API, мог делать вызовы, которые браузер иначе заблокировал бы.

Важен ли CORS — зависит от среды: в проде нет, в локальной разработке да. Разница — в origin'ах.

В проде фронтенд и API делят один. Приложение раздаётся с `msgboard.laraue.com`, и его API-вызовы идут на `msgboard.laraue.com/api/notes-board/...` — тот же origin, так что с точки зрения браузера ничего кросс-доменного нет, и CORS вообще не вступает в игру. Это и устроила маршрутизация nginx выше: приложение и его API живут за одним хостнеймом.

Локальная разработка ломает это единство. Как описано в [прошлой статье](deploy-nuxt-telegram-mini-app-https-nginx), тестировать Mini App внутри Telegram со своей машины — значит туннелировать через ngrok, а ngrok выдаёт фронтенду и бэкенду *два разных* публичных URL. Теперь приложение на одном origin, а его API — на другом, и это уже настоящий кросс-доменный запрос, который браузер контролирует через CORS. Если бэкенд явно не разрешит ngrok-origin фронтенда, каждый API-вызов блокируется. Внутри Mini App это проявляется не дружелюбной ошибкой, а **белым экраном**, потому что приложение грузится, шлёт первый API-запрос, ничего не получает в ответ и так и не рендерится.

Фикс — добавить текущий ngrok-адрес фронтенда в разрешённые origin бэкенда, в список `Cors__Hosts`, который web API читает на старте. Это тот самый шаг из настройки ngrok в прошлой статье («добавьте ngrok-URL фронтенда в CORS-allowlist бэкенда»), и вот *почему* он там: без него локальное тестирование Mini App — это белый экран.

К этому есть честная сноска. В закоммиченном конфиге среди разрешённых origin также перечислены `https://web.telegram.org`, `https://telegram.org` и `https://t.me`. В схеме с общим origin в проде они не несут никакой нагрузки — это значения, которые копируешь из ответа на форуме, пока разбираешься с CORS. Реально важен только ngrok-origin во время разработки; продовому приложению не нужен ни один из них, потому что оно вообще не делает кросс-доменный запрос. Полезный урок под этим: CORS зависит от *origin, с которого приходит запрос*, так что он вступает в игру только тогда, когда что-то разводит ваши origin'ы — здесь это локальная разработка через ngrok, — а не в продовой схеме с общим origin.

## Итоги

На экране результат выглядит почти так же, как в конце прошлой статьи: приложение открывается и показывает данные пользователя. Но за экраном теперь другое. Раньше приложение брало Telegram init data и показывало их на доверии — объект пользователя мог быть каким угодно. Теперь те же данные проходят полный круг: валидируются на сервере по токену бота, обмениваются на JWT-bearer, и объект пользователя на экране — тот, что вернул бэкенд, а не тот, что предположил фронтенд. Картинка та же, но под ней настоящая аутентификация, на которой уже можно разрабатывать реальные фичи.

## Что дальше

Раз аутентификация настоящая и приложение разговаривает с бэкендом, следующая статья разрабатывает первую реальную фичу в веб-приложении: слой issue — превращение прототипа из начала цикла в настоящие Vue-компоненты, подкреплённые API. Там же всплывёт продуктовая ошибка, обнаруженная после того, как приложение попробовали первые пользователи.