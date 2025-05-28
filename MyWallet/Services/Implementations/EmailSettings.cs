namespace MyWallet.Services.Implementations;

public class EmailSettings
{
    public string SenderName { get; set; }
    public string SenderEmail { get; set; }
    public string SmtpHost { get; set; }
    public int SmtpPort { get; set; }
    public bool UseSsl { get; set; }
    public string SmtpUser { get; set; }
    public string SmtpPass { get; set; }
}
