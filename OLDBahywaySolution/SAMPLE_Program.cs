using AlarmInsight.Application.Alarms.Commands.CreateAlarm;
using BahyWay.SharedKernel.Application.Abstractions;
using BahyWay.SharedKernel.Infrastructure.BackgroundJobs;
using BahyWay.SharedKernel.Infrastructure.Caching;
using BahyWay.SharedKernel.Infrastructure.Logging;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. ADD BASIC SERVICES
// ==========================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ==========================================
// 2. ADD LOGGING (Required by Hangfire)
// ==========================================
// Register custom IApplicationLogger before Hangfire
builder.Services.AddSingleton(typeof(IApplicationLogger<>), typeof(ApplicationLogger<>));

// ==========================================
// 3. ADD DATABASE CONTEXT & REPOSITORIES
// ==========================================
// Example (adjust to your actual implementation):
// builder.Services.AddDbContext<AlarmInsightDbContext>(options =>
//     options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
//
// builder.Services.AddScoped<IAlarmRepository, AlarmRepository>();
// builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ==========================================
// 4. ADD CACHING (Required by handlers)
// ==========================================
// Example:
// builder.Services.AddStackExchangeRedisCache(options =>
// {
//     options.Configuration = builder.Configuration.GetConnectionString("Redis");
// });
// builder.Services.AddSingleton<ICacheService, CacheService>();

// ==========================================
// 5. ADD MEDIATR (Registers all handlers)
// ==========================================
builder.Services.AddMediatR(cfg => {
    // Register handlers from the Application assembly
    cfg.RegisterServicesFromAssembly(typeof(CreateAlarmCommand).Assembly);
});

// ==========================================
// 6. CONFIGURE HANGFIRE WITH POSTGRESQL
// ==========================================
// This registers:
// - Hangfire services (IBackgroundJobClient, IRecurringJobManager)
// - IBackgroundJobService implementation
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.ConfigureBahyWayHangfire(connectionString, "AlarmInsight");

// IMPORTANT: Do NOT register IBackgroundJobService again!
// ConfigureBahyWayHangfire already does this

// ==========================================
// 7. ADD CORS (For development)
// ==========================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ==========================================
// BUILD THE APPLICATION
// ==========================================
var app = builder.Build();

// ==========================================
// CONFIGURE MIDDLEWARE PIPELINE
// ==========================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Add Hangfire Dashboard (only in development for security)
    // Navigate to: https://localhost:5001/hangfire
    app.UseHangfireDashboard("/hangfire");
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();


// ==========================================
// TROUBLESHOOTING CHECKLIST
// ==========================================
/*
 * If you still get the error, verify:
 *
 * 1. NuGet Packages Installed:
 *    - Hangfire.Core
 *    - Hangfire.AspNetCore
 *    - Hangfire.PostgreSql
 *    - Npgsql
 *    - MediatR
 *    - MediatR.Extensions.Microsoft.DependencyInjection
 *
 * 2. All dependencies registered:
 *    - IApplicationLogger<T>
 *    - IAlarmRepository
 *    - IUnitOfWork
 *    - ICacheService
 *    - DbContext (if using EF Core)
 *
 * 3. Connection string is correct in appsettings.json:
 *    {
 *      "ConnectionStrings": {
 *        "DefaultConnection": "Host=localhost;Database=alarminsight;Username=postgres;Password=yourpassword"
 *      }
 *    }
 *
 * 4. Service registration order:
 *    - Logging BEFORE Hangfire
 *    - MediatR BEFORE Hangfire
 *    - All infrastructure services BEFORE Hangfire
 *
 * 5. No duplicate registrations:
 *    - Don't register IBackgroundJobService twice
 *
 * 6. Check the complete exception stack trace:
 *    - Look at the InnerException for more details
 *    - Use View Details in Visual Studio to see which exact service is missing
 */
