using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using PracticeManager.Api.Data;
using PracticeManager.Api.Data.Seed;
using PracticeManager.Api.Features.Documents;
using PracticeManager.Api.Features.Notifications;
using PracticeManager.Api.Features.Practices;
using PracticeManager.Api.Features.Profile;
using PracticeManager.Api.Features.Tasks;
using PracticeManager.Api.Middleware;
using PracticeManager.Api.Services;
using PracticeManager.Api.Services.DocumentGeneration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Practice Manager Student API",
        Version = "v1",
        Description = "Student-facing API for the practice management information system prototype."
    });
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<CurrentStudentProvider>();
builder.Services.AddSingleton<FileStorage>();
builder.Services.AddSingleton<DocxTemplateRenderer>();
builder.Services.AddSingleton<DocumentStatusPolicy>();
builder.Services.AddScoped<DemoDataSeeder>();

const string CorsPolicy = "StudentFrontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"];
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors(CorsPolicy);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");

app.MapProfileEndpoints();
app.MapPracticesEndpoints();
app.MapDocumentsEndpoints();
app.MapTasksEndpoints();
app.MapNotificationsEndpoints();

await InitializeDatabaseAsync(app);

app.Run();

static async System.Threading.Tasks.Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    const int maxAttempts = 10;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            logger.LogInformation("Connecting to database (attempt {Attempt}/{Max})...", attempt, maxAttempts);
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied.");
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex, "Database not ready yet, retrying in 3s...");
            await System.Threading.Tasks.Task.Delay(3000);
        }
    }

    var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
    try
    {
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Seeding demo data failed.");
    }
}

public partial class Program;
