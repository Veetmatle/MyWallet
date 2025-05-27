namespace MyWallet.DTOs;

public class PortfolioProfitLossDto
{
    public decimal TotalInvested { get; set; }
    public decimal CurrentValue  { get; set; }
    public decimal ProfitLoss    { get; set; }
    public decimal ProfitLossPercentage { get; set; }
    public bool    IsProfit      { get; set; }
}

public class PortfolioProfitLossBreakdownDto
{
    public IEnumerable<AssetBreakdownDto> Assets { get; set; }
    public BreakdownSummaryDto Summary { get; set; }
}

public class AssetBreakdownDto
{
    public string  Symbol            { get; set; } = "";
    public decimal Quantity          { get; set; }
    public decimal AverageBuyPrice   { get; set; }
    public decimal CurrentPrice      { get; set; }
    public decimal CurrentValue      { get; set; }
    public decimal ProfitLoss        { get; set; }
    public decimal ProfitLossPercent { get; set; }
    public bool    IsProfit          { get; set; }
}

public class BreakdownSummaryDto
{
    public int     TotalAssets       { get; set; }
    public int     ProfitableAssets  { get; set; }
    public int     LosingAssets      { get; set; }
    public decimal TotalInvested     { get; set; }
    public decimal CurrentValue      { get; set; }
    public decimal ProfitLoss        { get; set; }
    public decimal ProfitLossPercent { get; set; }
}
