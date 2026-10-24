using System.Globalization;
using System.Text;
using TodoApi.Domain.Exceptions;

namespace TodoApi.Domain.Common;

public static class NameComparison
{
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    public static string NormalizeForLookup(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            return value.Trim()
                .Normalize(NormalizationForm.FormC)
                .ToUpper(TurkishCulture)
                .Normalize(NormalizationForm.FormC);
        }
        catch (ArgumentException)
        {
            throw new DomainRuleViolationException("A name must contain valid Unicode characters.");
        }
    }

    public static bool AreSame(string first, string second)
    {
        return string.Equals(NormalizeForLookup(first), NormalizeForLookup(second), StringComparison.Ordinal);
    }
}
