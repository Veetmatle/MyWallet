namespace MyWallet.DTOs;

public class DeletePortfoliosRequest
{
    public List<int> PortfolioIds { get; set; } = new();
    public string Password { get; set; } = "";
}
