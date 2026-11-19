using System.Globalization;

namespace LaraLauncher.Core.Services;

public readonly record struct WebAppUrlResult(
    bool IsValid,
    string NormalizedUrl,
    string Host,
    string SuggestedName,
    string? ErrorMessage);

/// <summary>
/// Pure URL normalizer and friendly display-name generator for user-defined WebApp tiles.
/// </summary>
public static class WebAppUrlNormalizer
{
    private static readonly string[] SubdomainPrefixesToStrip = ["www.", "m.", "app.", "tv.", "play."];

    public static WebAppUrlResult Normalize(string? rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return new WebAppUrlResult(false, string.Empty, string.Empty, string.Empty, "Ingresa la dirección de un sitio web (ej. youtube.com).");
        }

        string trimmed = rawInput.Trim();
        if (trimmed.Contains(' '))
        {
            return new WebAppUrlResult(false, string.Empty, string.Empty, string.Empty, "La dirección web no puede contener espacios.");
        }

        string candidate = trimmed.Contains("://", StringComparison.Ordinal)
            ? trimmed
            : $"https://{trimmed}";

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
        {
            return new WebAppUrlResult(false, string.Empty, string.Empty, string.Empty, "La dirección ingresada no tiene un formato de URL válido.");
        }

        if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return new WebAppUrlResult(false, string.Empty, string.Empty, string.Empty, "Solo se permiten sitios web http:// o https://.");
        }

        string host = uri.Host;
        bool isValidHost = !string.IsNullOrWhiteSpace(host) &&
                           (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                            (host.Contains('.') && !host.StartsWith('.') && !host.EndsWith('.')));

        if (!isValidHost)
        {
            return new WebAppUrlResult(false, string.Empty, string.Empty, string.Empty, "Ingresa un dominio válido (por ejemplo: netflix.com o twitch.tv).");
        }

        string normalizedUrl = uri.AbsoluteUri.TrimEnd('/');
        string suggestedName = SuggestDisplayName(host);

        return new WebAppUrlResult(true, normalizedUrl, host, suggestedName, null);
    }

    public static string SuggestDisplayName(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return "WebApp";
        }

        string cleaned = host.Trim().ToLowerInvariant();
        foreach (string prefix in SubdomainPrefixesToStrip)
        {
            if (cleaned.StartsWith(prefix, StringComparison.Ordinal) && cleaned.Length > prefix.Length)
            {
                cleaned = cleaned[prefix.Length..];
                break;
            }
        }

        string primarySegment = cleaned.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? cleaned;
        if (string.IsNullOrWhiteSpace(primarySegment))
        {
            return "WebApp";
        }

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(primarySegment);
    }
}
