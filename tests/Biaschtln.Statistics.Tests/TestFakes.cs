using Biaschtln.Statistics.Services;

namespace Biaschtln.Statistics.Tests;

/// <summary>Zählt Export-Aufrufe, ohne Datei-/Dialog-Nebenwirkungen — für VM-Tests.</summary>
internal sealed class NoopCsvExporter : ICsvExporter
{
    public int ExportCalls { get; private set; }

    public string? LastSuggestedName { get; private set; }

    public string RenderCsv<T>(IEnumerable<T> rows) => string.Empty;

    public void ExportCsv<T>(IEnumerable<T> rows, string suggestedName)
    {
        ExportCalls++;
        LastSuggestedName = suggestedName;
    }
}

/// <summary>Datei-Dialog-Fake, der immer denselben Pfad liefert (oder null bei "Abbruch").</summary>
internal sealed class FakeFileDialog : IFileDialogService
{
    private readonly string? _path;

    public FakeFileDialog(string? path) => _path = path;

    public IReadOnlyList<string>? OpenFiles(string filter, bool multiselect) =>
        _path is null ? null : [_path];

    public string? SaveFile(string filter, string defaultFileName) => _path;
}
