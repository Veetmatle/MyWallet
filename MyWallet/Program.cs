using Microsoft.EntityFrameworkCore;
using MyWallet.Data;
using MyWallet.Services;
using MyWallet.Services.Implementations;

// W metodzie ConfigureServices w Startup.cs lub bezpośrednio w Program.cs (dla .NET 6+)
var builder = WebApplication.CreateBuilder(args);

// Dodaj konfigurację DbContext dla PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Rejestracja serwisów
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IExternalApiService, ExternalApiService>();