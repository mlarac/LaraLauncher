namespace LaraLauncher.Core.Models;

/// <summary>
/// Geographic location selected by the user for weather forecasts.
/// </summary>
public sealed class GeoLocation
{
    public string Name { get; set; } = string.Empty;
    public string Admin1 { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public string DisplayName
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Name)) parts.Add(Name.Trim());
            if (!string.IsNullOrWhiteSpace(Admin1) && !Admin1.Equals(Name, StringComparison.OrdinalIgnoreCase)) parts.Add(Admin1.Trim());
            if (!string.IsNullOrWhiteSpace(Country)) parts.Add(Country.Trim());
            return string.Join(", ", parts);
        }
    }
}
