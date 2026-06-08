using System.Windows;
using PdfExtractToSkill.ViewModels;

namespace PdfExtractToSkill.Views;

public partial class FirstRunWizard : Window
{
    public FirstRunWizard(FirstRunWizardViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.Completed += (_, _) =>
        {
            DialogResult = true;
            Close();
        };
    }
}
