using SkiaSharp;

namespace Laraue.Apps.Blog.ApiHost.Services;

/// <summary>
/// Generates Open Graph preview images for blog articles.
///
/// NuGet packages required:
///   SkiaSharp                        (>= 2.88)
///   SkiaSharp.NativeAssets.Linux     (for Linux / Docker deployments)
///
/// Optional – embed Montserrat and Source Sans 3 for pixel-perfect branding:
///   Place .ttf files in wwwroot/fonts/ and set FontDirectory in appsettings.json.
///   Falls back to the system sans-serif when the folder is absent.
/// </summary>
public static class OgImageGenerator
{
    // ── Canvas dimensions ────────────────────────────────────────────────────
    private const int Width  = 1200;
    private const int Height = 630;

    // ── Spacing ──────────────────────────────────────────────────────────────
    private const float PadX         = 80f;
    private const float PadTop       = 72f;
    private const float PadBottom    = 60f;
    private const float AccentBarH   = 5f;

    // ── Brand colours (matches :root CSS vars) ───────────────────────────────
    private static readonly SKColor Paper       = SKColor.Parse("#f7f4ee"); // --paper
    private static readonly SKColor Cream       = SKColor.Parse("#ede9e0"); // --cream
    private static readonly SKColor Ink         = SKColor.Parse("#0f0e0c"); // --ink
    private static readonly SKColor Accent      = SKColor.Parse("#c84b2f"); // --accent
    private static readonly SKColor Muted       = SKColor.Parse("#7a7469"); // --muted
    private static readonly SKColor Border      = SKColor.Parse("#d9d4c9"); // --border

    // ── Font sizes ───────────────────────────────────────────────────────────
    private const float SiteNameSize   = 24f;
    private const float TitleSize      = 52f;
    private const float DescSize       = 26f;

    // ── Optional font directory (set via DI / appsettings) ───────────────────
    public static string? FontDirectory { get; set; }

    /// <summary>Renders an OG image and returns the raw PNG bytes.</summary>
    public static byte[] Generate(string siteName, string title, string description)
    {
        var info = new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;

        Draw(canvas, siteName, title, description);

        using var image = surface.Snapshot();
        using var data  = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    // ── Drawing ──────────────────────────────────────────────────────────────

    private static void Draw(SKCanvas canvas, string siteName, string title, string description)
    {
        // Background: --paper
        canvas.Clear(Paper);

        // Bottom strip in --cream to give depth
        using var stripPaint = new SKPaint { Color = Cream };
        canvas.DrawRect(0, Height - 100, Width, 100, stripPaint);

        // Thin bottom border
        using var borderPaint = new SKPaint { Color = Border, StrokeWidth = 1 };
        canvas.DrawLine(0, Height - 100, Width, Height - 100, borderPaint);

        // Left accent bar (--accent, vertical)
        using var accentPaint = new SKPaint { Color = Accent };
        canvas.DrawRect(0, 0, AccentBarH, Height, accentPaint);

        float y = PadTop;

        // ── Site name ────────────────────────────────────────────────────────
        using var siteNamePaint = MakePaint(Accent, SiteNameSize, bold: true, serif: true);
        canvas.DrawText(siteName.ToUpperInvariant(), PadX, y + SiteNameSize, siteNamePaint);
        y += SiteNameSize + 20;

        // Horizontal rule under site name
        using var rulePaint = new SKPaint { Color = Border, StrokeWidth = 1.5f };
        canvas.DrawLine(PadX, y, Width - PadX, y, rulePaint);
        y += 40;

        // ── Title ────────────────────────────────────────────────────────────
        using var titlePaint = MakePaint(Ink, TitleSize, bold: true, serif: true);
        float titleBottom = DrawWrapped(canvas, title, titlePaint,
            PadX, y, maxWidth: Width - PadX * 2,
            lineHeight: TitleSize * 1.22f, maxLines: 3);
        y = titleBottom + 28;

        // ── Description ──────────────────────────────────────────────────────
        float descMaxH  = (Height - PadBottom - 100) - y;
        int   descLines = Math.Max(1, (int)(descMaxH / (DescSize * 1.55f)));
        using var descPaint = MakePaint(Muted, DescSize, bold: false, serif: false);
        DrawWrapped(canvas, description, descPaint,
            PadX, y, maxWidth: Width - PadX * 2,
            lineHeight: DescSize * 1.55f, maxLines: descLines);

        // ── Domain label — right-aligned in cream strip, accent colour ────────
        const string domain = "LARAUE.COM";
        using var domainPaint = MakePaint(Accent, 20f, bold: true, serif: false);
        // Add letter-spacing manually by measuring and offsetting
        float domainW    = domainPaint.MeasureText(domain);
        float domainX    = Width - PadX - domainW;
        float domainY    = Height - PadBottom + 10;
        canvas.DrawText(domain, domainX, domainY, domainPaint);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static float DrawWrapped(
        SKCanvas canvas, string text, SKPaint paint,
        float x, float y, float maxWidth, float lineHeight, int maxLines)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var cur   = string.Empty;

        foreach (var word in words)
        {
            if (lines.Count >= maxLines) break;
            var candidate = cur.Length == 0 ? word : cur + " " + word;
            if (paint.MeasureText(candidate) <= maxWidth) { cur = candidate; }
            else { if (cur.Length > 0) lines.Add(cur); cur = word; }
        }
        if (cur.Length > 0 && lines.Count < maxLines) lines.Add(cur);

        // Ellipsis on last line if text was trimmed
        bool trimmed = string.Join(" ", lines) != text.Trim();
        if (trimmed && lines.Count > 0)
        {
            var last = lines[^1];
            while (last.Length > 0 && paint.MeasureText(last + "…") > maxWidth)
                last = last.Contains(' ') ? last[..last.LastIndexOf(' ')].TrimEnd() : last[..^1];
            lines[^1] = last + "…";
        }

        float baseline = y + paint.TextSize;
        foreach (var line in lines)
        {
            canvas.DrawText(line, x, baseline, paint);
            baseline += lineHeight;
        }
        return baseline - lineHeight; // bottom of last drawn line
    }

    private static SKPaint MakePaint(SKColor color, float size, bool bold, bool serif)
    {
        SKTypeface? face = null;

        if (!string.IsNullOrEmpty(FontDirectory))
        {
            // Try to load embedded brand fonts
            var file = serif
                ? Path.Combine(FontDirectory, bold ? "Montserrat-Bold.ttf" : "Montserrat-Regular.ttf")
                : Path.Combine(FontDirectory, bold ? "SourceSans3-SemiBold.ttf" : "SourceSans3-Regular.ttf");

            if (File.Exists(file))
                face = SKTypeface.FromFile(file);
        }

        face ??= SKTypeface.FromFamilyName(
            serif ? "Georgia" : "sans-serif",
            bold ? SKFontStyle.Bold : SKFontStyle.Normal);

        return new SKPaint
        {
            Color       = color,
            TextSize    = size,
            IsAntialias = true,
            Typeface    = face,
        };
    }
}