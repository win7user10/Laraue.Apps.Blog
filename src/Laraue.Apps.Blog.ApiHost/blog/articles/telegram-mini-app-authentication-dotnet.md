---
title: Telegram Mini App authentication in .NET, end to end — validating initData, issuing a JWT, and the Nuxt frontend
description: Part 8 of building a Telegram task tracker solo. The complete Telegram Mini App authentication flow in a real .NET and Nuxt app — validating the initData signature server-side with HMAC-SHA256, issuing and consuming a JWT bearer, reading the user from HttpContext, and why CORS matters.
type: article
createdAt: 2026-06-24 08:00
updatedAt: 2026-06-24 08:00
projects: [boards]
tags: [dotnet, aspnet-core, nuxt, telegram-mini-app, authentication, initdata, jwt, cors, devlog]
previousLink: deploy-nuxt-telegram-mini-app-https-nginx
---

> **Architecture First: Building a Jira Alternative Solo, AI-Assisted** — Part 8.
> The [previous article](deploy-nuxt-telegram-mini-app-https-nginx) got the Mini App opening inside Telegram, showing the raw user object. But it trusted that data blindly. This article makes it real: a backend for the app to talk to, and authentication that actually proves who the user is.

At the end of the last article the Mini App opened, read the Telegram init data, and displayed the user object — but it never checked whether that data was genuine. Anyone could have handed the app a fabricated init data string and it would have believed it. That was fine as a "does the pipe work" proof; it is not fine as authentication. This article closes that gap, and to do so it needs the thing the frontend has been missing entirely: a backend.

## The backend: a web API host

