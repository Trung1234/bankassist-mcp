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
try
{
    var store = CrmDataStore.LoadFrom(dataOptions.Directory);
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
builder.Services.AddScoped<LeakGuard>();
builder.Services.AddSingleton<AuditLogger>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

await app.RunAsync();
