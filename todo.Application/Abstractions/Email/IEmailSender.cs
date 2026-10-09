using System.Threading;
using System.Threading.Tasks;

namespace TodoApi.Application.Abstractions.Email;

public interface IEmailSender
{
    bool IsConfigured { get; }

    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
