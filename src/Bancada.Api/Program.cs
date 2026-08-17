using Bancada.Api.Endpoints;
using Bancada.Application;
using Bancada.Infrastructure;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 6 * 1024 * 1024);

if (!builder.Environment.IsEnvironment("Testing"))
{
    var connectionString = builder.Configuration.GetConnectionString("Bancada")
        ?? throw new InvalidOperationException(
            "ConnectionStrings:Bancada must be configured with user secrets or an environment variable.");
    builder.Services.AddDbContext<BancadaDbContext>(options => options.UseNpgsql(connectionString));
}

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddEntityFrameworkStores<BancadaDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "bancada.session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddPolicy("Client", policy =>
{
    var origin = builder.Configuration["Client:Origin"] ?? "https://localhost:7101";
    policy.WithOrigins(origin).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
}));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<DevelopmentDataSeeder>();
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.AddScoped<IFileStorage>(services =>
{
    var options = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<StorageOptions>>().Value;
    if (string.Equals(options.Provider, "R2", StringComparison.OrdinalIgnoreCase))
    {
        return new R2FileStorage(options.R2);
    }

    var environment = services.GetRequiredService<IWebHostEnvironment>();
    var rootPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.LocalPath));
    return new LocalFileStorage(rootPath);
});

var app = builder.Build();

if (app.Environment.IsEnvironment("Testing"))
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler();
}
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("Client");
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapRecipeEndpoints();
app.MapChallengeEndpoints();
app.MapProfileEndpoints();
app.MapMysteryBoxEndpoints();

if (app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<BancadaDbContext>();
    await dbContext.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync();
}

app.Run();

public partial class Program;
