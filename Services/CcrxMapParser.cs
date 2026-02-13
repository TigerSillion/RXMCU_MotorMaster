using System.IO;
using System.Text.RegularExpressions;

namespace MotorDebugStudio.Services;

public sealed record CcrxSymbol(string Name, uint Address, int SizeBytes);

public static partial class CcrxMapParser
{
    [GeneratedRegex("^\\s*_([A-Za-z][A-Za-z0-9_.$@?]*)\\s*$")]
    private static partial Regex SymbolOnlyRegex();

    [GeneratedRegex("^\\s*([0-9A-Fa-f]{6,8})\\s+(\\d+)\\s+data\\b")]
    private static partial Regex AddressSizeRegex();

    [GeneratedRegex("^\\s*([0-9A-Fa-f]{6,8})\\s+")]
    private static partial Regex AddressOnlyRegex();

    public static IReadOnlyList<CcrxSymbol> Parse(string mapPath)
    {
        if (string.IsNullOrWhiteSpace(mapPath) || !File.Exists(mapPath))
        {
            return [];
        }

        var lines = File.ReadAllLines(mapPath);
        var dict = new Dictionary<string, CcrxSymbol>(StringComparer.Ordinal);

        for (var i = 0; i < lines.Length; i++)
        {
            var symbolMatch = SymbolOnlyRegex().Match(lines[i]);
            if (!symbolMatch.Success)
            {
                continue;
            }

            var name = symbolMatch.Groups[1].Value;
            if (!IsUsefulSymbol(name))
            {
                continue;
            }

            var found = false;
            for (var look = i + 1; look <= i + 3 && look < lines.Length; look++)
            {
                var addrSize = AddressSizeRegex().Match(lines[look]);
                if (addrSize.Success)
                {
                    if (TryParseHex(addrSize.Groups[1].Value, out var addr) && int.TryParse(addrSize.Groups[2].Value, out var size))
                    {
                        Upsert(dict, new CcrxSymbol(name, addr, size));
                        found = true;
                        break;
                    }
                }

                var addrOnly = AddressOnlyRegex().Match(lines[look]);
                if (addrOnly.Success && TryParseHex(addrOnly.Groups[1].Value, out var addr2))
                {
                    Upsert(dict, new CcrxSymbol(name, addr2, 0));
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                // Keep symbol name with unknown address out of final set.
            }
        }

        return dict.Values
            .OrderBy(static x => x.Address)
            .ThenBy(static x => x.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static void Upsert(Dictionary<string, CcrxSymbol> dict, CcrxSymbol value)
    {
        if (!dict.TryGetValue(value.Name, out var old))
        {
            dict[value.Name] = value;
            return;
        }

        // Prefer entries that have explicit size info.
        if (old.SizeBytes <= 0 && value.SizeBytes > 0)
        {
            dict[value.Name] = value;
            return;
        }

        // Keep first explicit sized entry; otherwise update address-only by latest.
        if (old.SizeBytes <= 0 && value.SizeBytes <= 0)
        {
            dict[value.Name] = value;
        }
    }

    private static bool IsUsefulSymbol(string name)
    {
        if (name.Length < 2)
        {
            return false;
        }

        if (name.StartsWith("__", StringComparison.Ordinal))
        {
            return false;
        }

        if (name.StartsWith("_", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    private static bool TryParseHex(string raw, out uint value)
    {
        return uint.TryParse(raw, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out value);
    }
}
