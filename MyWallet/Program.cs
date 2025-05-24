using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyWallet.Data;
using MyWallet.Services;
using MyWallet.Services.Implementations;
using MyWallet.Mappers;

var builder = WebApplication.CreateBuilder(args);

// 📦 Połączenie z bazą danych PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🔧 Rejestracja serwisów (Dependency Injection)
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IExternalApiService, ExternalApiService>();
builder.Services.AddHttpClient();

// Cache?
builder.Services.AddMemoryCache();            
builder.Services.AddHttpClient();   


// ✨ Dodajemy politykę CORS, aby front na localhost:3000 mógł dzwonić do API
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

// Rejestracja mapperów (Mapperly)
builder.Services.AddScoped<UserMapper>();
builder.Services.AddScoped<PortfolioMapper>();
builder.Services.AddScoped<AssetMapper>();
builder.Services.AddScoped<TransactionMapper>();

// 🌐 Obsługa kontrolerów
builder.Services.AddControllers();

var app = builder.Build();

// 🔒 Routing i middleware
app.UseHttpsRedirection();

// ⬇️ Włączamy CORS przed autoryzacją i mapowaniem kontrolerów
app.UseCors("AllowFrontend");

app.UseAuthorization(); // JWT w przyszłości

// 🌍 Mapowanie endpointów z kontrolerów
app.MapControllers();

app.MapGet("/", () => "API działa!");

// 🚀 Start aplikacji
app.Run();