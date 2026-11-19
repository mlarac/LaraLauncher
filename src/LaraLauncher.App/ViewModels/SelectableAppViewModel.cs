using CommunityToolkit.Mvvm.ComponentModel;
using LaraLauncher.Core.Models;

namespace LaraLauncher.App.ViewModels;

public sealed partial class SelectableAppViewModel : ObservableObject
{
    public DiscoveredApp App { get; }

    public string Name => App.Name;
    public string Category => App.Category;
    public string ExecutablePath => App.ExecutablePath;
    public string? IconPath => App.IconPath;
    public string AccentHex => App.SuggestedAccentHex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActionBadgeText))]
    private bool _isSelected;

    public string ActionBadgeText => IsSelected ? "✓ En Inicio" : "+ Agregar";

    public SelectableAppViewModel(DiscoveredApp app, bool isSelected)
    {
        App = app;
        _isSelected = isSelected;
    }
}
