using System;
using System.Collections.Generic;
using System.Text;
using Godot;

namespace RabbitWolfTiger.Tests.Localization;

/// <summary>Checks actual control theme fonts and loaded glyphs, not system-font fallback availability.</summary>
public static class ChineseUiChecks
{
    private static IEnumerable<Node> Walk(Node root)
    {
        yield return root;
        foreach (Node child in root.GetChildren())
            foreach (Node descendant in Walk(child)) yield return descendant;
    }
    public static int DeclaredGlyphs(string packagedFontPath)
    {
        Font font = GD.Load<Font>(packagedFontPath);
        int count = 0;
        foreach (Rune rune in Godot.FileAccess.GetFileAsString("res://resources/fonts/ui_glyphs.txt").EnumerateRunes())
        {
            if (Rune.IsControl(rune) || Rune.IsWhiteSpace(rune)) continue;
            if (!font.HasChar(rune.Value)) throw new InvalidOperationException($"Declared UI glyph missing from bundled font: U+{rune.Value:X} '{rune}'.");
            count++;
        }
        if (count < 200) throw new InvalidOperationException("Declared Chinese glyph inventory is unexpectedly incomplete.");
        return count;
    }
    public static int Run(Node root, string packagedFontPath)
    {
        Font packaged = GD.Load<Font>(packagedFontPath);
        if (packaged == null) throw new InvalidOperationException("Bundled Chinese font did not load: " + packagedFontPath);
        int count = 0;
        foreach (Node node in Walk(root))
        {
            string? text = node switch { Label label => label.Text, Button button => button.Text, _ => null };
            if (text == null || node is not Control control || !control.Visible) continue;
            Font actual = control.GetThemeFont("font");
            Font baseFont = actual is FontVariation variation ? variation.BaseFont : actual;
            if (baseFont.ResourcePath != packagedFontPath)
                throw new InvalidOperationException($"Control '{control.Name}' is not using the bundled font: '{baseFont.ResourcePath}', text='{text}'.");
            count++;
            foreach (Rune rune in text.EnumerateRunes())
            {
                if (Rune.IsWhiteSpace(rune) || Rune.IsControl(rune)) continue;
                if (!packaged.HasChar(rune.Value) || !actual.HasChar(rune.Value))
                    throw new InvalidOperationException($"Missing bundled/active glyph U+{rune.Value:X} '{rune}' in '{text}'.");
                count++;
            }
        }
        return count;
    }
}
