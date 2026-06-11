using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using PdfExtractToSkill.ViewModels;

namespace PdfExtractToSkill.Views;

public partial class ActivityLogWindow : Window
{
    public ActivityLogWindow(ActivityLogViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.Entries.CollectionChanged += ScrollToBottom;
    }

    protected override void OnClosed(EventArgs e)
    {
        ((ActivityLogViewModel)DataContext).Dispose();
        base.OnClosed(e);
    }

    private void ScrollToBottom(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (LogList.Items.Count == 0) return;
        LogList.ScrollIntoView(LogList.Items[^1]);
    }
}
