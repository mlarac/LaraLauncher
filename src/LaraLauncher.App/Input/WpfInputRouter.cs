using System.Windows.Input;
using LaraLauncher.Core.Interfaces;
using LaraLauncher.Core.Models;

namespace LaraLauncher.App.Input;

/// <summary>
/// Translates WPF keyboard / IR-RF remote keys and external IInputProvider events
/// into unified semantic NavigationActions.
/// </summary>
public sealed class WpfInputRouter : IDisposable
{
    private readonly IReadOnlyList<IInputProvider> _providers;

    public event EventHandler<NavigationAction>? ActionReceived;

    public WpfInputRouter(IEnumerable<IInputProvider> providers)
    {
        _providers = providers.ToList();
        foreach (var provider in _providers)
        {
            provider.ActionReceived += OnExternalProviderAction;
        }
    }

    public void StartExternalProviders()
    {
        foreach (var provider in _providers)
        {
            provider.Start();
        }
    }

    public void StopExternalProviders()
    {
        foreach (var provider in _providers)
        {
            provider.Stop();
        }
    }

    public static NavigationAction MapKey(Key key) => key switch
    {
        Key.Up => NavigationAction.Up,
        Key.Down => NavigationAction.Down,
        Key.Left => NavigationAction.Left,
        Key.Right => NavigationAction.Right,
        Key.Enter or Key.Return or Key.Space => NavigationAction.Select,
        Key.Escape or Key.Back or Key.BrowserBack => NavigationAction.Back,
        Key.Apps or Key.M => NavigationAction.ContextMenu,
        Key.BrowserHome or Key.Home => NavigationAction.Home,
        _ => NavigationAction.None
    };

    public bool TryRouteKey(Key key)
    {
        var action = MapKey(key);
        if (action == NavigationAction.None)
        {
            return false;
        }

        ActionReceived?.Invoke(this, action);
        return true;
    }

    private void OnExternalProviderAction(object? sender, NavigationAction action)
    {
        if (action != NavigationAction.None)
        {
            ActionReceived?.Invoke(this, action);
        }
    }

    public void Dispose()
    {
        foreach (var provider in _providers)
        {
            provider.ActionReceived -= OnExternalProviderAction;
            provider.Dispose();
        }
    }
}
