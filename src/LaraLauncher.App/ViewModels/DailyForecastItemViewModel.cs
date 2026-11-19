using System.Globalization;
using LaraLauncher.Core.Models;
using LaraLauncher.Core.Services;

namespace LaraLauncher.App.ViewModels;

public sealed class DailyForecastItemViewModel
{
    public string DayLabel { get; }
    public string DateLabel { get; }
    public string Icon { get; }
    public string ConditionText { get; }
    public string MaxTempText { get; }
    public string MinTempText { get; }
    public string RainChanceText { get; }

    public DailyForecastItemViewModel(DailyForecast day, TemperatureUnit unit, DateOnly today)
    {
        var culture = CultureInfo.CurrentCulture;

        if (day.Date == today)
        {
            DayLabel = "Hoy";
        }
        else if (day.Date == today.AddDays(1))
        {
            DayLabel = "Mañana";
        }
        else
        {
            DayLabel = culture.TextInfo.ToTitleCase(day.Date.ToString("ddd", culture));
        }

        DateLabel = day.Date.ToString("dd MMM", culture);
        Icon = day.ConditionIcon;
        ConditionText = day.ConditionText;
        MaxTempText = TemperatureFormatter.FormatShort(day.MaxTemperatureC, unit);
        MinTempText = TemperatureFormatter.FormatShort(day.MinTemperatureC, unit);
        RainChanceText = $"💧 {day.PrecipitationProbabilityPercent}%";
    }
}
