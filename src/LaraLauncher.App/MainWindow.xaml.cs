using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LaraLauncher.App.Input;
using LaraLauncher.App.ViewModels;
using LaraLauncher.Core.Interfaces;
using LaraLauncher.Core.Models;
using LaraLauncher.Core.Services;
using LaraLauncher.Infrastructure.Input;

namespace LaraLauncher.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly WpfInputRouter _inputRouter;
    private readonly IAppLauncherService _appLauncherService;
    private readonly XInputGamepadProvider _gamepadProvider;
    private readonly INavigationSoundService _soundService;

    private bool _isFullscreen = true;
    private bool _cliForceWindowed;
    private bool _hasAppliedInitialWindowPref;
    private int _lastFocusedTileIndex;
    private int _focusedSidebarIndex;

    public MainWindow(
        MainViewModel viewModel,
        WpfInputRouter inputRouter,
        IAppLauncherService appLauncherService,
        XInputGamepadProvider gamepadProvider,
        INavigationSoundService soundService)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _inputRouter = inputRouter;
        _appLauncherService = appLauncherService;
        _gamepadProvider = gamepadProvider;
        _soundService = soundService;
        DataContext = _viewModel;

        _inputRouter.ActionReceived += OnNavigationActionReceived;
        _appLauncherService.ApplicationLaunched += OnExternalAppLaunched;
        _appLauncherService.ApplicationExited += OnExternalAppExited;
        _viewModel.RuntimePreferencesUpdated += OnRuntimePreferencesUpdated;
        _viewModel.Settings.PowerActionRequested += OnPowerActionRequested;

        Closed += (_, _) =>
        {
            _inputRouter.ActionReceived -= OnNavigationActionReceived;
            _appLauncherService.ApplicationLaunched -= OnExternalAppLaunched;
            _appLauncherService.ApplicationExited -= OnExternalAppExited;
            _viewModel.RuntimePreferencesUpdated -= OnRuntimePreferencesUpdated;
            _viewModel.Settings.PowerActionRequested -= OnPowerActionRequested;
            _inputRouter.StopExternalProviders();
        };
    }

    public void ConfigureStartupWindowOverride(bool forceWindowed)
    {
        _cliForceWindowed = forceWindowed;
    }

    public void RestoreAndActivate()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = _isFullscreen ? WindowState.Maximized : WindowState.Normal;
        }

        Show();
        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    private void OnRuntimePreferencesUpdated(object? sender, LauncherSettings settings)
    {
        _soundService.IsEnabled = settings.EnableNavigationSounds;
        _gamepadProvider.IsEnabled = settings.EnableGamepad;
        if (settings.EnableGamepad && IsActive)
        {
            _inputRouter.StartExternalProviders();
        }
        else if (!settings.EnableGamepad)
        {
            _inputRouter.StopExternalProviders();
        }

        if (!_hasAppliedInitialWindowPref)
        {
            _hasAppliedInitialWindowPref = true;
            if (!_cliForceWindowed)
            {
                ApplyWindowMode(settings.StartFullscreen);
            }
        }
        else if (_isFullscreen != settings.StartFullscreen)
        {
            ApplyWindowMode(settings.StartFullscreen);
        }
    }

    private void PlayFeedbackSound(NavigationAction action)
    {
        switch (action)
        {
            case NavigationAction.Up:
            case NavigationAction.Down:
            case NavigationAction.Left:
            case NavigationAction.Right:
                _soundService.PlayMove();
                break;
            case NavigationAction.Select:
            case NavigationAction.ContextMenu:
                _soundService.PlaySelect();
                break;
            case NavigationAction.Back:
            case NavigationAction.Home:
                _soundService.PlayBack();
                break;
        }
    }

    private void OnPowerActionRequested(object? sender, PowerActionKind action)
    {
        Dispatcher.BeginInvoke(() => CancelExitButton.Focus());
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
        Dispatcher.BeginInvoke(() => _inputRouter.StopExternalProviders());
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

        // Do not hijack typing keys (letters, space, backspace, left/right cursor) while typing in a TextBox.
        if (Keyboard.FocusedElement is TextBox)
        {
            if (e.Key == Key.Escape)
            {
                _inputRouter.TryRouteKey(e.Key);
                e.Handled = true;
            }
            else if (e.Key == Key.Down &&
                     (_viewModel.IsManageAppsSection || _viewModel.IsWeatherSection) &&
                     !_viewModel.IsEditTileDialogVisible)
            {
                _inputRouter.TryRouteKey(e.Key);
                e.Handled = true;
            }

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
        PlayFeedbackSound(action);

        if (_viewModel.IsExitDialogVisible)
        {
            HandleExitDialogNavigation(action);
            return;
        }

        if (_viewModel.IsEditTileDialogVisible)
        {
            HandleEditTileDialogNavigation(action);
            return;
        }

        if (_viewModel.IsTileContextMenuVisible)
        {
            HandleTileContextMenuNavigation(action);
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
        else if (_viewModel.IsManageAppsSection)
        {
            AppManagerViewControl.HandleNavigationAction(action);
        }
        else if (_viewModel.IsWeatherSection)
        {
            _ = WeatherDetailViewControl.HandleNavigationActionAsync(action);
        }
        else if (_viewModel.IsSettingsSection)
        {
            SettingsViewControl.HandleNavigationAction(action);
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
                    ExecuteConfirmedPowerAction();
                }
                else
                {
                    CloseExitDialog();
                }
                break;
        }
    }

    private async void HandleTileContextMenuNavigation(NavigationAction action)
    {
        Button[] buttons = [ContextMoveButton, ContextEditButton, ContextRemoveButton, ContextCancelButton];
        int current = Array.FindIndex(buttons, b => b.IsKeyboardFocused);
        if (current < 0)
        {
            current = 0;
        }

        switch (action)
        {
            case NavigationAction.Up:
                buttons[Math.Max(0, current - 1)].Focus();
                break;
            case NavigationAction.Down:
                buttons[Math.Min(buttons.Length - 1, current + 1)].Focus();
                break;
            case NavigationAction.Back:
                CloseTileContextMenuAndRestoreFocus();
                break;
            case NavigationAction.Select:
                if (ReferenceEquals(buttons[current], ContextMoveButton))
                {
                    StartMoveModeFromContext();
                }
                else if (ReferenceEquals(buttons[current], ContextEditButton))
                {
                    OpenEditDialogFromContext();
                }
                else if (ReferenceEquals(buttons[current], ContextRemoveButton))
                {
                    await RemoveTileFromContextAsync();
                }
                else
                {
                    CloseTileContextMenuAndRestoreFocus();
                }
                break;
        }
    }

    private async void HandleEditTileDialogNavigation(NavigationAction action)
    {
        switch (action)
        {
            case NavigationAction.Back:
                CancelEditDialogAndRestoreFocus();
                break;
            case NavigationAction.Left:
                if (EditSaveButton.IsKeyboardFocused)
                {
                    EditCancelButton.Focus();
                }
                break;
            case NavigationAction.Right:
                if (EditCancelButton.IsKeyboardFocused)
                {
                    EditSaveButton.Focus();
                }
                break;
            case NavigationAction.Select:
                if (EditSaveButton.IsKeyboardFocused)
                {
                    await SaveEditDialogAndRestoreFocusAsync();
                }
                else if (EditCancelButton.IsKeyboardFocused)
                {
                    CancelEditDialogAndRestoreFocus();
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
                FocusActiveSectionContent();
                break;

            case NavigationAction.Select:
                var item = _viewModel.SidebarItems[_focusedSidebarIndex];
                ActivateSidebarSection(item.Section);
                break;

            case NavigationAction.Back:
                FocusActiveSectionContent();
                break;
        }
    }

    private async void HandleEmptyStateNavigation(NavigationAction action)
    {
        switch (action)
        {
            case NavigationAction.Left:
                if (EmptyAddStarterButton.IsKeyboardFocused)
                {
                    EmptyManageAppsButton.Focus();
                }
                else
                {
                    FocusSidebarForActiveSection();
                }
                break;

            case NavigationAction.Right:
                EmptyAddStarterButton.Focus();
                break;

            case NavigationAction.Back:
                FocusSidebarForActiveSection();
                break;

            case NavigationAction.Select:
                if (EmptyAddStarterButton.IsKeyboardFocused)
                {
                    await AddStarterAppsAndFocusGridAsync();
                }
                else
                {
                    OpenManageAppsAndFocus();
                }
                break;
        }
    }

    private async void HandleTileGridNavigation(NavigationAction action)
    {
        int tileCount = _viewModel.AppTiles.Count;

        if (_viewModel.IsMoveMode)
        {
            await HandleMoveModeNavigationAsync(action, tileCount);
            return;
        }

        switch (action)
        {
            case NavigationAction.Up:
            case NavigationAction.Down:
            case NavigationAction.Left:
            case NavigationAction.Right:
                var result = GridNavigator.Move(_lastFocusedTileIndex, tileCount, _viewModel.GridColumns, action);
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
                    OpenContextMenuForTile(_viewModel.AppTiles[_lastFocusedTileIndex]);
                }
                break;
        }
    }

    private async Task HandleMoveModeNavigationAsync(NavigationAction action, int tileCount)
    {
        switch (action)
        {
            case NavigationAction.Up:
            case NavigationAction.Down:
            case NavigationAction.Left:
            case NavigationAction.Right:
                var result = GridNavigator.Move(_lastFocusedTileIndex, tileCount, _viewModel.GridColumns, action);
                if (!result.ExitToSidebar && result.NewIndex >= 0 && result.NewIndex != _lastFocusedTileIndex)
                {
                    int targetIndex = result.NewIndex;
                    if (_viewModel.TrySwapTileInMoveMode(_lastFocusedTileIndex, targetIndex))
                    {
                        _lastFocusedTileIndex = targetIndex;
                        await Dispatcher.InvokeAsync(() => FocusTileAt(targetIndex));
                    }
                }
                break;

            case NavigationAction.Select:
            case NavigationAction.Back:
                await _viewModel.FinishMoveModeAsync();
                FocusTileAt(_lastFocusedTileIndex);
                break;
        }
    }

    private void FocusActiveSectionContent()
    {
        if (_viewModel.IsHomeSection)
        {
            FocusHomePrimaryControl();
        }
        else if (_viewModel.IsManageAppsSection)
        {
            AppManagerViewControl.FocusPrimaryControl();
        }
        else if (_viewModel.IsWeatherSection)
        {
            WeatherDetailViewControl.FocusPrimaryControl();
        }
        else if (_viewModel.IsSettingsSection)
        {
            SettingsViewControl.FocusPrimaryControl();
        }
    }

    private async Task AddStarterAppsAndFocusGridAsync()
    {
        await _viewModel.AddStarterAppsAsync();
        await Dispatcher.InvokeAsync(() => FocusTileAt(0));
    }

    private void OpenManageAppsAndFocus()
    {
        _viewModel.OpenManageApps();
        Dispatcher.BeginInvoke(() => AppManagerViewControl.FocusPrimaryControl());
    }

    private void FocusHomePrimaryControl()
    {
        if (_viewModel.IsHomeGridVisible && FocusTileAt(_lastFocusedTileIndex))
        {
            return;
        }

        if (_viewModel.IsHomeEmptyVisible)
        {
            EmptyManageAppsButton.Focus();
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
        else if (section == SidebarSection.ManageApps)
        {
            Dispatcher.BeginInvoke(() => AppManagerViewControl.FocusPrimaryControl());
        }
        else if (section == SidebarSection.Weather)
        {
            Dispatcher.BeginInvoke(() => WeatherDetailViewControl.FocusPrimaryControl());
        }
        else if (section == SidebarSection.Settings)
        {
            Dispatcher.BeginInvoke(() => SettingsViewControl.FocusPrimaryControl());
        }
    }

    private void OpenContextMenuForTile(AppTile tile)
    {
        _viewModel.OpenTileContextMenu(tile);
        Dispatcher.BeginInvoke(() => ContextMoveButton.Focus());
    }

    private void CloseTileContextMenuAndRestoreFocus()
    {
        _viewModel.CloseTileContextMenu();
        FocusHomePrimaryControl();
    }

    private void StartMoveModeFromContext()
    {
        _viewModel.StartMoveMode();
        FocusTileAt(_lastFocusedTileIndex);
    }

    private void OpenEditDialogFromContext()
    {
        _viewModel.OpenEditTileDialog();
        Dispatcher.BeginInvoke(() =>
        {
            EditNameTextBox.Focus();
            EditNameTextBox.SelectAll();
        });
    }

    private async Task SaveEditDialogAndRestoreFocusAsync()
    {
        await _viewModel.SaveEditTileAsync();
        await Dispatcher.InvokeAsync(FocusHomePrimaryControl);
    }

    private void CancelEditDialogAndRestoreFocus()
    {
        _viewModel.CancelEditTile();
        FocusHomePrimaryControl();
    }

    private async Task RemoveTileFromContextAsync()
    {
        await _viewModel.RemoveContextTileAsync();
        await Dispatcher.InvokeAsync(FocusHomePrimaryControl);
    }

    private void CloseExitDialog()
    {
        _viewModel.CancelExit();
        FocusSidebarForActiveSection();
    }

    // Event handlers for Sidebar, Grid, AppManagerView, and Overlays
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

    private void TileButton_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is Button { DataContext: AppTile tile })
        {
            int idx = _viewModel.AppTiles.IndexOf(tile);
            if (idx >= 0)
            {
                _lastFocusedTileIndex = idx;
            }

            OpenContextMenuForTile(tile);
            e.Handled = true;
        }
    }

    private async void AppManagerView_AppToggled(object? sender, SelectableAppViewModel item)
        => await _viewModel.ToggleDiscoveredAppAsync(item);

    private async void AppManagerView_ManualFileSelected(object? sender, string filePath)
        => await _viewModel.AddManualAppFileAsync(filePath);

    private async void AppManagerView_RefreshRequested(object? sender, EventArgs e)
        => await _viewModel.AppManager.RefreshScanAsync(_viewModel.AppTiles);

    private void AppManagerView_ExitToSidebarRequested(object? sender, EventArgs e)
        => FocusSidebarForActiveSection();

    private void WeatherDetailView_ExitToSidebarRequested(object? sender, EventArgs e)
        => FocusSidebarForActiveSection();

    private void SettingsView_ExitToSidebarRequested(object? sender, EventArgs e)
        => FocusSidebarForActiveSection();

    private void WeatherHeaderPill_Click(object sender, RoutedEventArgs e)
        => ActivateSidebarSection(SidebarSection.Weather);

    private void ExecuteConfirmedPowerAction()
    {
        bool shouldExitApp = _viewModel.ExecutePendingPowerAction();
        if (shouldExitApp)
        {
            Application.Current.Shutdown();
        }
        else
        {
            FocusSidebarForActiveSection();
        }
    }

    private async void EmptyAddStarterButton_Click(object sender, RoutedEventArgs e)
        => await AddStarterAppsAndFocusGridAsync();

    private void EmptyManageAppsButton_Click(object sender, RoutedEventArgs e)
        => OpenManageAppsAndFocus();

    private void ContextMoveButton_Click(object sender, RoutedEventArgs e) => StartMoveModeFromContext();
    private void ContextEditButton_Click(object sender, RoutedEventArgs e) => OpenEditDialogFromContext();
    private async void ContextRemoveButton_Click(object sender, RoutedEventArgs e) => await RemoveTileFromContextAsync();
    private void ContextCancelButton_Click(object sender, RoutedEventArgs e) => CloseTileContextMenuAndRestoreFocus();

    private void AccentSwatchButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string hex })
        {
            _viewModel.SelectEditAccent(hex);
        }
    }

    private async void EditSaveButton_Click(object sender, RoutedEventArgs e) => await SaveEditDialogAndRestoreFocusAsync();
    private void EditCancelButton_Click(object sender, RoutedEventArgs e) => CancelEditDialogAndRestoreFocus();

    private void CancelExitButton_Click(object sender, RoutedEventArgs e) => CloseExitDialog();
    private void ConfirmExitButton_Click(object sender, RoutedEventArgs e) => ExecuteConfirmedPowerAction();

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
