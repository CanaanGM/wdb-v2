using System.Globalization;

namespace Api.Application.Text;

public static class StorageTextNormalizer
{
    public static string NormalizeKey(string value)
    {
        return value.Trim().ToLower(CultureInfo.InvariantCulture);
    }

    public static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
