using System;
using System.Collections.Generic;

namespace ChronoLog.Models;

/// <summary>
/// A reusable "how do I parse this kind of log" definition - file format, timestamp profile,
/// and how it's shown (color/icon). Sources no longer own any of this directly: a Source is
/// just a named group of LogTypes (see <see cref="SourceLogType"/>), so the same LogType (e.g.
/// "Kepware") can be reused across many Source cards without re-configuring it each time.
/// </summary>
public class LogType
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name { get; set; } = string.Empty;

    public FileType Format { get; set; } = FileType.FlatText;

    public TimestampProfile TimestampProfile { get; set; } = new();

    /// <summary>Hex color string, e.g. "#4C9BFF" - used for the chip swatch, the collapsed-rail
    /// wedge, and (when the "Log line coloring" setting is set to LogType) row tinting.</summary>
    public string ColorHex { get; set; } = "#4C9BFF";

    /// <summary>A key into <see cref="Converters.IconGeometry"/>'s vector icon set (e.g.
    /// "gear") - NOT a font glyph. This used to hold a Segoe MDL2 Assets codepoint, but those
    /// were hand-transcribed from memory and, confirmed by the user's own screenshot of the
    /// running app, rendered as blank/invisible. Must be one of <see cref="IconChoices"/>'s keys
    /// (the editor only offers those), but nothing enforces that at the model level - an unknown
    /// key just falls back to the default icon (see IconGeometry.Resolve), never a blank one.</summary>
    public string IconGlyph { get; set; } = IconChoices[0].Key;

    public LogTypeDisplayMode DisplayMode { get; set; } = LogTypeDisplayMode.Both;

    public LogType Clone() => new()
    {
        Id = Id,
        Name = Name,
        Format = Format,
        TimestampProfile = TimestampProfile.Clone(),
        ColorHex = ColorHex,
        IconGlyph = IconGlyph,
        DisplayMode = DisplayMode
    };

    /// <summary>
    /// Curated set of vector icon keys (see <see cref="Converters.IconGeometry"/>) relevant to
    /// log sources/devices, offered in the LogType editor's icon picker. These used to be Segoe
    /// MDL2 Assets glyph characters; they're plain string keys now, resolved to hand-authored
    /// Path geometry instead of a font, so there's no font-availability or codepoint-transcription
    /// risk - see IconGeometry's class comment for the full story.
    /// Exposed as <see cref="IconChoice"/> records (real properties) rather than a value tuple,
    /// because WPF data binding can't see value-tuple fields - "{Binding Key}" in the icon
    /// picker would silently bind to nothing.
    /// </summary>
    public static readonly IReadOnlyList<IconChoice> IconChoices = new IconChoice[]
    {
        new("gear", "Settings"),
        new("warning", "Warning"),
        new("pencil", "Edit"),
        new("plus", "Add"),
        new("cross", "Cancel"),
        new("refresh", "Refresh"),
        new("globe", "Globe"),
        new("home", "Home"),
        new("calendar", "Calendar"),
        new("clock", "Recent"),
        new("cloud", "Cloud"),
        new("manage", "Manage"),
        new("chip", "Chip"),
        new("star", "Important"),
        new("print", "Print"),
        new("reportdocument", "ReportDocument"),
        new("comment", "Comment"),
        new("settile", "SetTile"),
        new("certificate", "Certificate"),
        new("streaming", "Streaming"),
        new("monitor", "DeviceMonitor"),
        new("calendarweek", "CalendarWeek"),
        new("redeye", "RedEye"),
        new("tag", "Tag")
    };

    /// <summary>Maps each legacy Segoe MDL2 Assets codepoint (what IconGlyph held up to config
    /// version 2 / database schema version 2) to its vector icon key - same 24 icons, same
    /// order. Used only to migrate existing config/database files forward (see
    /// ConfigService.LoadAsync and LogDatabase.Initialize) instead of discarding them.</summary>
    public static readonly IReadOnlyDictionary<string, string> LegacyGlyphToIconKey = new Dictionary<string, string>
    {
        ["\uE713"] = "gear", // Settings
        ["\uE7BA"] = "warning", // Warning
        ["\uE70F"] = "pencil", // Edit
        ["\uE710"] = "plus", // Add
        ["\uE711"] = "cross", // Cancel
        ["\uE72C"] = "refresh", // Refresh
        ["\uE774"] = "globe", // Globe
        ["\uE80F"] = "home", // Home
        ["\uE787"] = "calendar", // Calendar
        ["\uE81C"] = "clock", // Recent
        ["\uE753"] = "cloud", // Cloud
        ["\uE7C3"] = "manage", // Manage
        ["\uE964"] = "chip", // Chip
        ["\uE7F4"] = "star", // Important
        ["\uE749"] = "print", // Print
        ["\uE9F9"] = "reportdocument", // ReportDocument
        ["\uE90A"] = "comment", // Comment
        ["\uE9E9"] = "settile", // SetTile
        ["\uEB95"] = "certificate", // Certificate
        ["\uE968"] = "streaming", // Streaming
        ["\uE7C4"] = "monitor", // DeviceMonitor
        ["\uE8BF"] = "calendarweek", // CalendarWeek
        ["\uE7B3"] = "redeye", // RedEye
        ["\uE8EC"] = "tag", // Tag
    };

    /// <summary>Converts a legacy glyph to its icon key; anything already a key (or unknown)
    /// passes through unchanged - IconGeometry.Resolve falls back to the default icon for an
    /// unknown key anyway.</summary>
    public static string MigrateLegacyIcon(string? value) =>
        value is not null && LegacyGlyphToIconKey.TryGetValue(value, out var key) ? key : value ?? IconChoices[0].Key;
}

/// <summary>How a LogType is rendered wherever it's shown as a chip/wedge: color swatch, icon
/// glyph, or both together. Chips get larger when Both is selected (icon over/next to swatch).</summary>
public enum LogTypeDisplayMode
{
    Color,
    Icon,
    Both
}

/// <summary>One entry in <see cref="LogType.IconChoices"/>: a vector icon key (see
/// Converters.IconGeometry) and its human-readable label for the picker's tooltip.</summary>
public sealed record IconChoice(string Key, string Label);
