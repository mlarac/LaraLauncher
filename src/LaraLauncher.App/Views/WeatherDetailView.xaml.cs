using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LaraLauncher.App.ViewModels;
using LaraLauncher.Core.Models;

namespace LaraLauncher.App.Views;

public partial class WeatherDetailView : UserControl
{
    private int _focusedResultIndex;

    public event EventHandler? ExitToSidebarRequested;

    public WeatherDetailView()
    {
        InitializeComponent();
    }

    private WeatherViewModel? ViewModel => DataContext as WeatherViewModel;

    public bool FocusPrimaryControl()
    {
        if (ViewModel is null)
        {
            return false;
        }

        if (ViewModel.IsCityPickerVisible)
        {
            return CitySearchBox.Focus();
        }

        return RefreshWeatherButton.Focus();
    }

    public async Task<bool> HandleNavigationActionAsync(NavigationAction action)
    {
        if (ViewModel is null)
        {
            return false;
        }

        if (CitySearchBox.IsKeyboardFocused)
        {
            if (action == NavigationAction.Back)
            {
                return RaiseExitToSidebar();
            }

            if (action == NavigationAction.Down && ViewModel.HasCitySearchResults)
            {
                return FocusResultAt(0);
            }

            return false;
        }

        if (SearchCityButton.IsKeyboardFocused)
        {
            switch (action)
            {
                case NavigationAction.Left:
                    return CitySearchBox.Focus();
                case NavigationAction.Right when ViewModel.HasForecast:
                    return CloseCityPickerButton.Focus();
                case NavigationAction.Down when ViewModel.HasCitySearchResults:
                    return FocusResultAt(0);
                case NavigationAction.Back:
                    return RaiseExitToSidebar();
                case NavigationAction.Select:
                    await SearchAndFocusResultsAsync();
                    return true;
            }
        }

        if (CloseCityPickerButton.IsKeyboardFocused)
        {
            switch (action)
            {
                case NavigationAction.Left:
                    return SearchCityButton.Focus();
                case NavigationAction.Back:
                case NavigationAction.Select:
                    ViewModel.ToggleCityPicker(false);
                    RefreshWeatherButton.Focus();
                    return true;
            }
        }

        if (RefreshWeatherButton.IsKeyboardFocused)
        {
            switch (action)
            {
                case NavigationAction.Left:
                case NavigationAction.Back:
                    return RaiseExitToSidebar();
                case NavigationAction.Down:
                    return ChangeCityButton.Focus();
                case NavigationAction.Select:
                    await ViewModel.LoadForecastAsync(forceRefresh: true);
                    return true;
            }
        }

        if (ChangeCityButton.IsKeyboardFocused)
        {
            switch (action)
            {
                case NavigationAction.Left:
                case NavigationAction.Back:
                    return RaiseExitToSidebar();
                case NavigationAction.Up:
                    return RefreshWeatherButton.Focus();
                case NavigationAction.Select:
                    ViewModel.ToggleCityPicker(true);
                    await Dispatcher.InvokeAsync(() => CitySearchBox.Focus());
                    return true;
            }
        }

        // City search result list navigation
        if (ViewModel.HasCitySearchResults)
        {
            switch (action)
            {
                case NavigationAction.Up:
                    return _focusedResultIndex <= 0
                        ? SearchCityButton.Focus()
                        : FocusResultAt(_focusedResultIndex - 1);

                case NavigationAction.Down:
                    if (_focusedResultIndex + 1 < ViewModel.CitySearchResults.Count)
                    {
                        return FocusResultAt(_focusedResultIndex + 1);
                    }
                    return true;

                case NavigationAction.Left:
                case NavigationAction.Back:
                    return RaiseExitToSidebar();

                case NavigationAction.Select:
                    if (_focusedResultIndex >= 0 && _focusedResultIndex < ViewModel.CitySearchResults.Count)
                    {
                        await SelectLocationAndFocusHeroAsync(ViewModel.CitySearchResults[_focusedResultIndex]);
                        return true;
                    }
                    break;
            }
        }

        if (action is NavigationAction.Left or NavigationAction.Back)
        {
            return RaiseExitToSidebar();
        }

        return false;
    }

    private async Task SearchAndFocusResultsAsync()
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.SearchCitiesAsync();
        if (ViewModel.HasCitySearchResults)
        {
            await Dispatcher.InvokeAsync(() => FocusResultAt(0));
        }
    }

    private async Task SelectLocationAndFocusHeroAsync(GeoLocation location)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.SelectLocationAsync(location);
        await Dispatcher.InvokeAsync(() => RefreshWeatherButton.Focus());
    }

    private bool RaiseExitToSidebar()
    {
        ExitToSidebarRequested?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private async void CitySearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await SearchAndFocusResultsAsync();
        }
    }

    private async void SearchCityButton_Click(object sender, RoutedEventArgs e)
        => await SearchAndFocusResultsAsync();

    private void CloseCityPickerButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.ToggleCityPicker(false);
        RefreshWeatherButton.Focus();
    }

    private async void RefreshWeatherButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
        {
            await ViewModel.LoadForecastAsync(forceRefresh: true);
        }
    }

    private void ChangeCityButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.ToggleCityPicker(true);
        Dispatcher.BeginInvoke(() => CitySearchBox.Focus());
    }

    private async void CityResultButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: GeoLocation loc })
        {
            await SelectLocationAndFocusHeroAsync(loc);
        }
    }

    private void CityResultButton_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is Button { DataContext: GeoLocation loc } button && ViewModel is not null)
        {
            int idx = ViewModel.CitySearchResults.IndexOf(loc);
            if (idx >= 0)
            {
                _focusedResultIndex = idx;
            }

            button.BringIntoView();
        }
    }

    private bool FocusResultAt(int index)
    {
        if (ViewModel is null || ViewModel.CitySearchResults.Count == 0)
        {
            return false;
        }

        int clamped = Math.Clamp(index, 0, ViewModel.CitySearchResults.Count - 1);
        _focusedResultIndex = clamped;
        CityResultsList.UpdateLayout();

        if (CityResultsList.ItemContainerGenerator.ContainerFromIndex(clamped) is DependencyObject container)
        {
            var button = FindVisualChild<Button>(container);
            if (button is not null)
            {
                button.BringIntoView();
                return button.Focus();
            }
        }

        return false;
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T target)
            {
                return target;
            }

            var nested = FindVisualChild<T>(child);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }
}
