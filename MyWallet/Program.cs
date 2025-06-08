using System;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyWallet.Data;
using MyWallet.Mappers;
using MyWallet.Services;
using MyWallet.Services.Implementations;
using NLog;
using NLog.Web;

var logger = NLogBuilder
    .ConfigureNLog("nlog.config")      // ładuje konfigurację NLog z pliku
    .GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // 🎯 Konfiguracja logowania: wyczyść domyślne provider’y, ustaw NLog
    builder.Logging.ClearProviders();
    builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
    builder.Host.UseNLog();

    // 🔒 Wczytaj tajne ustawienia w środowisku Development
    if (builder.Environment.IsDevelopment())
    {
        builder.Configuration.AddUserSecrets<Program>();
    }

    // 🔑 Wczytaj zmienne środowiskowe z prefixem MYWALLET_
    builder.Configuration.AddEnvironmentVariables(prefix: "MYWALLET_");

    // 📦 Połączenie z bazą danych PostgreSQL
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // 🔧 Rejestracja serwisów (Dependency Injection)
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IPortfolioService, PortfolioService>();
    builder.Services.AddScoped<ITransactionService, TransactionService>();
    builder.Services.AddScoped<IExternalApiService, ExternalApiService>();
    builder.Services.AddScoped<IAssetService, AssetService>();

    // 🔌 HTTP Client
    builder.Services.AddHttpClient();

    // 📧 Konfiguracja EmailSettings z IOptions
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

    // 🔐 WYMAGANE: Distributed Memory Cache dla sesji
    builder.Services.AddDistributedMemoryCache();

    // 🔐 Konfiguracja sesji dla funkcjonalności administratora
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

    // 🌍 CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy
                .WithOrigins("http://localhost:3000")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials(); // dla sesji
        });
    });

    var app = builder.Build();

    // 🌩️ Globalna obsługa wyjątków – przechwytuje wszystkie nieobsłużone wyjątki
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
            var log = context.RequestServices.GetRequiredService<ILogger<Program>>();
            log.LogError(ex, "Globalny nieobsłużony wyjątek");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Wewnętrzny błąd serwera");
        });
    });

    // 🔒 Middleware
    app.UseHttpsRedirection();

    // ❗️ Static files (wwwroot)
    app.UseStaticFiles();

    // ⬇️ CORS przed autoryzacją i mapowaniem kontrolerów
    app.UseCors("AllowFrontend");

    // 🔐 Włączenie obsługi sesji
    app.UseSession();

    app.UseAuthorization();

    // 🔧 (Opcjonalnie) Hangfire Dashboard pod /hangfire
    app.UseHangfireDashboard("/hangfire");

    // 🕒 Definiujemy recurring job – co sobotę o 19:00 (TimeZoneInfo.Local → Europe/Warsaw)
    RecurringJob.AddOrUpdate<ReportService>(
        "weekly-portfolio-report",
        service => service.SendWeeklyReports(),
        Cron.Weekly(DayOfWeek.Saturday, 19, 0),
        TimeZoneInfo.Local
    );

    // 🌍 Mapowanie endpointów z kontrolerów
    app.MapControllers();

    // 🚀 Endpoint testowy
    app.MapGet("/", () => "API działa!");

    // 🚀 Start aplikacji
    app.Run();
}
catch (Exception ex)
{
    // logowanie wyjątków podczas startu aplikacji
    logger.Error(ex, "Program start-up failed");
    throw;
}
finally
{
    // upewnij się, że wszystkie logi zostały zapisane
    LogManager.Shutdown();
}
