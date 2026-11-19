using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LaraLauncher.App.ViewModels;
using LaraLauncher.Core.Models;

namespace LaraLauncher.App.Views;

public partial class SettingsView : UserControl
{
    public event EventHandler? ExitToSidebarRequested;

    public SettingsView()
    {
        InitializeComponent();
    }

    private SettingsViewModel? ViewModel => DataContext as SettingsViewModel;

    private Button[] SettingRows =>
    [
        StartupToggleButton,
        FullscreenToggleButton,
        GamepadToggleButton,
        TempUnitButton,
        GridColumnsButton,
        SoundsToggleButton
    ];

    private Button[] PowerButtons =>
    [
        PowerExitButton,
        PowerSleepButton,
        PowerRestartButton,
        PowerShutdownButton
    ];

    public bool FocusPrimaryControl() => StartupToggleButton.Focus();

    public bool HandleNavigationAction(NavigationAction action)
    {
        if (ViewModel is null)
        {
            return false;
        }

        var rows = SettingRows;
        int rowIndex = Array.FindIndex(rows, b => b.IsKeyboardFocused);
        if (rowIndex >= 0)
        {
            switch (action)
            {
                case NavigationAction.Up:
                    if (rowIndex > 0) rows[rowIndex - 1].Focus();
                    return true;
                case NavigationAction.Down:
                    if (rowIndex + 1 < rows.Length)
                    {
                        rows[rowIndex + 1].Focus();
                    }
                    else
                    {
                        PowerExitButton.Focus();
                    }
                    return true;
                case NavigationAction.Left:
                case NavigationAction.Back:
                    ExitToSidebarRequested?.Invoke(this, EventArgs.Empty);
                    return true;
                case NavigationAction.Select:
                    TriggerSettingRow(rowIndex);
                    return true;
            }
        }

        var power = PowerButtons;
        int powerIndex = Array.FindIndex(power, b => b.IsKeyboardFocused);
        if (powerIndex >= 0)
        {
            switch (action)
            {
                case NavigationAction.Up:
                    SoundsToggleButton.Focus();
                    return true;
                case NavigationAction.Left:
                    if (powerIndex == 0)
                    {
                        ExitToSidebarRequested?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        power[powerIndex - 1].Focus();
                    }
                    return true;
                case NavigationAction.Right:
                    if (powerIndex + 1 < power.Length)
                    {
                        power[powerIndex + 1].Focus();
                    }
                    return true;
                case NavigationAction.Back:
                    ExitToSidebarRequested?.Invoke(this, EventArgs.Empty);
                    return true;
                case NavigationAction.Select:
                    TriggerPowerButton(powerIndex);
                    return true;
            }
        }

        return false;
    }

    private void TriggerSettingRow(int rowIndex)
    {
        if (ViewModel is null) return;

        switch (rowIndex)
        {
            case 0: ViewModel.ToggleStartWithWindows(); break;
            case 1: ViewModel.ToggleStartFullscreen(); break;
            case 2: ViewModel.ToggleEnableGamepad(); break;
            case 3: ViewModel.ToggleTemperatureUnit(); break;
            case 4: ViewModel.CycleGridColumns(); break;
            case 5: ViewModel.ToggleNavigationSounds(); break;
        }
    }

    private void TriggerPowerButton(int powerIndex)
    {
        if (ViewModel is null) return;

        var action = powerIndex switch
        {
            0 => PowerActionKind.ExitToDesktop,
            1 => PowerActionKind.Sleep,
            2 => PowerActionKind.Restart,
            _ => PowerActionKind.Shutdown
        };

        ViewModel.RequestPowerAction(action);
    }

    private void RowControl_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            element.BringIntoView();
        }
    }

    private void StartupToggleButton_Click(object sender, RoutedEventArgs e) => ViewModel?.ToggleStartWithWindows();
    private void FullscreenToggleButton_Click(object sender, RoutedEventArgs e) => ViewModel?.ToggleStartFullscreen();
    private void GamepadToggleButton_Click(object sender, RoutedEventArgs e) => ViewModel?.ToggleEnableGamepad();
    private void TempUnitButton_Click(object sender, RoutedEventArgs e) => ViewModel?.ToggleTemperatureUnit();
    private void GridColumnsButton_Click(object sender, RoutedEventArgs e) => ViewModel?.CycleGridColumns();
    private void SoundsToggleButton_Click(object sender, RoutedEventArgs e) => ViewModel?.ToggleNavigationSounds();

    private void PowerExitButton_Click(object sender, RoutedEventArgs e) => ViewModel?.RequestPowerAction(PowerActionKind.ExitToDesktop);
    private void PowerSleepButton_Click(object sender, RoutedEventArgs e) => ViewModel?.RequestPowerAction(PowerActionKind.Sleep);
    private void PowerRestartButton_Click(object sender, RoutedEventArgs e) => ViewModel?.RequestPowerAction(PowerActionKind.Restart);
    private void PowerShutdownButton_Click(object sender, RoutedEventArgs e) => ViewModel?.RequestPowerAction(PowerActionKind.Shutdown);
}
