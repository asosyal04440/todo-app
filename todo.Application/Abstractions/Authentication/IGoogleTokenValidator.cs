using System.Threading;
using System.Threading.Tasks;

namespace TodoApi.Application.Abstractions.Authentication;

public interface IGoogleTokenValidator
{
    Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken);
}
