using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MyWallet.Services.Implementations;
using NUnit.Framework;

namespace MyWalletTests;

[TestFixture]
public class EmailServiceTests
{
    [Test]
    public void SendEmailWithAttachmentAsync_ThrowsOnConnectionFailure()
    {
        var opts = Options.Create(new EmailSettings
        {
            SmtpHost   = "not.existing.local",
            SmtpPort   = 2525,
            SenderEmail= "noreply@test.pl",
            SenderName = "Test"
        });

        var svc = new EmailService(opts);

        Assert.ThrowsAsync<SocketException>(async () =>
            await svc.SendEmailWithAttachmentAsync(
                "u@x.pl", "Sub", "Body", new byte[]{1}, "a.pdf"));
    }
}