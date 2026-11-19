using CommunityToolkit.Mvvm.ComponentModel;
using LaraLauncher.Core.Models;

namespace LaraLauncher.App.ViewModels;

public sealed partial class SidebarItemViewModel : ObservableObject
{
    public SidebarSection Section { get; }
    public string Title { get; }
    public string Glyph { get; }

    [ObservableProperty]
    private bool _isSelected;

    public SidebarItemViewModel(SidebarSection section, string title, string glyph, bool isSelected = false)
    {
        Section = section;
        Title = title;
        Glyph = glyph;
        _isSelected = isSelected;
    }
}
