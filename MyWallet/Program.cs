using System;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyWallet.Data;
using MyWallet.Mappers;
using MyWallet.Services;
using MyWallet.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

// 📦 Połączenie z bazą danych PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🔧 Rejestracja serwisów (Dependency Injection)
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IExternalApiService, ExternalApiService>();
builder.Services.AddScoped<IAssetService, AssetService>();

builder.Services.AddHttpClient();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();

// ✨ Hangfire: konfiguracja storage i uruchomienie serwera
builder.Services
    .AddHangfire(cfg => cfg.UsePostgreSqlStorage(
        builder.Configuration.GetConnectionString("DefaultConnection")))
    .AddHangfireServer();

// 📦 Rejestracja ReportService – tu wrzucamy logikę cotygodniowej wysyłki
builder.Services.AddScoped<ReportService>();

// 🔄 Rejestracja mapperów (Mapperly)
builder.Services.AddScoped<UserMapper>();
builder.Services.AddScoped<PortfolioMapper>();
builder.Services.AddScoped<AssetMapper>();
builder.Services.AddScoped<TransactionMapper>();

// 🌐 Obsługa kontrolerów + JSON cycles
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// 📝 Cache
builder.Services.AddMemoryCache();

// 🌍 CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// 🔒 Middleware
app.UseHttpsRedirection();

// ❗️ Static files (wwwroot)
app.UseStaticFiles();

// ⬇️ CORS przed autoryzacją i mapowaniem kontrolerów
app.UseCors("AllowFrontend");

app.UseAuthorization();

// 🔧 (Opcjonalnie) Hangfire Dashboard pod /hangfire
app.UseHangfireDashboard("/hangfire");

// 🕒 Definiujemy recurring job – co sobotę o 18:00
RecurringJob.AddOrUpdate<ReportService>(
    "weekly-portfolio-report",
    service => service.SendWeeklyReports(),
    Cron.Weekly(DayOfWeek.Saturday, 19, 00),
    TimeZoneInfo.Local
);

// 🌍 Mapowanie endpointów z kontrolerów
app.MapControllers();

// 🚀 Endpoint testowy
app.MapGet("/", () => "API działa!");

// 🚀 Start aplikacji
app.Run();
