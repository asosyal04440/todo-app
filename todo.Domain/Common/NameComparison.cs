using System.Globalization;

namespace TodoApi.Domain.Common;

public static class NameComparison
{
    private static readonly CompareInfo TurkishCompareInfo = CultureInfo.GetCultureInfo("tr-TR").CompareInfo;

    public static bool AreSame(string first, string second)
    {
        return TurkishCompareInfo.Compare(first.Trim(), second.Trim(), CompareOptions.IgnoreCase) == 0;
    }
}
