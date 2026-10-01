using Shoppet_VetClinic.Components;
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
var app = builder.Build();
try { if(app.Configuration.GetValue("InitializeDatabase",true)) {
 await SharedSchemaInitializer.EnsureAsync(app.Configuration);
 await CommunitySchemaInitializer.EnsureAsync(app.Configuration);
 await CredentialMigration.EnsureAsync(app.Configuration);
} } catch(Exception ex) { app.Logger.LogError(ex,"Shared SQL initialization failed. Check connection and migration permissions."); }


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Error",
        createScopeForErrors: true);

    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
