using System.Windows;
using PdfExtractToSkill.ViewModels;

namespace PdfExtractToSkill.Views;

public partial class ConfirmationDialog : Window
{
    public ConfirmationResult? Result { get; private set; }

    public ConfirmationDialog(ConfirmationDialogViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.CloseRequested += (_, result) =>
        {
            Result = result;
            DialogResult = result is not null;
            Close();
        };
    }
}
