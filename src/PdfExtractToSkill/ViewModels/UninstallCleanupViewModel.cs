using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfExtractToSkill.Application.Interfaces;

namespace PdfExtractToSkill.ViewModels;

public sealed partial class UninstallCleanupViewModel : ObservableObject
{
    private readonly IUninstallHelper _helper;

    public string ConfigFolderPath => _helper.ConfigFolderPath;

    [ObservableProperty]
    private bool _deleteConfig = false;

    public event EventHandler<bool>? CloseRequested;

    public UninstallCleanupViewModel(IUninstallHelper helper)
    {
        _helper = helper;
    }

    [RelayCommand]
    private void CleanUp()
    {
        _helper.RemoveAutostart();
        if (DeleteConfig)
            _helper.DeleteConfigFolder();
        CloseRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);
}
