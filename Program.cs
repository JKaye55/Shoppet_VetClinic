using Shoppet_VetClinic.Components;
using Shoppet_VetClinic.Api;
using Shoppet_VetClinic.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment();
    })
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment();
    });

builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<FileStorageService>();
builder.Services.AddScoped<Shoppet_VetClinic.Services.CardService>();
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage.ProtectedSessionStorage>();
builder.Services.AddScoped<Shoppet_VetClinic.Services.NotificationService>();
builder.Services.AddScoped<MarketplaceService>();
builder.Services.AddScoped<MarketplaceCartService>();
builder.Services.AddScoped<MarketplaceOrderService>();
builder.Services.AddScoped<CommunityService>();
builder.Services.AddScoped<OwnerCareService>();
builder.Services.AddScoped<AdminAccountService>();
builder.Services.AddScoped<PetCareService>();
builder.Services.AddScoped<CommerceService>();
builder.Services.AddScoped<MobileTokenService>();
var app = builder.Build();
try { if(app.Configuration.GetValue("InitializeDatabase",true)) {
 await SharedSchemaInitializer.EnsureAsync(app.Configuration);
 await CommunitySchemaInitializer.EnsureAsync(app.Configuration);
 await CredentialMigration.EnsureAsync(app.Configuration);
} } catch(Exception ex) { app.Logger.LogError(ex,"Shared SQL initialization failed. Check connection and migration permissions."); throw; }


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Error",
        createScopeForErrors: true);

    app.UseHsts();
}

app.UseHttpsRedirection();

var webWwwroot = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
var webUploads = Path.Combine(webWwwroot, "uploads");
Directory.CreateDirectory(Path.Combine(webUploads, "community"));
Directory.CreateDirectory(Path.Combine(webUploads, "documents"));
Directory.CreateDirectory(Path.Combine(webUploads, "pets"));
Directory.CreateDirectory(Path.Combine(webUploads, "users"));
Directory.CreateDirectory(Path.Combine(webUploads, "marketplace"));

app.UseStaticFiles();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/health/ready", async (IConfiguration config) =>
{
    try
    {
        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(config.GetConnectionString("ShoppetDb"));
        await connection.OpenAsync();
        await using var command = new Microsoft.Data.SqlClient.SqlCommand("SELECT TOP(0) Id,ApiToken,ApiTokenExpiresAt,IsDisabled FROM dbo.UserAccounts; SELECT TOP(0) Reference,Subtotal,Total FROM dbo.MarketplaceOrders; SELECT TOP(0) Quantity,MarketplaceListingId,CartId FROM dbo.MarketplaceCartItems;", connection);
        await command.ExecuteNonQueryAsync();
        return Results.Ok(new { status = "ready" });
    }
    catch { return Results.StatusCode(503); }
});

app.MapMobileApi();

app.Run();
