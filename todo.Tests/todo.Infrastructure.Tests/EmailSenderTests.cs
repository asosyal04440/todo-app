using Microsoft.Extensions.Options;
using TodoApi.Infrastructure.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TodoApi.Application.Abstractions.Email;
using TodoApi.Infrastructure.Services.Email;

namespace todo.Infrastructure.Tests;

public class EmailSenderTests
{
    [Fact]
    public async Task MailKitEmailSender_DeliversTheMessageToTheSmtpServer()
    {
        using FakeSmtpServer smtpServer = FakeSmtpServer.Start();
        SmtpSettings settings = new SmtpSettings(
            "127.0.0.1",
            smtpServer.Port,
            SmtpSecurity.None,
            username: null,
            password: null,
            fromAddress: "todo@example.com",
            fromName: "Todo");
        MailKitEmailSender sender = new MailKitEmailSender(Options.Create(new SmtpOptions
        { Host = settings.Host, Port = settings.Port, Security = settings.Security, FromAddress = settings.FromAddress, FromName = settings.FromName }));
        EmailMessage message = new EmailMessage(
            "ali@example.com",
            "Ali",
            "Hatırlatma",
            "Düz metin içerik",
            "<p>HTML içerik</p>");

        await sender.SendAsync(message, TestContext.Current.CancellationToken);
        string received = await smtpServer.WaitForMessageAsync(TestContext.Current.CancellationToken);

        Assert.Contains("MAIL FROM:<todo@example.com>", smtpServer.Commands);
        Assert.Contains("RCPT TO:<ali@example.com>", smtpServer.Commands);
        Assert.Contains("multipart/alternative", received);
        Assert.Contains("text/plain", received);
        Assert.Contains("text/html", received);
    }

    [Fact]
    public void SmtpSettings_RefuseAnUnencryptedConnectionToARemoteServer()
    {
        Assert.Throws<InvalidOperationException>(
            () => new SmtpSettings("smtp.gmail.com", 25, SmtpSecurity.None, "ali@gmail.com", "x", null, null));
    }

    [Fact]
    public void SmtpSettings_AreNotConfiguredWithoutAPassword()
    {
        SmtpSettings settings = new SmtpSettings(
            "smtp.gmail.com",
            587,
            SmtpSecurity.StartTls,
            "ali@gmail.com",
            password: null,
            fromAddress: null,
            fromName: null);

        Assert.False(settings.IsConfigured);
        Assert.Equal("ali@gmail.com", settings.FromAddress);
    }

    [Fact]
    public void SmtpSettings_AreConfiguredWithAUsernameAndPassword()
    {
        SmtpSettings settings = new SmtpSettings(
            "smtp.gmail.com",
            587,
            SmtpSecurity.StartTls,
            "ali@gmail.com",
            "uygulama-sifresi",
            fromAddress: null,
            fromName: null);

        Assert.True(settings.IsConfigured);
    }

    private sealed class FakeSmtpServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly Task<string> _session;

        private FakeSmtpServer()
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            _session = Task.Run(RunSessionAsync);
        }

        public int Port
        {
            get
            {
                return ((IPEndPoint)_listener.LocalEndpoint).Port;
            }
        }

        public List<string> Commands { get; } = new List<string>();

        public static FakeSmtpServer Start()
        {
            return new FakeSmtpServer();
        }

        public async Task<string> WaitForMessageAsync(CancellationToken cancellationToken)
        {
            return await _session.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        }

        public void Dispose()
        {
            _listener.Stop();
        }

        private async Task<string> RunSessionAsync()
        {
            using TcpClient client = await _listener.AcceptTcpClientAsync();
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new StreamReader(stream, Encoding.ASCII);
            using StreamWriter writer = new StreamWriter(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };

            StringBuilder data = new StringBuilder();
            await writer.WriteLineAsync("220 localhost test smtp");

            while (true)
            {
                string? line = await reader.ReadLineAsync();

                if (line == null)
                {
                    break;
                }

                Commands.Add(line);
                string command = line.ToUpperInvariant();

                if (command.StartsWith("EHLO") || command.StartsWith("HELO"))
                {
                    await writer.WriteLineAsync("250 localhost");
                }
                else if (command.StartsWith("DATA"))
                {
                    await writer.WriteLineAsync("354 end with <CRLF>.<CRLF>");

                    while (true)
                    {
                        string? dataLine = await reader.ReadLineAsync();

                        if (dataLine == null || dataLine == ".")
                        {
                            break;
                        }

                        data.AppendLine(dataLine);
                    }

                    await writer.WriteLineAsync("250 queued");
                }
                else if (command.StartsWith("QUIT"))
                {
                    await writer.WriteLineAsync("221 bye");
                    break;
                }
                else
                {
                    await writer.WriteLineAsync("250 ok");
                }
            }

            return data.ToString();
        }
    }
}
