using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PdfExtractToSkill.Application.Interfaces;

namespace PdfExtractToSkill.ViewModels;

public sealed partial class ConfirmationDialogViewModel : ObservableObject
{
    private static readonly Regex KebabCaseRegex = new(@"^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    private readonly ISkillInstaller _installer;

    public event EventHandler<ConfirmationResult?>? CloseRequested;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string _skillName = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private bool _skillAlreadyExists;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private bool _overwriteConfirmed;

    public ConfirmationDialogViewModel(ISkillInstaller installer, ISkillNameDeriver deriver, string pdfPath)
    {
        _installer = installer;
        SkillName = deriver.DeriveFrom(System.IO.Path.GetFileName(pdfPath));
        Description = $"Reference document extracted from {System.IO.Path.GetFileName(pdfPath)}";
    }

    partial void OnSkillNameChanged(string value)
    {
        SkillAlreadyExists = !string.IsNullOrEmpty(value) && _installer.Exists(value);
        ConfirmCommand.NotifyCanExecuteChanged();
    }

    partial void OnSkillAlreadyExistsChanged(bool value) =>
        ConfirmCommand.NotifyCanExecuteChanged();

    private bool CanConfirm() =>
        IsValidKebabCase(SkillName) && (!SkillAlreadyExists || OverwriteConfirmed);

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private void Confirm() =>
        CloseRequested?.Invoke(this, new ConfirmationResult(SkillName, Description, OverwriteConfirmed));

    [RelayCommand]
    private void Cancel() =>
        CloseRequested?.Invoke(this, null);

    public static bool IsValidKebabCase(string? value) =>
        !string.IsNullOrEmpty(value) && KebabCaseRegex.IsMatch(value);
}

public sealed record ConfirmationResult(string SkillName, string Description, bool Overwrite);
