using Microsoft.Win32;

namespace Biaschtln.Statistics.Services;

/// <inheritdoc cref="IFileDialogService" />
public sealed class FileDialogService : IFileDialogService
{
    public IReadOnlyList<string>? OpenFiles(string filter, bool multiselect)
    {
        var dialog = new OpenFileDialog
        {
            Filter = filter,
            Multiselect = multiselect,
            CheckFileExists = true,
        };

        return dialog.ShowDialog() == true ? dialog.FileNames : null;
    }

    public string? SaveFile(string filter, string defaultFileName)
    {
        var dialog = new SaveFileDialog
        {
            Filter = filter,
            FileName = defaultFileName,
            AddExtension = true,
            OverwritePrompt = true,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
