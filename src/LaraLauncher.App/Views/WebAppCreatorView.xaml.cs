using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LaraLauncher.App.ViewModels;
using LaraLauncher.Core.Models;
using LaraLauncher.Core.Services;

namespace LaraLauncher.App.Views;

public partial class WebAppCreatorView : UserControl
{
    private int _focusedSwatchIndex;

    public event EventHandler? CreateWebAppRequested;
    public event EventHandler? ExitToSidebarRequested;

    public WebAppCreatorView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => SyncKeyboardHeader();
    }

    private WebAppCreatorViewModel? ViewModel => DataContext as WebAppCreatorViewModel;

    public bool FocusPrimaryControl()
    {
        ViewModel?.SetActiveField(WebAppInputField.Url);
        SyncKeyboardHeader();
        return EditUrlWithKeyboardButton.Focus();
    }

    public bool HandleNavigationAction(NavigationAction action)
    {
        if (ViewModel is null)
        {
            return false;
        }

        // 1. Inside Virtual Keyboard
        if (VirtualKeyboard.IsFocusInsideKeyboard)
        {
            return VirtualKeyboard.HandleNavigationAction(action);
        }

        // 2. Top form controls (100% remote/gamepad operable)
        if (EditUrlWithKeyboardButton.IsKeyboardFocused)
        {
            switch (action)
            {
                case NavigationAction.Left:
                case NavigationAction.Back:
                    return RaiseExitToSidebar();
                case NavigationAction.Down:
                    return EditNameWithKeyboardButton.Focus();
                case NavigationAction.Select:
                    StartKeyboardForField(WebAppInputField.Url);
                    return true;
            }
        }

        if (EditNameWithKeyboardButton.IsKeyboardFocused)
        {
            switch (action)
            {
                case NavigationAction.Left:
                case NavigationAction.Back:
                    return RaiseExitToSidebar();
                case NavigationAction.Up:
                    return EditUrlWithKeyboardButton.Focus();
                case NavigationAction.Down:
                    return CreateWebAppButton.Focus();
                case NavigationAction.Select:
                    StartKeyboardForField(WebAppInputField.Name);
                    return true;
            }
        }

        if (CreateWebAppButton.IsKeyboardFocused)
        {
            switch (action)
            {
                case NavigationAction.Up:
                    return EditNameWithKeyboardButton.Focus();
                case NavigationAction.Left:
                    return FocusSwatchAt(ViewModel.AccentSwatches.Count - 1);
                case NavigationAction.Down:
                    return VirtualKeyboard.FocusKey(0);
                case NavigationAction.Back:
                    return RaiseExitToSidebar();
                case NavigationAction.Select:
                    CreateWebAppRequested?.Invoke(this, EventArgs.Empty);
                    return true;
            }
        }

        if (IsFocusInSwatches())
        {
            switch (action)
            {
                case NavigationAction.Up:
                    return EditNameWithKeyboardButton.Focus();
                case NavigationAction.Down:
                    return VirtualKeyboard.FocusKey(0);
                case NavigationAction.Left:
                    return _focusedSwatchIndex <= 0
                        ? RaiseExitToSidebar()
                        : FocusSwatchAt(_focusedSwatchIndex - 1);
                case NavigationAction.Right:
                    return _focusedSwatchIndex + 1 < ViewModel.AccentSwatches.Count
                        ? FocusSwatchAt(_focusedSwatchIndex + 1)
                        : CreateWebAppButton.Focus();
                case NavigationAction.Back:
                    return RaiseExitToSidebar();
                case NavigationAction.Select:
                    ViewModel.SelectAccent(ViewModel.AccentSwatches[_focusedSwatchIndex]);
                    return true;
            }
        }

        // 3. Physical keyboard typing inside TextBoxes
        if (UrlTextBox.IsKeyboardFocused || NameTextBox.IsKeyboardFocused)
        {
            if (action == NavigationAction.Back)
            {
                return RaiseExitToSidebar();
            }

            if (action == NavigationAction.Down)
            {
                return UrlTextBox.IsKeyboardFocused
                    ? NameTextBox.Focus()
                    : CreateWebAppButton.Focus();
            }

            return false;
        }

        if (action is NavigationAction.Left or NavigationAction.Back)
        {
            return RaiseExitToSidebar();
        }

        return false;
    }

    private void StartKeyboardForField(WebAppInputField field)
    {
        if (ViewModel is null)
        {
            return;
        }

        ViewModel.SetActiveField(field);
        SyncKeyboardHeader();
        VirtualKeyboard.FocusKey(10); // Focus 'q'
    }

    private void SyncKeyboardHeader()
    {
        if (ViewModel is not null)
        {
            VirtualKeyboard.HeaderLabel = ViewModel.KeyboardHeaderLabel;
        }
    }

    private bool RaiseExitToSidebar()
    {
        ExitToSidebarRequested?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private void EditUrlWithKeyboardButton_Click(object sender, RoutedEventArgs e)
        => StartKeyboardForField(WebAppInputField.Url);

    private void EditNameWithKeyboardButton_Click(object sender, RoutedEventArgs e)
        => StartKeyboardForField(WebAppInputField.Name);

    private void UrlTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        ViewModel?.SetActiveField(WebAppInputField.Url);
        SyncKeyboardHeader();
    }

    private void NameTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        ViewModel?.SetActiveField(WebAppInputField.Name);
        SyncKeyboardHeader();
    }

    private void SwatchButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string hex })
        {
            ViewModel?.SelectAccent(hex);
        }
    }

    private void SwatchButton_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is Button { Tag: string hex } && ViewModel is not null)
        {
            for (int i = 0; i < ViewModel.AccentSwatches.Count; i++)
            {
                if (ViewModel.AccentSwatches[i] == hex)
                {
                    _focusedSwatchIndex = i;
                    break;
                }
            }
        }
    }

    private void CreateWebAppButton_Click(object sender, RoutedEventArgs e)
        => CreateWebAppRequested?.Invoke(this, EventArgs.Empty);

    private void VirtualKeyboard_KeyActivated(object? sender, (VirtualKeyDefinition Key, bool IsUpperCase) args)
        => ViewModel?.ApplyVirtualKey(args.Key, args.IsUpperCase);

    private void VirtualKeyboard_DoneRequested(object? sender, EventArgs e)
        => CreateWebAppButton.Focus();

    private void VirtualKeyboard_ExitUpRequested(object? sender, EventArgs e)
        => CreateWebAppButton.Focus();

    private void VirtualKeyboard_ExitLeftRequested(object? sender, EventArgs e)
        => RaiseExitToSidebar();

    private bool IsFocusInSwatches()
    {
        if (Keyboard.FocusedElement is not DependencyObject focused)
        {
            return false;
        }

        DependencyObject? current = focused;
        while (current is not null)
        {
            if (ReferenceEquals(current, SwatchList))
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private bool FocusSwatchAt(int index)
    {
        if (ViewModel is null || ViewModel.AccentSwatches.Count == 0)
        {
            return false;
        }

        int clamped = Math.Clamp(index, 0, ViewModel.AccentSwatches.Count - 1);
        _focusedSwatchIndex = clamped;
        SwatchList.UpdateLayout();

        if (SwatchList.ItemContainerGenerator.ContainerFromIndex(clamped) is DependencyObject container)
        {
            var button = FindVisualChild<Button>(container);
            if (button is not null)
            {
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
