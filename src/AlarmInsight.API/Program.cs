using BahyWay.SharedKernel.Infrastructure.PostgreSQL;
using BahyWay.SharedKernel.Infrastructure.Hangfire;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add PostgreSQL HA Health Monitoring
builder.Services.AddPostgreSQLHealthMonitoring();

// Configure Hangfire with PostgreSQL storage for HA
builder.Services.AddHangfireWithPostgreSQL(
    builder.Configuration,
    connectionStringName: "HangfireConnection",
    serverName: "AlarmInsight.API",
    workerCount: Environment.ProcessorCount * 5);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Add Hangfire Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

app.MapControllers();

// Example: Schedule a recurring job that monitors PostgreSQL health
RecurringJob.AddOrUpdate<PostgreSQLHealthMonitorJob>(
    "postgresql-health-monitor",
    job => job.MonitorHealthAsync(),
    Cron.MinuteInterval(5)); // Run every 5 minutes

app.Run();

// Simple authorization filter for Hangfire dashboard (for development only)
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // In production, implement proper authentication
        return true;
    }
}
