using Microsoft.EntityFrameworkCore;
using ProteinTracker.Api.Data;
using ProteinTracker.Api.Exceptions;
using ProteinTracker.Api.Repositories;
using ProteinTracker.Api.Services;
using ProteinTracker.Api.Swagger;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("ProteinTrackerDatabase")
    ?? throw new InvalidOperationException("Connection string 'ProteinTrackerDatabase' was not found.");


builder.Services.AddDbContext<ProteinTrackerDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<FoodRepository>();
builder.Services.AddScoped<FoodService>();
builder.Services.AddScoped<FoodEntryRepository>();
builder.Services.AddScoped<FoodEntryService>();
builder.Services.AddScoped<DailyTargetRepository>();
builder.Services.AddScoped<DailyTargetService>();
builder.Services.AddSingleton(
    TimeZoneInfo.FindSystemTimeZoneById("Europe/Bratislava"));
builder.Services.AddScoped<DailySummaryService>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();

builder.Services.AddProteinTrackerSwagger();

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
