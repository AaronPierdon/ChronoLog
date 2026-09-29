using System;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using ChronoLog.Models;

namespace ChronoLog.Services;

/// <summary>
/// Loads/saves sources.config.json in the application's base directory (next to the exe), so
/// all sources, LogTypes, colors, and settings survive an app restart.
/// </summary>
public class ConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string ConfigPath { get; }

    public ConfigService(string? overridePath = null)
    {
        ConfigPath = overridePath ?? Path.Combine(AppContext.BaseDirectory, "sources.config.json");
    }

    public async Task<AppConfig> LoadAsync()
    {
        try
        {
            if (!File.Exists(ConfigPath)) return new AppConfig();

            using var stream = File.OpenRead(ConfigPath);
            var config = await JsonSerializer.DeserializeAsync<AppConfig>(stream, JsonOptions).ConfigureAwait(false);

            // Version cutover: the LogTypes/restructured-Source model (schema version 2) has no
            // equivalent shape for an older config file, so - per the app's current
            // early-development stage - an old or unrecognized version just starts fresh rather
            // than attempting a field-by-field migration. LogDatabase.Initialize() does the same
            // thing for the SQLite side (see its SchemaVersion check).
            if (config is null) return new AppConfig();

            // Version 2 -> 3 is the one real migration: the shape is identical, only the
            // meaning of LogType.IconGlyph changed (Segoe MDL2 codepoint -> vector icon key, see
            // Converters.IconGeometry), so remap it in place rather than discarding the user's
            // sources and LogTypes.
            if (config.Version == 2)
            {
                foreach (var logType in config.LogTypes)
                    logType.IconGlyph = LogType.MigrateLegacyIcon(logType.IconGlyph);
                config.Version = AppConfig.CurrentVersion;
            }

            if (config.Version != AppConfig.CurrentVersion) return new AppConfig();

            return config;
        }
        catch
        {
            // A corrupt or unreadable config shouldn't prevent the app from starting.
            return new AppConfig();
        }
    }

    public async Task SaveAsync(AppConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath) ?? AppContext.BaseDirectory);

        var tempPath = ConfigPath + ".tmp";
        using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, config, JsonOptions).ConfigureAwait(false);
        }

        File.Copy(tempPath, ConfigPath, overwrite: true);
        File.Delete(tempPath);
    }
}
