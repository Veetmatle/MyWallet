using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace MyWallet.Migrations
{
    public partial class AddInitialApiConfigs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Dodanie Coin Gecko API dla kryptowalut
            migrationBuilder.InsertData(
                table: "ExternalApiConfigs",
                columns: new[] { "Id", "Name", "Description", "ApiKey", "SecretKey", "BaseUrl", "CreatedAt", "IsActive" },
                values: new object[] { 1, "cryptocurrency", "CoinGecko API", "", "", "https://api.coingecko.com/api/v3", DateTime.UtcNow, true }
            );

            // Dodanie Alpha Vantage API dla akcji
            migrationBuilder.InsertData(
                table: "ExternalApiConfigs",
                columns: new[] { "Id", "Name", "Description", "ApiKey", "SecretKey", "BaseUrl", "CreatedAt", "IsActive" },
                values: new object[] { 2, "stock", "Alpha Vantage API", "Q9V35X6S0YRVCIZY", "", "https://www.alphavantage.co/query", DateTime.UtcNow, true }
            );

            // Dodanie Alpha Vantage API również dla ETF-ów (wykorzystujemy to samo API)
            migrationBuilder.InsertData(
                table: "ExternalApiConfigs",
                columns: new[] { "Id", "Name", "Description", "ApiKey", "SecretKey", "BaseUrl", "CreatedAt", "IsActive" },
                values: new object[] { 3, "etf", "Alpha Vantage API", "Q9V35X6S0YRVCIZY", "", "https://www.alphavantage.co/query", DateTime.UtcNow, true }
            );
            
            migrationBuilder.InsertData(
                table: "ExternalApiConfigs",
                columns: new[] { "Id", "Name", "Description", "ApiKey", "SecretKey", "BaseUrl", "CreatedAt", "IsActive" },
                values: new object[] { 4, "commodity", "Alpha Vantage API for commodities", "TWÓJ_KLUCZ_API", "", "https://www.alphavantage.co/query", DateTime.UtcNow, true }
            );

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ExternalApiConfigs",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ExternalApiConfigs",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ExternalApiConfigs",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}