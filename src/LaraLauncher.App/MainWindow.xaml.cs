using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LaraLauncher.App.Input;
using LaraLauncher.App.ViewModels;
using LaraLauncher.Core.Interfaces;
using LaraLauncher.Core.Models;
using LaraLauncher.Core.Services;

namespace LaraLauncher.App;

public partial class MainWindow : Window
{
    private const int TileColumns = 3;

    private readonly MainViewModel _viewModel;
    private readonly WpfInputRouter _inputRouter;
    private readonly IAppLauncherService _appLauncherService;

    private bool _isFullscreen = true;
    private int _lastFocusedTileIndex;
    private int _focusedSidebarIndex;

    public MainWindow(
        MainViewModel viewModel,
        WpfInputRouter inputRouter,
        IAppLauncherService appLauncherService)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _inputRouter = inputRouter;
        _appLauncherService = appLauncherService;
        DataContext = _viewModel;

        _inputRouter.ActionReceived += OnNavigationActionReceived;
        _appLauncherService.ApplicationLaunched += OnExternalAppLaunched;
        _appLauncherService.ApplicationExited += OnExternalAppExited;

        Closed += (_, _) =>
        {
            _inputRouter.ActionReceived -= OnNavigationActionReceived;
            _appLauncherService.ApplicationLaunched -= OnExternalAppLaunched;
            _appLauncherService.ApplicationExited -= OnExternalAppExited;
            _inputRouter.StopExternalProviders();
        };
    }

    public void ApplyWindowMode(bool fullscreen)
    {
        _isFullscreen = fullscreen;

        if (fullscreen)
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
        }
        else
        {
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            Width = 1280;
            Height = 720;
        }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
        _inputRouter.StartExternalProviders();

        await Dispatcher.InvokeAsync(FocusHomePrimaryControl);
    }

    private void Window_Activated(object? sender, EventArgs e)
    {
        _inputRouter.StartExternalProviders();
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        _inputRouter.StopExternalProviders();
    }

    private void OnExternalAppLaunched(object? sender, AppTile tile)
    {
        Dispatcher.BeginInvoke(() =>
        {
            _inputRouter.StopExternalProviders();
        });
    }

    private void OnExternalAppExited(object? sender, AppTile tile)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = _isFullscreen ? WindowState.Maximized : WindowState.Normal;
            }

            Activate();
            Focus();
            _inputRouter.StartExternalProviders();

            if (_viewModel.IsHomeGridVisible)
            {
                FocusTileAt(_lastFocusedTileIndex);
            }

            _viewModel.ShowToast($"De vuelta en LaraLauncher ({tile.Name} cerrado).");
        });
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            ApplyWindowMode(!_isFullscreen);
            e.Handled = true;
            return;
        }

        if (_inputRouter.TryRouteKey(e.Key))
        {
            e.Handled = true;
        }
    }

    private void OnNavigationActionReceived(object? sender, NavigationAction action)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => HandleNavigationAction(action));
            return;
        }

        HandleNavigationAction(action);
    }

    private void HandleNavigationAction(NavigationAction action)
    {
        if (_viewModel.IsExitDialogVisible)
        {
            HandleExitDialogNavigation(action);
            return;
        }

        if (action == NavigationAction.Home)
        {
            _viewModel.SelectSection(SidebarSection.Home);
            Dispatcher.BeginInvoke(FocusHomePrimaryControl);
            return;
        }

        if (IsFocusInSidebar())
        {
            HandleSidebarNavigation(action);
        }
        else if (_viewModel.IsHomeGridVisible)
        {
            HandleTileGridNavigation(action);
        }
        else if (_viewModel.IsHomeEmptyVisible)
        {
            HandleEmptyStateNavigation(action);
        }
        else if (action is NavigationAction.Left or NavigationAction.Back)
        {
            FocusSidebarForActiveSection();
        }
    }

    private void HandleExitDialogNavigation(NavigationAction action)
    {
        switch (action)
        {
            case NavigationAction.Back:
                CloseExitDialog();
                break;
            case NavigationAction.Left:
                CancelExitButton.Focus();
                break;
            case NavigationAction.Right:
                ConfirmExitButton.Focus();
                break;
            case NavigationAction.Select:
                if (ConfirmExitButton.IsKeyboardFocused)
                {
                    Application.Current.Shutdown();
                }
                else
                {
                    CloseExitDialog();
                }
                break;
        }
    }

    private void HandleSidebarNavigation(NavigationAction action)
    {
        int count = _viewModel.SidebarItems.Count;
        if (count == 0)
        {
            return;
        }

        switch (action)
        {
            case NavigationAction.Up:
                FocusSidebarAt(Math.Max(0, _focusedSidebarIndex - 1));
                break;

            case NavigationAction.Down:
                FocusSidebarAt(Math.Min(count - 1, _focusedSidebarIndex + 1));
                break;

            case NavigationAction.Right:
                if (_viewModel.IsHomeSection)
                {
                    FocusHomePrimaryControl();
                }
                break;

            case NavigationAction.Select:
                var item = _viewModel.SidebarItems[_focusedSidebarIndex];
                ActivateSidebarSection(item.Section);
                break;

            case NavigationAction.Back:
                if (_viewModel.IsHomeSection)
                {
                    FocusHomePrimaryControl();
                }
                break;
        }
    }

    private async void HandleEmptyStateNavigation(NavigationAction action)
    {
        switch (action)
        {
            case NavigationAction.Left:
                if (EmptyManageAppsButton.IsKeyboardFocused)
                {
                    EmptyAddStarterButton.Focus();
                }
                else
                {
                    FocusSidebarForActiveSection();
                }
                break;

            case NavigationAction.Right:
                EmptyManageAppsButton.Focus();
                break;

            case NavigationAction.Back:
                FocusSidebarForActiveSection();
                break;

            case NavigationAction.Select:
                if (EmptyManageAppsButton.IsKeyboardFocused)
                {
                    _viewModel.OpenManageApps();
                    FocusSidebarForActiveSection();
                }
                else
                {
                    await AddStarterAppsAndFocusGridAsync();
                }
                break;
        }
    }

    private async void HandleTileGridNavigation(NavigationAction action)
    {
        int tileCount = _viewModel.AppTiles.Count;

        switch (action)
        {
            case NavigationAction.Up:
            case NavigationAction.Down:
            case NavigationAction.Left:
            case NavigationAction.Right:
                var result = GridNavigator.Move(_lastFocusedTileIndex, tileCount, TileColumns, action);
                if (result.ExitToSidebar)
                {
                    FocusSidebarForActiveSection();
                }
                else if (result.NewIndex >= 0)
                {
                    FocusTileAt(result.NewIndex);
                }
                break;

            case NavigationAction.Back:
                FocusSidebarForActiveSection();
                break;

            case NavigationAction.Select:
                if (_lastFocusedTileIndex >= 0 && _lastFocusedTileIndex < tileCount)
                {
                    await _viewModel.LaunchTileAsync(_viewModel.AppTiles[_lastFocusedTileIndex]);
                }
                break;

            case NavigationAction.ContextMenu:
                if (_lastFocusedTileIndex >= 0 && _lastFocusedTileIndex < tileCount)
                {
                    var tile = _viewModel.AppTiles[_lastFocusedTileIndex];
                    _viewModel.ShowToast($"Opciones de {tile.Name}: Mover / Editar / Quitar (Fase 3)");
                }
                break;
        }
    }

    private async Task AddStarterAppsAndFocusGridAsync()
    {
        await _viewModel.AddStarterAppsAsync();
        await Dispatcher.InvokeAsync(() => FocusTileAt(0));
    }

    private void FocusHomePrimaryControl()
    {
        if (_viewModel.IsHomeGridVisible && FocusTileAt(_lastFocusedTileIndex))
        {
            return;
        }

        if (_viewModel.IsHomeEmptyVisible)
        {
            EmptyAddStarterButton.Focus();
            return;
        }

        FocusSidebarForActiveSection();
    }

    private void ActivateSidebarSection(SidebarSection section)
    {
        _viewModel.SelectSection(section);

        if (section == SidebarSection.Exit)
        {
            Dispatcher.BeginInvoke(() => CancelExitButton.Focus());
            return;
        }

        if (section == SidebarSection.Home)
        {
            Dispatcher.BeginInvoke(FocusHomePrimaryControl);
        }
    }

    private void CloseExitDialog()
    {
        _viewModel.CancelExit();
        FocusSidebarForActiveSection();
    }

    private void SidebarButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SidebarSection section })
        {
            ActivateSidebarSection(section);
        }
    }

    private void SidebarButton_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is Button { DataContext: SidebarItemViewModel item })
        {
            int idx = _viewModel.SidebarItems.IndexOf(item);
            if (idx >= 0)
            {
                _focusedSidebarIndex = idx;
                if (item.Section != SidebarSection.Exit)
                {
                    _viewModel.SelectSection(item.Section);
                }
            }
        }
    }

    private void TileButton_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is Button { DataContext: AppTile tile } button)
        {
            int idx = _viewModel.AppTiles.IndexOf(tile);
            if (idx >= 0)
            {
                _lastFocusedTileIndex = idx;
            }

            button.BringIntoView();
        }
    }

    private async void EmptyAddStarterButton_Click(object sender, RoutedEventArgs e)
        => await AddStarterAppsAndFocusGridAsync();

    private void EmptyManageAppsButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.OpenManageApps();
        FocusSidebarForActiveSection();
    }

    private void CancelExitButton_Click(object sender, RoutedEventArgs e) => CloseExitDialog();

    private void ConfirmExitButton_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

    private bool IsFocusInSidebar()
    {
        if (Keyboard.FocusedElement is not DependencyObject focused)
        {
            return false;
        }

        DependencyObject? current = focused;
        while (current is not null)
        {
            if (ReferenceEquals(current, SidebarItemsControl))
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private void FocusSidebarForActiveSection()
    {
        for (int i = 0; i < _viewModel.SidebarItems.Count; i++)
        {
            if (_viewModel.SidebarItems[i].Section == _viewModel.ActiveSection)
            {
                FocusSidebarAt(i);
                return;
            }
        }

        FocusSidebarAt(0);
    }

    private bool FocusSidebarAt(int index)
    {
        if (index < 0 || index >= _viewModel.SidebarItems.Count)
        {
            return false;
        }

        _focusedSidebarIndex = index;
        SidebarItemsControl.UpdateLayout();
        if (SidebarItemsControl.ItemContainerGenerator.ContainerFromIndex(index) is DependencyObject container)
        {
            var button = FindVisualChild<Button>(container);
            if (button is not null)
            {
                return button.Focus();
            }
        }

        return false;
    }

    private bool FocusTileAt(int index)
    {
        if (_viewModel.AppTiles.Count == 0)
        {
            return false;
        }

        int clamped = Math.Clamp(index, 0, _viewModel.AppTiles.Count - 1);
        _lastFocusedTileIndex = clamped;
        TileGridControl.UpdateLayout();

        if (TileGridControl.ItemContainerGenerator.ContainerFromIndex(clamped) is DependencyObject container)
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
