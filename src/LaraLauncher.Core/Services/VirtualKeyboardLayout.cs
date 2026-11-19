namespace LaraLauncher.Core.Services;

public enum VirtualKeyKind
{
    Character,
    Snippet,
    Space,
    Backspace,
    Clear,
    ToggleCase,
    Done
}

public sealed record VirtualKeyDefinition(
    string LowerLabel,
    string UpperLabel,
    string Value,
    VirtualKeyKind Kind)
{
    public string GetDisplayLabel(bool isUpperCase) => isUpperCase ? UpperLabel : LowerLabel;
}

/// <summary>
/// Pure 5x10 Roku-style virtual keyboard definition and text mutation logic
/// designed for effortless D-Pad / remote / gamepad URL and title input.
/// </summary>
public static class VirtualKeyboardLayout
{
    public const int Columns = 10;

    public static IReadOnlyList<VirtualKeyDefinition> Keys { get; } = BuildKeys();

    public static string ApplyKey(string currentText, VirtualKeyDefinition key, bool isUpperCase)
    {
        string text = currentText ?? string.Empty;

        return key.Kind switch
        {
            VirtualKeyKind.Character => text + (isUpperCase ? key.UpperLabel : key.LowerLabel),
            VirtualKeyKind.Snippet => text + key.Value,
            VirtualKeyKind.Space => text + " ",
            VirtualKeyKind.Backspace => text.Length > 0 ? text[..^1] : string.Empty,
            VirtualKeyKind.Clear => string.Empty,
            _ => text
        };
    }

    private static IReadOnlyList<VirtualKeyDefinition> BuildKeys()
    {
        var list = new List<VirtualKeyDefinition>(50);

        // Row 0: Digits (10)
        AddChars(list, "1234567890");

        // Row 1: QWERTY (10)
        AddChars(list, "qwertyuiop");

        // Row 2: ASDF + dash (10)
        AddChars(list, "asdfghjkl-");

        // Row 3: ZXCV + URL punctuation (10)
        AddChars(list, "zxcvbnm./:");

        // Row 4: Case + URL shortcuts + Actions (10)
        list.Add(new("Mayús", "MAYÚS", string.Empty, VirtualKeyKind.ToggleCase));
        list.Add(new("www.", "www.", "www.", VirtualKeyKind.Snippet));
        list.Add(new(".com", ".com", ".com", VirtualKeyKind.Snippet));
        list.Add(new(".cl", ".cl", ".cl", VirtualKeyKind.Snippet));
        list.Add(new(".tv", ".tv", ".tv", VirtualKeyKind.Snippet));
        list.Add(new("_", "_", "_", VirtualKeyKind.Character));
        list.Add(new("Espacio", "Espacio", " ", VirtualKeyKind.Space));
        list.Add(new("⌫ Borrar", "⌫ Borrar", string.Empty, VirtualKeyKind.Backspace));
        list.Add(new("Limpiar", "Limpiar", string.Empty, VirtualKeyKind.Clear));
        list.Add(new("Listo ✓", "Listo ✓", string.Empty, VirtualKeyKind.Done));

        return list;
    }

    private static void AddChars(List<VirtualKeyDefinition> list, string chars)
    {
        foreach (char c in chars)
        {
            string lower = c.ToString();
            string upper = char.ToUpperInvariant(c).ToString();
            list.Add(new VirtualKeyDefinition(lower, upper, lower, VirtualKeyKind.Character));
        }
    }
}
