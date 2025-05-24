namespace MyWallet.Settings;

public class ReportSettings
{
    /// <summary>Folder, w którym zapisywane będą PDF-y.</summary>
    public string OutputDirectory { get; set; } = "";

    /// <summary>Interwał w minutach (np. 1440 = raz dziennie).</summary>
    public int IntervalMinutes { get; set; }
}