using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using LaraLauncher.Core.Models;
using LaraLauncher.Core.Services;

namespace LaraLauncher.App.Controls;

public sealed partial class VirtualKeyItem : ObservableObject
{
    public VirtualKeyDefinition Definition { get; }
    public bool IsActionKey => Definition.Kind is not VirtualKeyKind.Character;
    public bool IsPrimaryDoneKey => Definition.Kind == VirtualKeyKind.Done;

    [ObservableProperty]
    private string _label;

    public VirtualKeyItem(VirtualKeyDefinition definition, bool isUpperCase)
    {
        Definition = definition;
        _label = definition.GetDisplayLabel(isUpperCase);
    }

    public void RefreshCase(bool isUpperCase)
    {
        Label = Definition.GetDisplayLabel(isUpperCase);
    }
}

public partial class VirtualKeyboardControl : UserControl
{
    private readonly ObservableCollection<VirtualKeyItem> _keyItems = new();
    private bool _isUpperCase;
    private int _focusedKeyIndex = 10; // Default to 'q' (first letter key)

    public event EventHandler<(VirtualKeyDefinition Key, bool IsUpperCase)>? KeyActivated;
    public event EventHandler? DoneRequested;
    public event EventHandler? ExitUpRequested;
    public event EventHandler? ExitLeftRequested;

    public string HeaderLabel
    {
        get => KeyboardHeaderText.Text;
        set => KeyboardHeaderText.Text = value;
    }

    public bool IsFocusInsideKeyboard
    {
        get
        {
            if (Keyboard.FocusedElement is not DependencyObject focused)
            {
                return false;
            }

            DependencyObject? current = focused;
            while (current is not null)
            {
                if (ReferenceEquals(current, KeysGrid))
                {
                    return true;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }
    }

    public VirtualKeyboardControl()
    {
        InitializeComponent();

        foreach (var def in VirtualKeyboardLayout.Keys)
        {
            _keyItems.Add(new VirtualKeyItem(def, _isUpperCase));
        }

        KeysGrid.ItemsSource = _keyItems;
    }

    public bool FocusKey(int? index = null)
    {
        int target = Math.Clamp(index ?? _focusedKeyIndex, 0, _keyItems.Count - 1);
        _focusedKeyIndex = target;
        KeysGrid.UpdateLayout();

        if (KeysGrid.ItemContainerGenerator.ContainerFromIndex(target) is DependencyObject container)
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

    public bool HandleNavigationAction(NavigationAction action)
    {
        if (!IsFocusInsideKeyboard)
        {
            return false;
        }

        switch (action)
        {
            case NavigationAction.Up:
                if (_focusedKeyIndex < VirtualKeyboardLayout.Columns)
                {
                    ExitUpRequested?.Invoke(this, EventArgs.Empty);
                    return true;
                }

                return FocusKey(_focusedKeyIndex - VirtualKeyboardLayout.Columns);

            case NavigationAction.Down:
            case NavigationAction.Left:
            case NavigationAction.Right:
                var result = GridNavigator.Move(
                    _focusedKeyIndex,
                    _keyItems.Count,
                    VirtualKeyboardLayout.Columns,
                    action);

                if (result.ExitToSidebar)
                {
                    ExitLeftRequested?.Invoke(this, EventArgs.Empty);
                    return true;
                }

                return FocusKey(result.NewIndex);

            case NavigationAction.Back:
                DoneRequested?.Invoke(this, EventArgs.Empty);
                return true;

            case NavigationAction.Select:
                if (_focusedKeyIndex >= 0 && _focusedKeyIndex < _keyItems.Count)
                {
                    TriggerKey(_keyItems[_focusedKeyIndex]);
                    return true;
                }

                return false;

            default:
                return false;
        }
    }

    private void TriggerKey(VirtualKeyItem item)
    {
        switch (item.Definition.Kind)
        {
            case VirtualKeyKind.ToggleCase:
                _isUpperCase = !_isUpperCase;
                foreach (var key in _keyItems)
                {
                    key.RefreshCase(_isUpperCase);
                }
                break;

            case VirtualKeyKind.Done:
                DoneRequested?.Invoke(this, EventArgs.Empty);
                break;

            default:
                KeyActivated?.Invoke(this, (item.Definition, _isUpperCase));
                break;
        }
    }

    private void KeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: VirtualKeyItem item })
        {
            TriggerKey(item);
        }
    }

    private void KeyButton_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is Button { DataContext: VirtualKeyItem item })
        {
            int idx = _keyItems.IndexOf(item);
            if (idx >= 0)
            {
                _focusedKeyIndex = idx;
            }
        }
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
