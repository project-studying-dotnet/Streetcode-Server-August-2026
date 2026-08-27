using Streetcode.DAL.Enums;

namespace Streetcode.BLL.MediatR.Streetcode.Streetcode.Filters;

public static class StreetcodeFilterParser
{
    private const string StatusField = "status";

    public static bool TryParse(
        string? filter,
        out StreetcodeStatus status)
    {
        status = default;

        if (string.IsNullOrWhiteSpace(filter))
        {
            return false;
        }

        string[] parts = filter.Split(':');

        if (parts.Length != 2 ||
            !string.Equals(
                parts[0].Trim(),
                StatusField,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string statusValue = parts[1].Trim();

        if (!Enum.GetNames<StreetcodeStatus>().Any(name =>
                string.Equals(
                    name,
                    statusValue,
                    StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return Enum.TryParse(
            statusValue,
            ignoreCase: true,
            out status);
    }
}