That backend is a new host. Back in the [backend architecture article](clean-dotnet-telegram-bot-architecture) the hosts were named by function — `TelegramHost` for the bot, and a `WebApiHost` that was mentioned but did not yet exist. Now it exists. The [`WebApiHost`](https://github.com/win7user10/Laraue.Apps.Boards/tree/main/src/Laraue.Apps.Boards.WebApiHost) is its own host, its own runnable project, deployed as its own container — exactly the per-host separation argued for earlier, now with a second host actually filling the role.

It shares everything below the host layer with the bot. The same `DataAccess` models, the same core `Services` — creating an issue from the web API runs the very same core logic the bot already uses. What is new is a host-specific service layer for the web surface, and the host itself.

The host's `Program.cs` is short, and reads as a fairly standard ASP.NET Core web API — with two things bolted on that matter for this article: authentication and CORS.

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

Most of this is shared with the bot and was covered earlier in the series. The `TelegramOptions` carry the bot token (the web API needs it too, to validate init data — more on that below). `AddDatabaseServices` registers the same data layer the bot uses; the migrate-on-startup block and `MapHealthChecks("/_health")` are the same patterns from the bot's host, and `app.Services.UseLinq2Db()` is the same EF-Core-plus-linq2db setup described in the [backend architecture article](clean-dotnet-telegram-bot-architecture).

What is genuinely new here is the pair of lines that bracket everything: `AddAuthentication()` / `UseAuthentication()`, and the CORS block at the end. Those two are the whole subject of this article — proving who the user is, and controlling which origins are allowed to talk to the API. The CORS configuration, pulling its allowed origins from a `Cors:Hosts` config section, is explained in full in the final section, including why it matters in local development but not in production.

### Sharing the core, layering the host on top

`AddApplicationServices` deserves a closer look, because it is where the core-versus-host-specific split from that earlier article shows up concretely. It is *not* one shared method — each host has its own [`AddApplicationServices`](https://github.com/win7user10/Laraue.Apps.Boards/blob/main/src/Laraue.Apps.Boards.WebApiHost/WebApplicationBuilderExtensions.cs), and the web API's version does two things: it registers the services specific to this host, and it calls into the shared core registration. Its first line is [`builder.AddCoreServices()`](https://github.com/win7user10/Laraue.Apps.Boards/blob/main/src/Laraue.Apps.Boards.Services/WebApplicationBuilderExtensions.cs), which registers the core `Services` — the surface-agnostic logic that creates issues, moves them, and so on, the same logic the bot calls. Everything after that line is host-specific: the web-facing services that wrap the core, the `TelegramAuthService` that validates init data, an `ITelegramBotClient`, the exception-handling middleware, and `AddControllers()` for the HTTP surface.

So the bot and the web API each register their *own* host-specific services and share the *same* core registration underneath — exactly the layering the structure article described, now visible in one method. As new features arrive later in the series, their services are added here, but the shape stays the same: core services shared, host-specific services layered on top.

The `ExceptionHandleMiddleware` registered here is a small piece of new behaviour for the web surface: it is a custom middleware from our shared [Laraue.Core](https://github.com/win7user10/Laraue.Core) library that maps the library's own web exceptions onto HTTP status codes automatically. Throw a `BadRequestException` from anywhere in the request, and it comes back to the client as a `400` without the calling code having to build the response — `ForbiddenException` becomes `403`, and so on. It means the service layer can express failures as plain exceptions and let the middleware translate them into correct HTTP responses, which is a concern the bot never had.

### Two hosts, one database: migrations stay safe

One detail worth flagging: this host *also* runs `MigrateAsync()` on startup, just like the bot. With two hosts now potentially booting at the same time and both pointed at the same database, the obvious worry is a race — two processes trying to apply the same migrations at once. EF Core handles this for us: applying migrations acquires a database lock, so the migration step is serialised even across separate processes. If both hosts start together, one acquires the lock and applies any pending migrations while the other simply waits; once the first finishes and releases the lock, the second sees the schema is already up to date and continues its startup. So migrate-on-startup stays safe with more than one host — the database itself enforces that migrations run one at a time.

## The authentication flow, end to end

Before the code, here is the whole sequence the login goes through, so the pieces below have somewhere to hang. When the Mini App starts:

1. The app checks whether it is running inside Telegram — does it have init data? (the [`auth.init.ts`](https://github.com/win7user10/laraue-boards/blob/master/app/plugins/auth.init.ts) plugin from the previous article).
2. If it does, it sends that init data to the web API's authentication endpoint.
3. The backend validates the init data's signature against the bot token, and returns a **bearer token**.
4. The frontend saves the bearer to local storage.
5. The frontend then makes a second call — "give me the current user" — now authenticated with that bearer.
6. The backend reads the user from the database and returns the user data.
7. The frontend puts that user into a shared app-state composable.

From then on, every request the app makes carries the stored bearer, and any component can read the user object out of app state. The rest of this section walks that path — frontend trigger, backend validation, bearer, and the user round-trip — in order.

### Step 1–2: the frontend sends the init data

The trigger is the startup plugin from the previous article — [`auth.init.ts`](https://github.com/win7user10/laraue-boards/blob/master/app/plugins/auth.init.ts), a Nuxt plugin that runs automatically when the app boots. (Nuxt runs any file in the `plugins/` directory on startup; that is how this code executes without anything explicitly calling it.) In its first version the plugin did `setUser(WebApp.initData)` and trusted the data blindly. Now it hands the init data to the backend instead.

Requests go through a small stack of composables. The top layer is `userApi.ts`, which defines the actual endpoints as small typed functions — for example `loadUser`, which is just a `GET /user` returning a `UserDto`:

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

It gets its `client` from `userClient.ts`, the layer underneath, which builds an authenticated HTTP client pointed at the web API's base address:

```ts
export const useUserClient = () => {
    const configuration = useRuntimeConfig();
    const { createClient } = useUserAuthApi();
    return createClient(configuration.public.messagesBaseAddress);
}
```

`useUserAuthApi` is the piece that actually creates the client and gives it its behaviour. This is where the bearer is attached, so it is worth seeing:

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

The line that matters is the header: `Authorization: Bearer ${getUserToken()}`. Every client built here reads the stored bearer from local storage and attaches it, so once login has happened, nothing downstream has to think about authentication again — `userApi` just calls endpoints, and the token rides along automatically.

So the layering on the frontend mirrors the backend's: `userApi` knows *what* to call (the endpoints and their shapes), and `userClient`/`useUserAuthApi` know *how* to talk to the API (the base address and the bearer header).

The base address itself, `messagesBaseAddress`, comes from Nuxt's runtime configuration in [`nuxt.config.ts`](https://github.com/win7user10/laraue-boards/blob/master/nuxt.config.ts), where it is read from an environment variable so it can differ per environment without code changes:

```ts
runtimeConfig: {
    public: {
        messagesBaseAddress: process.env.NUXT_PUBLIC_MESSAGES_BASE_ADDRESS,
        // ...
    }
}
```

In production that value is the same origin the app is served from; in local development it is the ngrok backend URL. That one configuration point is exactly where the CORS story at the end of this article comes from — it is what determines whether the API lives on the same origin as the app or a different one.

### Step 3: the backend validates the init data

Now the real work on the server. When the Mini App launches inside Telegram, Telegram provides `initData` — a string containing the user's identity and other launch parameters. Crucially, Telegram **signs** this data with a hash derived from the bot's token. That signature is what makes trustworthy authentication possible: a server that knows the bot token can verify the signature and be certain the data genuinely came from Telegram and was not tampered with.

(This `initData` flow is the one specific to Mini Apps — apps running *inside* Telegram. Telegram also supports logging in from an ordinary external website, through its "Log In with Telegram" widget, which the web app will gain later in the series; that is a separate mechanism, covered when we get to it. For now the app lives inside Telegram, so `initData` is the right path.)

The first version of the frontend skipped this entirely — it took `initData` and trusted it. The web API does it properly: the frontend sends the raw `initData` to an authentication endpoint, the server validates the signature, and only if it checks out does it treat the request as a genuine user.

That endpoint is a thin controller — [`TelegramAuthController`](https://github.com/win7user10/Laraue.Apps.Boards/blob/main/src/Laraue.Apps.Boards.WebApiHost/Controllers/TelegramAuthController.cs) — that simply receives the init data and hands it to the service:

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

Notably this endpoint has no `[Authorize]` attribute — it cannot, because this is the call that *establishes* authentication; there is no bearer yet. It accepts the raw init data in the request body and returns the bearer token. Everything else in the API requires a bearer, but this one door has to be open for the handshake to start.

The controller hands off to the [`TelegramAuthService`](https://github.com/win7user10/Laraue.Apps.Boards/blob/main/src/Laraue.Apps.Boards.WebApiHost/TelegramAuthService.cs), whose `Authenticate` method is the entry point — and it reads as the two halves of the whole flow: validate the init data, then turn the validated user into a token.

```csharp
public Task<string> Authenticate(
    AuthenticateViaStringInitDataRequest request,
    CancellationToken cancellationToken)
{
    var userData = ValidateInitData(request.InitData);
    return CreateBearerToken(userData, cancellationToken);
}
```

The validation comes first. Two methods do that work: `ValidateInitData`, which checks the signature, and `BuildHash`, which recomputes it the way Telegram does.

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

The init data arrives as a URL query string — `user=...&auth_date=...&hash=...` and so on. `ValidateInitData` parses it, lifts out the `hash` that Telegram included, and removes it from the collection, because the hash is computed over *everything except itself*. If there is no hash at all, the request is rejected immediately.

`BuildHash` is the heart of it, and it follows Telegram's documented algorithm exactly. There are three steps:

1. **Build the data-check-string.** Take every remaining field, sort the keys in ordinal order, format each as `key=value`, and join them with newline characters. The sort matters — Telegram computes its hash over the fields in a specific order, so the server has to reproduce that order precisely or the hashes will never match.
2. **Derive the secret key.** This is the step that ties the signature to *this specific bot*. The secret key is `HMAC-SHA256` of the literal string `"WebAppData"` keyed by the bot token. Only someone holding the bot token can produce this key, which is what makes the whole scheme trustworthy.
3. **Compute the signature.** Run `HMAC-SHA256` over the data-check-string using that secret key, and hex-encode the result.

If the hash the server computes matches the hash Telegram sent, the data is genuine — it came from Telegram, it was signed with this bot's token, and nothing in it was altered in transit. Only then does the service deserialize the `user` field into a `MiniAppUser` and treat the request as that user. Any mismatch throws, and the request is refused.

This is exactly the check the first version of the frontend did not do. There, `setUser(WebApp.initData)` trusted the data as-is; here, the server proves it before trusting it. The validation has to be on the server, too — it depends on the bot token, which can never be shipped to the browser, so this is inherently a backend job.

One thing this implementation does *not* do, which a stricter one should: check freshness. The init data includes an `auth_date` field — the timestamp of when Telegram produced it — and a hardened implementation rejects data older than a few minutes, so that a captured init data string cannot be replayed indefinitely. The signature check proves the data is *authentic*; the `auth_date` check would prove it is *recent*. For a single-user project the replay window is a small risk and it was left out, but for anything handling real users it is worth adding — it is a few lines comparing `auth_date` against the current time, and it closes a real gap that the signature check alone does not.

### Step 4: the backend issues a bearer, the frontend stores it

Validating `initData` on every single request would be wasteful — the signature check is real work, and the init data is a launch parameter, not something to re-send on every API call. So validation happens once, at login, and in exchange the server issues a **bearer token**. From then on, the app sends that token with each request, and the server trusts it without re-validating the original init data.

#### Issuing the token (and registering on first launch)

Once `ValidateInitData` has confirmed the user is genuine, `Authenticate` calls `CreateBearerToken` to turn that user into a token:

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

`CreateBearerToken` does double duty as the registration point. It looks up a user by their Telegram ID; if one exists, it issues a token for them, and if not, it registers a new user first and then issues the token. So a brand-new user's very first Mini App launch silently creates their account — there is no separate sign-up step.

The token itself is created by a shared `AuthService`, signed with the `Auth__Key` secret from the container configuration. It is a standard JWT:

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

It is a JWT carrying a single claim — the internal user ID — signed with HMAC-SHA256 using the `Auth__Key` secret. Choosing a JWT here, rather than an opaque token backed by a server-side session store, is a deliberate fit for the scale: the token is self-contained, the server validates it with the same symmetric key it signed with, and there is no session table to look up on every request.

This is the bearer the frontend stores and sends on subsequent requests. The expensive init-data check happens once, at login; everything after is ordinary JWT bearer auth in the standard `Authorization` header.

#### How the bearer authenticates each later request

The `AddAuthentication()` and `UseAuthentication()` lines in `Program.cs` are what make that bearer mean something. The JWT is issued under a named authentication *scheme* that says how a token of this kind is validated — which issuer and audience to expect, and which key verifies the signature (the same `Auth__Key` used to sign it). `AddAuthentication()` registers the scheme; the `UseAuthentication()` middleware runs early in the request pipeline, and when a request arrives carrying `Authorization: Bearer <token>` it verifies the signature, checks issuer/audience/expiry, and — if everything holds — builds a `ClaimsPrincipal` from the token's claims and assigns it to `HttpContext.User`. A missing, expired, or wrong-key token is rejected with a `401` before any controller code runs.

The payoff is that a controller never parses tokens itself — it just reads the user off `HttpContext.User`. The `UserController` that the frontend's `loadUser` call hits is the whole thing:

```csharp
[HttpGet]
public Task<UserDto> GetAsync(CancellationToken ct)
{
    return service.GetUser(HttpContext.User.GetId(), ct);
}
```

`HttpContext.User` is the `ClaimsPrincipal` the middleware built from the bearer, and `GetId()` is a small extension that pulls the user ID back out of the `id` claim that was baked into the token at login:

```csharp
public static Guid GetId(this ClaimsPrincipal claimsPrincipal)
{
    var id = claimsPrincipal.FindFirstValue("id");
    return Guid.Parse(id!);
}
```

So the entire authenticated read is: the middleware validated the bearer and populated `HttpContext.User`, the controller reads the user's ID from it via `GetId()`, and the service loads and returns that user. The `id` claim written in `CreateUserToken` is the same one read back here — that is the round trip the JWT exists to make.

The bearer here is a single signed JWT, deliberately without the short-lived-access-plus-refresh-token machinery a larger system would use — a trade-off worth examining once the token is in hand on the frontend, which the next step does.

### Steps 5–7: load the user and put it in app state

Back in the previous article, the frontend's first version simply did `setUser(WebApp.initData)` — it took Telegram's init data and treated it as the user directly, with no server involved. Now the bearer is in hand, and the app does the real thing: store the token, ask the backend who the user is, and put that user into shared state.

Storing the token is a thin wrapper over the browser's local storage:

```ts
const userTokenKey = 'bearer'

const getUserToken = () => localStorage.getItem(userTokenKey)
const setUserToken = async (bearerToken: string) =>
    localStorage.setItem(userTokenKey, bearerToken)
```

Local storage is the right fit here: the bearer needs to survive the Mini App being closed and reopened, so the next launch can reuse it instead of re-running the whole Telegram handshake.

This is the place to be honest about a shortcut. In a production-grade auth system you would not lean on a single long-lived bearer like this — the standard is a short-lived access token paired with a longer-lived refresh token, so that a leaked token is only useful for a few minutes and sessions can be revoked. That is the more correct design, and a real product handling many users' data should have it. It also takes real time to build properly: the refresh endpoint, the rotation logic, the storage and revocation of refresh tokens, the frontend interceptor that transparently refreshes on a 401. For a single-user-scale project behind Telegram's own authentication, that work buys very little right now, so it was deliberately skipped in favour of one signed JWT. It is a conscious trade — the kind of corner that is fine to cut early and worth coming back to as the product and its risk grow — not something to copy blindly into an app with real users and real data.

With the token stored, the app loads the user — the `loadUser` call from earlier, the `GET /user` that the `UserController` above answers by reading `HttpContext.User.GetId()`. The result comes back as a `UserDto` (username, language, colour, avatar, initials, preferences), and the last step is putting it somewhere the rest of the app can see it.

The two functions in the `auth.ts` composable tie storage and loading together:

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

`initUserWithBearer` is the post-login step (flow steps 4–7): store the freshly issued bearer, call `loadUser` to fetch the `UserDto`, and hand it to `initUserWithUserData`, which puts the user into a shared `appState` composable. Once it is there, any component in the app can read the current user out of app state — name, avatar, preferences — without fetching it again.

`tryAuthWithStoredBearer` is the optimisation for return visits. On startup, before doing the Telegram handshake at all, the app checks whether it already has a stored bearer from a previous session. If it does, it skips straight to loading the user with it — and if that succeeds, the user is logged in instantly. Only if there is no stored token, or it has expired (the `loadUser` call fails with a 401), does the app fall back to the full validate-init-data-and-issue-a-new-bearer path. It is a small thing, but it means reopening the Mini App is usually instant rather than re-running the signature exchange every time.

So the startup plugin from the previous article gains its real shape, and it matches the flow laid out at the top of this section: try a stored bearer first; if that fails, take the Telegram init data, send it to the backend to be validated, receive a fresh bearer, store it, load the user, and drop the user into app state. The blank screen that once printed a raw, unverified init data object now shows a real user, authenticated end to end.

## Adding it to Docker Compose

With the web API written and the authentication flow working, the remaining work is deployment: getting this second host running on the server next to the bot, and routing the app's API calls to it. The web API becomes the third container in the stack, alongside the bot and PostgreSQL:

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

The Dockerfile it builds from is essentially the bot's, pointed at the web API's output:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY structured-messages-webapi-host app
WORKDIR app
ENTRYPOINT ["dotnet", "Laraue.Apps.StructuredMessages.WebApiHost.dll"]
```

It is the same pattern established in the [deploy article](deploying-dotnet-postgres-vps-docker-compose): the .NET runtime image, `curl` installed so the container healthcheck can actually run (the runtime images ship without it), the pre-built output copied in, and an entry point. The only differences from the bot's Dockerfile are the folder it copies and the DLL it runs. The `StructuredMessages` name in both the `COPY` path and the DLL is the same pre-rename artifact that turns up throughout the deployment files.

Beyond the Dockerfile, the service follows the same shape as the bot's — a memory limit, a `curl` healthcheck against `/_health` (the same endpoint, the same reason `curl` is installed in the image), and `depends_on` waiting for Postgres to be healthy before it starts.

Three environment values are specific to this host. `Telegram__Token` is the bot token, which the web API needs in order to validate init data. `Auth__Key` is the secret used to sign the JWT bearer tokens it issues. And `Cors__Hosts__0/1/2` are the allowed CORS origins. The article comes back to these at the end — they are less straightforward than they look, and the story of when CORS actually matters here is not the obvious one.

This is also the third container that the resource-limits discussion in the [deploy article](deploying-dotnet-postgres-vps-docker-compose) anticipated. With the bot, Postgres, and now the web API all on a single-core VPS, fixed CPU fractions would have over-subscribed the core and stopped protecting each other — which is exactly why CPU limits were dropped in favour of memory-only limits and the kernel's fair scheduling. The web API gets a 256 MB memory ceiling, like the bot, and no CPU cap. The decision made earlier pays off now that the third container is real.

## Routing /api to the web API in nginx

In the previous article the nginx config served the static frontend and deliberately left out the `/api` routes, because there was no backend to route them to. Now there is. The Mini App's server block gains `location` blocks that proxy API calls to the web API container:

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

The frontend calls `/api/notes-board/...` on its own origin (`msgboard.laraue.com`), and nginx rewrites that to `/api/...` and forwards it to the web API container on the internal Docker network. The app and its API share an origin from the browser's point of view — the app is served from `msgboard.laraue.com` and its API calls go to `msgboard.laraue.com/api/notes-board/...` — which keeps the browser-facing setup simple. (This same-origin detail is what makes CORS a non-issue in production, explained in the final section.)

## CORS: why it matters in local dev but not production

First, what CORS even is, because the rest of this section depends on it. CORS — Cross-Origin Resource Sharing — is a browser security mechanism. By default, a browser refuses to let a page on one origin read responses from a different origin, where an "origin" is the combination of scheme, host, and port (`https://msgboard.laraue.com` is one origin; `https://abc123.ngrok-free.app` is another). This is the browser protecting users: without it, any site you visited could quietly fire authenticated requests at your bank's API using cookies your browser already holds, and read the results. The same-origin default blocks that whole class of attack.

CORS is the controlled exception to that default. When a server *wants* to allow specific other origins to call it, it says so by returning headers — `Access-Control-Allow-Origin` and friends — naming the origins it trusts. That is exactly what the `UseCors(...)` block in `Program.cs` does: it reads a list of allowed origins from configuration and tells the browser "requests from these origins are welcome." The browser still does the enforcing; the server is only declaring who it is willing to accept. So CORS is set up here for one reason — to let the frontend, when it is served from a *different* origin than the API, make calls the browser would otherwise block.

Whether that matters depends on the environment. In production it does not; in local development it does. The difference is origins.

In production, the frontend and the API share one. The app is served from `msgboard.laraue.com`, and its API calls go to `msgboard.laraue.com/api/notes-board/...` — same origin, so from the browser's point of view nothing is cross-origin at all, and CORS never enters the picture. That is exactly what the nginx routing above arranged: the app and its API live behind the same hostname.

Local development breaks that unity. As described in the [previous article](deploy-nuxt-telegram-mini-app-https-nginx), testing the Mini App inside Telegram from your machine means tunnelling with ngrok — and ngrok gives the frontend and the backend *two different* public URLs. Now the app is on one origin and its API is on another, which is a genuine cross-origin request, and the browser enforces CORS. If the backend does not explicitly allow the frontend's ngrok origin, every API call is blocked. Inside the Mini App that does not show up as a friendly error — it shows up as a **white screen**, because the app loads, fires its first API request, gets nothing back, and never renders.

The fix is to add the frontend's current ngrok address to the backend's allowed origins — the `Cors__Hosts` list the web API reads at startup. That is the step from the previous article's ngrok setup ("add the ngrok frontend URL to the backend's CORS allow-list"), and this is *why* it is there: without it, local Mini App testing is a blank screen.

There is an honest footnote to this. The committed config also lists `https://web.telegram.org`, `https://telegram.org`, and `https://t.me` as allowed origins. Those are, frankly, the kind of values that get copied from a forum answer when you are fighting a CORS problem and trying everything — and in this same-origin production setup they are not actually doing any load-bearing work. The origin that genuinely matters is the ngrok one during development; the production app does not need any of them, because it is not making a cross-origin request in the first place. It is a small, real example of how configuration accretes lines that look meaningful but are mostly there because they were in some snippet that "fixed it" once. The useful lesson underneath is the one worth keeping: CORS depends on the *origin the request comes from*, so it only comes into play when something splits your origins — here, local development with ngrok — and not in the same-origin production setup.

## Where this leaves us

On screen, the result looks almost identical to the end of the previous article: the app opens and shows the user's data. But what is behind that screen is completely different. Before, the app took Telegram's init data and displayed it on trust — the user object could have been anything. Now that same data has made a round trip: validated on the server against the bot token, exchanged for a JWT bearer, and the user object on screen is one the backend returned, not one the frontend assumed. The picture is the same; the foundation under it is real — and real enough to start building actual features on, which is the next step.

## What comes next

With authentication real and the app talking to a backend, the next article builds the first actual feature in the web app: the issues layer — turning the prototype from early in the series into real Vue components backed by the API. It is also where a product mistake surfaces, discovered when a friend first tried the bot.