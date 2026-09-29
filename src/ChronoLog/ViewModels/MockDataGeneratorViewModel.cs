using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ChronoLog.Common;
using ChronoLog.Models;
using ChronoLog.Services;
using Microsoft.Win32;

namespace ChronoLog.ViewModels;

/// <summary>
/// Backs the Settings window's Developer tab - "Generate Mock Log Files". Every bindable
/// property here maps onto a MockDataOptions field; GenerateAsync hands them to
/// MockLogGeneratorService, which does the actual work. Generated files have no special
/// integration with the app's own ingestion pipeline - they're just normal files afterward, to
/// be dragged onto the main window's drop zone (or added via "+ Add Source" / "Manage Log
/// Types") exactly like any real log file.
/// </summary>
public class MockDataGeneratorViewModel : ObservableObject
{
    private MockOutputFormat _outputFormat = MockOutputFormat.FlatText;
    private MockWildness _wildness = MockWildness.Standard;
    private MockUsagePreset _usagePreset = MockUsagePreset.SimpleExample;
    private int _fileCount = 3;
    private int _recordsPerFile = 250;
    private string _outputFolder;
    private string _statusMessage = "Click \"Generate Mock Log Files\" to write files to the folder below.";
    private bool _isGenerating;
    private bool _lastRunSucceeded;

    public MockOutputFormat OutputFormat { get => _outputFormat; set => SetProperty(ref _outputFormat, value); }
    public MockWildness Wildness { get => _wildness; set => SetProperty(ref _wildness, value); }
    public MockUsagePreset UsagePreset { get => _usagePreset; set => SetProperty(ref _usagePreset, value); }

    /// <summary>Text-box-friendly view of FileCount. The XAML binds this with
    /// UpdateSourceTrigger=LostFocus (not PropertyChanged) so nothing commits or clamps
    /// mid-keystroke - typing "15" no longer risks the box re-rendering itself between the "1"
    /// and the "5". The FileCount setter below explicitly re-raises this property's change
    /// notification whenever the clamp actually changes the value, so if you tab away after
    /// typing e.g. "500" the box snaps to show the real clamped "50" instead of silently
    /// disagreeing with what's stored - that silent disagreement (type a too-large number, see
    /// it clamped, but the box never says so) was the "keeps going to 50" glitch.</summary>
    public string FileCountText
    {
        get => _fileCount.ToString();
        set { if (int.TryParse(value, out var n)) FileCount = n; }
    }

    public int FileCount
    {
        get => _fileCount;
        set { if (SetProperty(ref _fileCount, Math.Clamp(value, 1, 50))) OnPropertyChanged(nameof(FileCountText)); }
    }

    /// <summary>See the FileCountText remarks above - same LostFocus-commit / re-sync-on-clamp
    /// pattern.</summary>
    public string RecordsPerFileText
    {
        get => _recordsPerFile.ToString();
        set { if (int.TryParse(value, out var n)) RecordsPerFile = n; }
    }

    public int RecordsPerFile
    {
        get => _recordsPerFile;
        set { if (SetProperty(ref _recordsPerFile, Math.Clamp(value, 5, 200_000))) OnPropertyChanged(nameof(RecordsPerFileText)); }
    }

    public string OutputFolder { get => _outputFolder; set => SetProperty(ref _outputFolder, value); }

    public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }
    public bool IsGenerating { get => _isGenerating; private set => SetProperty(ref _isGenerating, value); }
    public bool LastRunSucceeded { get => _lastRunSucceeded; private set => SetProperty(ref _lastRunSucceeded, value); }

    public ICommand BrowseFolderCommand { get; }
    public AsyncRelayCommand GenerateCommand { get; }
    public ICommand OpenOutputFolderCommand { get; }

    public MockDataGeneratorViewModel()
    {
        _outputFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ChronoLog", "MockLogs");

        BrowseFolderCommand = new RelayCommand(BrowseFolder);
        GenerateCommand = new AsyncRelayCommand(GenerateAsync);
        OpenOutputFolderCommand = new RelayCommand(OpenOutputFolder, () => LastRunSucceeded && Directory.Exists(OutputFolder));
    }

    private void BrowseFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choose where to write mock log files",
            InitialDirectory = Directory.Exists(OutputFolder)
                ? OutputFolder
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dialog.ShowDialog() == true)
        {
            OutputFolder = dialog.FolderName;
        }
    }

    private async Task GenerateAsync()
    {
        IsGenerating = true;
        StatusMessage = "Generating...";
        try
        {
            var options = new MockDataOptions
            {
                OutputFormat = OutputFormat,
                Wildness = Wildness,
                UsagePreset = UsagePreset,
                FileCount = FileCount,
                RecordsPerFile = RecordsPerFile,
                OutputFolder = OutputFolder
            };

            var result = await MockLogGeneratorService.GenerateAsync(options).ConfigureAwait(true);
            var names = string.Join(", ", result.Files.Select(f => f.ServerName));

            StatusMessage = result.InjectedDefectCount > 0
                ? $"Generated {result.Files.Count} file(s), {result.TotalRecords:N0} records total ({result.InjectedDefectCount} intentional defect(s) mixed in), as: {names}. Drag them onto the main window (or use \"+ Add Source\") to try them out."
                : $"Generated {result.Files.Count} file(s), {result.TotalRecords:N0} records total, as: {names}. Drag them onto the main window (or use \"+ Add Source\") to try them out.";
            LastRunSucceeded = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Generation failed: {ex.Message}";
            LastRunSucceeded = false;
        }
        finally
        {
            IsGenerating = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private void OpenOutputFolder()
    {
        if (!Directory.Exists(OutputFolder)) return;
        Process.Start(new ProcessStartInfo { FileName = OutputFolder, UseShellExecute = true });
    }
}
