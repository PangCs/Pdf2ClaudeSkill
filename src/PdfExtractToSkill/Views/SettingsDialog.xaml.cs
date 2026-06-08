using System.Windows;
using PdfExtractToSkill.ViewModels;

namespace PdfExtractToSkill.Views;

public partial class SettingsDialog : Window
{
    public SettingsDialog(SettingsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.CloseRequested += (_, _) => Close();
    }
}
