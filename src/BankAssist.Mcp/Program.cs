using BankAssist.Mcp.Configuration;
using BankAssist.Mcp.Data;
using BankAssist.Mcp.Masking;
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

app.MapMcp("/mcp");

await app.RunAsync();
