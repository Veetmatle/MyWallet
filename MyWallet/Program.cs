// Program.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyWallet.Data;
using MyWallet.Services;
using MyWallet.Services.Implementations;
using MyWallet.Mappers;
using MyWallet.Settings;                           // <- przestrzeń nazw dla ReportSettings, EmailSettings

var builder = WebApplication.CreateBuilder(args);

// 1️⃣ Konfiguracja IOptions dla ReportSettings i EmailSettings
builder.Services.Configure<ReportSettings>(builder.Configuration.GetSection("ReportSettings"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IOrderService, OrderService>();
// 2️⃣ DbContext PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3️⃣ Rejestracja własnych serwisów
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IExternalApiService, ExternalApiService>();
builder.Services.AddHttpClient();

// 4️⃣ Rejestracja mapperów
builder.Services.AddScoped<UserMapper>();
builder.Services.AddScoped<PortfolioMapper>();
builder.Services.AddScoped<AssetMapper>();
builder.Services.AddScoped<TransactionMapper>();

// 5️⃣ Rejestracja BackgroundService
builder.Services.AddHostedService<OpenOrderReportBackgroundService>();

// 6️⃣ CORS dla frontu
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// 7️⃣ Kontrolery
builder.Services.AddControllers();

var app = builder.Build();

// ─── Middleware ───────────────────────────────────
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthorization();

// ─── Endpointy ────────────────────────────────────
app.MapControllers();
app.MapGet("/", () => "API działa!");

// ─── Start ────────────────────────────────────────
app.Run();
