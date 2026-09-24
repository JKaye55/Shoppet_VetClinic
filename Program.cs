using Shoppet_VetClinic.Components;
using Shoppet_VetClinic.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<DatabaseService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<FileStorageService>();
builder.Services.AddScoped<Shoppet_VetClinic.Services.CardService>();
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage.ProtectedSessionStorage>();
builder.Services.AddScoped<Shoppet_VetClinic.Services.NotificationService>();
var app = builder.Build();

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