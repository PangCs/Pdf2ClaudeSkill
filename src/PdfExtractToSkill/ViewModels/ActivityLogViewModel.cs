using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PdfExtractToSkill.Application;
using PdfExtractToSkill.Application.Interfaces;

namespace PdfExtractToSkill.ViewModels;

public sealed partial class ActivityLogViewModel : ObservableObject
{
    private readonly IActivityLog _log;

    public ObservableCollection<LogEntry> Entries { get; } = new();

    [ObservableProperty]
    private string _watchedPath = string.Empty;

    public ActivityLogViewModel(IActivityLog log, IAppConfigRepository config)
    {
        _log = log;
        WatchedPath = config.Load().WatchedRootPath;

        foreach (var entry in log.Entries)
            Entries.Add(entry);

        log.EntryAdded += OnEntryAdded;
    }

    private void OnEntryAdded(object? sender, LogEntry entry)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() => Entries.Add(entry));
    }
}
