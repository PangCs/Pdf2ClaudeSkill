using Microsoft.Win32;
using PdfExtractToSkill.Application.Interfaces;

namespace PdfExtractToSkill.Infrastructure.Config;

public sealed class StartupRegistrar : IStartupRegistrar
{
    private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "PdfExtractToSkill";

    private readonly string _registryKeyPath;
    private readonly string _valueName;

    public StartupRegistrar()
        : this(RunKey, ValueName) { }

    internal StartupRegistrar(string registryKeyPath, string valueName)
    {
        _registryKeyPath = registryKeyPath;
        _valueName = valueName;
    }

    public void Register(string exePath)
    {
        using var key = OpenWritable();
        key.SetValue(_valueName, exePath);
    }

    public void Unregister()
    {
        using var key = OpenWritable();
        key.DeleteValue(_valueName, throwOnMissingValue: false);
    }

    public bool IsRegistered()
    {
        using var key = Registry.CurrentUser.OpenSubKey(_registryKeyPath);
        return key?.GetValue(_valueName) is not null;
    }

    private RegistryKey OpenWritable() =>
        Registry.CurrentUser.CreateSubKey(_registryKeyPath, writable: true);
}
