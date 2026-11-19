using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LaraLauncher.App.ViewModels;
using LaraLauncher.Core.Models;
using Microsoft.Win32;

namespace LaraLauncher.App.Views;

public partial class AppManagerView : UserControl
{
    private int _focusedRowIndex;

    public event EventHandler<SelectableAppViewModel>? AppToggled;
    public event EventHandler<string>? ManualFileSelected;
    public event EventHandler? RefreshRequested;
    public event EventHandler? ExitToSidebarRequested;

    public AppManagerView()
    {
        InitializeComponent();
    }

    private AppManagerViewModel? ViewModel => DataContext as AppManagerViewModel;

    public bool FocusPrimaryControl()
    {
        if (ViewModel?.FilteredApps.Count > 0 && FocusRowAt(Math.Min(_focusedRowIndex, ViewModel.FilteredApps.Count - 1)))
        {
            return true;
        }

        return AddManualButton.Focus();
    }

    public bool HandleNavigationAction(NavigationAction action)
    {
        if (ViewModel is null)
        {
            return false;
        }

        // Toolbar focus handling
        if (SearchBox.IsKeyboardFocused)
        {
            if (action == NavigationAction.Down)
            {
                return FocusRowAt(0);
            }

            if (action == NavigationAction.Back)
            {
                ExitToSidebarRequested?.Invoke(this, EventArgs.Empty);
                return true;
            }

            return false;
        }

        if (AddManualButton.IsKeyboardFocused)
        {
            return action switch
            {
                NavigationAction.Left => SearchBox.Focus(),
                NavigationAction.Right => RefreshButton.Focus(),
                NavigationAction.Down => FocusRowAt(0),
                NavigationAction.Back => RaiseExitToSidebar(),
                NavigationAction.Select => OpenManualFilePicker(),
                _ => false
            };
        }

        if (RefreshButton.IsKeyboardFocused)
        {
            return action switch
            {
                NavigationAction.Left => AddManualButton.Focus(),
                NavigationAction.Down => FocusRowAt(0),
                NavigationAction.Back => RaiseExitToSidebar(),
                NavigationAction.Select => RaiseRefresh(),
                _ => false
            };
        }

        // List row focus handling
        int count = ViewModel.FilteredApps.Count;
        switch (action)
        {
            case NavigationAction.Up:
                if (_focusedRowIndex <= 0)
                {
                    return AddManualButton.Focus();
                }

                return FocusRowAt(_focusedRowIndex - 1);

            case NavigationAction.Down:
                if (_focusedRowIndex + 1 < count)
                {
                    return FocusRowAt(_focusedRowIndex + 1);
                }

                return true;

            case NavigationAction.Left:
            case NavigationAction.Back:
                return RaiseExitToSidebar();

            case NavigationAction.Select:
                if (_focusedRowIndex >= 0 && _focusedRowIndex < count)
                {
                    AppToggled?.Invoke(this, ViewModel.FilteredApps[_focusedRowIndex]);
                    return true;
                }

                return false;

            default:
                return false;
        }
    }

    private bool RaiseExitToSidebar()
    {
        ExitToSidebarRequested?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private bool RaiseRefresh()
    {
        RefreshRequested?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private bool OpenManualFilePicker()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar aplicación o acceso directo",
            Filter = "Aplicaciones y Accesos Directos (*.exe;*.lnk)|*.exe;*.lnk|Todos los archivos (*.*)|*.*",
            CheckFileExists = true,
            DereferenceLinks = false
        };

        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FileName))
        {
            ManualFileSelected?.Invoke(this, dialog.FileName);
        }

        return true;
    }

    private void AddManualButton_Click(object sender, RoutedEventArgs e)
        => OpenManualFilePicker();

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
        => RaiseRefresh();

    private void AppRowButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: SelectableAppViewModel item })
        {
            AppToggled?.Invoke(this, item);
        }
    }

    private void AppRowButton_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is Button { DataContext: SelectableAppViewModel item } button && ViewModel is not null)
        {
            int idx = ViewModel.FilteredApps.IndexOf(item);
            if (idx >= 0)
            {
                _focusedRowIndex = idx;
            }

            button.BringIntoView();
        }
    }

    private bool FocusRowAt(int index)
    {
        if (ViewModel is null || ViewModel.FilteredApps.Count == 0)
        {
            return false;
        }

        int clamped = Math.Clamp(index, 0, ViewModel.FilteredApps.Count - 1);
        _focusedRowIndex = clamped;
        DiscoveredAppsList.UpdateLayout();

        if (DiscoveredAppsList.ItemContainerGenerator.ContainerFromIndex(clamped) is DependencyObject container)
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
