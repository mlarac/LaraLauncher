using CommunityToolkit.Mvvm.ComponentModel;
using LaraLauncher.Core.Interfaces;
using LaraLauncher.Core.Models;
using LaraLauncher.Core.Services;

namespace LaraLauncher.App.ViewModels;

public enum WebAppInputField
{
    Url,
    Name
}

public sealed partial class WebAppCreatorViewModel : ObservableObject
{
    private readonly IWebAppIconFetcher _iconFetcher;

    public IReadOnlyList<string> AccentSwatches { get; } =
    [
        "#5A2EA6",
        "#B32028",
        "#2457A6",
        "#1A7F3E",
        "#B85C14",
        "#7A2E8C"
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewTitle))]
    [NotifyPropertyChangedFor(nameof(PreviewSubtitle))]
    private string _urlInput = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewTitle))]
    private string _nameInput = string.Empty;

    [ObservableProperty]
    private string _selectedAccentHex = "#5A2EA6";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditingUrl))]
    [NotifyPropertyChangedFor(nameof(IsEditingName))]
    [NotifyPropertyChangedFor(nameof(KeyboardHeaderLabel))]
    private WebAppInputField _activeField = WebAppInputField.Url;

    [ObservableProperty]
    private bool _isCreating;

    [ObservableProperty]
    private string _feedbackMessage = "Ingresa la dirección del sitio web usando el teclado virtual o tu teclado físico.";

    [ObservableProperty]
    private bool _isFeedbackError;

    public bool IsEditingUrl => ActiveField == WebAppInputField.Url;
    public bool IsEditingName => ActiveField == WebAppInputField.Name;

    public string KeyboardHeaderLabel => ActiveField == WebAppInputField.Url
        ? "Teclado Virtual  •  Editando: Dirección del sitio (URL)"
        : "Teclado Virtual  •  Editando: Nombre de la tarjeta";

    public string PreviewTitle
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(NameInput))
            {
                return NameInput.Trim();
            }

            var normalized = WebAppUrlNormalizer.Normalize(UrlInput);
            return normalized.IsValid ? normalized.SuggestedName : "Nueva WebApp";
        }
    }

    public string PreviewSubtitle
    {
        get
        {
            var normalized = WebAppUrlNormalizer.Normalize(UrlInput);
            return normalized.IsValid ? normalized.Host : "Se abrirá en tu navegador por defecto";
        }
    }

    public WebAppCreatorViewModel(IWebAppIconFetcher iconFetcher)
    {
        _iconFetcher = iconFetcher;
    }

    public void SetActiveField(WebAppInputField field)
    {
        ActiveField = field;
    }

    public void SelectAccent(string hex)
    {
        if (!string.IsNullOrWhiteSpace(hex))
        {
            SelectedAccentHex = hex;
        }
    }

    public void ApplyVirtualKey(VirtualKeyDefinition key, bool isUpperCase)
    {
        IsFeedbackError = false;

        if (ActiveField == WebAppInputField.Url)
        {
            UrlInput = VirtualKeyboardLayout.ApplyKey(UrlInput, key, isUpperCase);
        }
        else
        {
            NameInput = VirtualKeyboardLayout.ApplyKey(NameInput, key, isUpperCase);
        }
    }

    public void ResetForm()
    {
        UrlInput = string.Empty;
        NameInput = string.Empty;
        ActiveField = WebAppInputField.Url;
        IsFeedbackError = false;
        FeedbackMessage = "Ingresa la dirección del sitio web usando el teclado virtual o tu teclado físico.";
    }

    public async Task<AppTile?> CreateWebAppTileAsync(IReadOnlyCollection<AppTile> existingTiles, int order)
    {
        if (IsCreating)
        {
            return null;
        }

        var normalized = WebAppUrlNormalizer.Normalize(UrlInput);
        if (!normalized.IsValid)
        {
            IsFeedbackError = true;
            FeedbackMessage = normalized.ErrorMessage ?? "URL inválida.";
            return null;
        }

        if (existingTiles.Any(t =>
                t.LaunchType == LaunchType.Uri &&
                string.Equals(t.ExecutablePath.TrimEnd('/'), normalized.NormalizedUrl, StringComparison.OrdinalIgnoreCase)))
        {
            IsFeedbackError = true;
            FeedbackMessage = $"Ya existe una tarjeta en Inicio para {normalized.Host}.";
            return null;
        }

        IsCreating = true;
        IsFeedbackError = false;
        FeedbackMessage = $"Creando tarjeta para {normalized.Host} y buscando su ícono...";

        try
        {
            string displayName = string.IsNullOrWhiteSpace(NameInput)
                ? normalized.SuggestedName
                : NameInput.Trim();

            string? faviconPath = await _iconFetcher.FetchFaviconAsync(normalized.NormalizedUrl);

            var tile = new AppTile
            {
                Name = displayName,
                ExecutablePath = normalized.NormalizedUrl,
                IconPath = faviconPath,
                AccentHex = SelectedAccentHex,
                FallbackGlyph = "\uE774",
                Category = "WebApp",
                LaunchType = LaunchType.Uri,
                Order = order
            };

            ResetForm();
            FeedbackMessage = $"Tarjeta WebApp '{displayName}' creada en Inicio.";
            return tile;
        }
        finally
        {
            IsCreating = false;
        }
    }
}
