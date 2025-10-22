using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using SidecarWeb.Components;
using SidecarWeb.Options;
using SidecarWeb.Services;
using SidecarWeb.SQL.Migration;
using SidecarWeb.Utility;
using SQLMigration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHttpClient();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(p => p.GetRequiredService<AuthProvider>());
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Audience = "minecraft-sidecar";
    });
builder.Services.AddAuthorizationCore();

builder.Services.Configure<OAuth2Option>(builder.Configuration.GetRequiredSection("OAuth2"));
builder.Services.Configure<MySqlOptions>(builder.Configuration.GetRequiredSection("MySql"));

builder.Services.Configure<JwtOptions>(builder.Configuration.GetRequiredSection("JwtOptions"));
builder.Services.AddScoped<JwtTokenIssuer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await StartMigrationAsync(app.Lifetime.ApplicationStopping);

app.Run();

async ValueTask StartMigrationAsync(CancellationToken cancellationToken)
{
    var options = app.Services.GetRequiredService<IOptions<MySqlOptions>>();
    var scripts = new Scripts();
    var logger = new LoggerTextWriter(app.Logger);
    await Executor.RunAsync(options.Value.ConnectionString, options.Value.Database, [.. scripts.GetScripts()], logger, cancellationToken);
}