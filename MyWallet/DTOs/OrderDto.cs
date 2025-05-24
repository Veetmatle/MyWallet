namespace MyWallet.DTOs
{
    public class OrderDto
    {
        public int      Id          { get; set; }
        public DateTime Date        { get; set; }
        public string   Description { get; set; } = string.Empty;
    }
}