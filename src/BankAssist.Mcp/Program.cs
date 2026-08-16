using BankAssist.Mcp.Configuration;
using BankAssist.Mcp.Data;
using BankAssist.Mcp.Masking;
using BankAssist.Mcp.Models;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<VaultOptions>(builder.Configuration.GetSection(VaultOptions.SectionName));
builder.Services.Configure<MaskingOptions>(builder.Configuration.GetSection(MaskingOptions.SectionName));
builder.Services.Configure<DataOptions>(builder.Configuration.GetSection(DataOptions.SectionName));
builder.Services.Configure<AuditOptions>(builder.Configuration.GetSection(AuditOptions.SectionName));

var secrets = SecretLoader.Load(builder.Configuration);
builder.Services.AddSingleton(secrets);

var dataOptions = builder.Configuration.GetSection(DataOptions.SectionName).Get<DataOptions>() ?? new();

// Đường dẫn tương đối tính theo thư mục chứa binary, không theo working directory,
// để `dotnet run --project src/BankAssist.Mcp` và bản publish đều tìm thấy data/.
var dataDirectory = Path.IsPathRooted(dataOptions.Directory)
    ? dataOptions.Directory
    : Path.Combine(AppContext.BaseDirectory, dataOptions.Directory);

try
{
    var store = CrmDataStore.LoadFrom(dataDirectory);
    builder.Services.AddSingleton(store);
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine($"Lỗi nạp dữ liệu: {ex.Message}");
    Environment.Exit(1);
}

builder.Services.AddScoped<ICrmRepository, JsonCrmRepository>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<AesValueProtector>();
builder.Services.AddSingleton<IVault, MemoryVault>();
builder.Services.AddScoped<IPiiMasker, PiiMasker>();
builder.Services.AddScoped<ILeakGuard, LeakGuard>();
builder.Services.AddSingleton<IAuditLogger, AuditLogger>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ISessionContext>(sp => SessionContextFactory.FromHttpContext(
    sp.GetRequiredService<IHttpContextAccessor>().HttpContext,
    sp.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(SessionContextFactory))));

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// KHÔNG chuyển thành MCP tool. Nếu LLM gọi được endpoint này,
// toàn bộ mục đích của lớp masking mất tác dụng (SPEC §7, TEST-SPEC E-04).
app.MapPost("/vault/resolve", (
    HttpContext httpContext,
    SecretOptions secrets,
    IVault vault,
    VaultResolveRequest? request) =>
{
    if (!httpContext.Request.Headers.TryGetValue("X-Api-Key", out var apiKey) ||
        !string.Equals(apiKey.ToString(), secrets.ApiKey, StringComparison.Ordinal))
    {
        return Results.Unauthorized();
    }

    if (request is null || string.IsNullOrWhiteSpace(request.SessionId) || request.Tokens is null)
    {
        return Results.BadRequest(new { message = "Body rỗng hoặc thiếu sessionId/tokens." });
    }

    var resolved = vault.Resolve(request.SessionId, request.Tokens);
    return Results.Ok(new VaultResolveResponse(resolved));
});

app.MapMcp("/mcp");

await app.RunAsync();

public partial class Program { }

