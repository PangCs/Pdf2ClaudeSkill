using System.Windows;
using PdfExtractToSkill.ViewModels;

namespace PdfExtractToSkill.Views;

public partial class UninstallCleanupDialog : Window
{
    public UninstallCleanupDialog(UninstallCleanupViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.CloseRequested += (_, cleaned) =>
        {
            DialogResult = cleaned;
            Close();
        };
    }
}
