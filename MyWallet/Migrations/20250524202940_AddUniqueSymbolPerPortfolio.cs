using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyWallet.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueSymbolPerPortfolio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder m)
        {
            m.CreateIndex(
                name: "IX_Assets_Portfolio_Symbol_Category",
                table: "Assets",
                columns: new[] { "PortfolioId", "Symbol", "Category" },
                unique: true);
        }
        /// <inheritdoc />
        protected override void Down(MigrationBuilder m)
        {
            m.DropIndex(
                name: "IX_Assets_Portfolio_Symbol_Category",
                table: "Assets");
        }
    }
}
