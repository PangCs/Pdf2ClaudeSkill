; Inno Setup script for PDF Extract to Skill
; Requires Inno Setup 6 — https://jrsoftware.org/isinfo.php
; Build via: .\build-installer.ps1  (runs dotnet publish first, then ISCC)

#define AppName    "PDF Extract to Skill"
#define AppVersion "1.0.0.0"
#define AppPublisher "PangCs"
#define AppExe     "PdfExtractToSkill.exe"
#define PublishDir "src\PdfExtractToSkill.Packaging\bin\x64\Release\publish"

[Setup]
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=installer-output
OutputBaseFilename=PdfExtractToSkill-{#AppVersion}-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
; Windows 10 1809+ (same requirement as the MSIX)
MinVersion=10.0.17763
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
; Uncomment and point to a .ico file to brand the setup wizard:
; SetupIconFile=src\PdfExtractToSkill.Packaging\Assets\app.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
; All self-contained publish output (exe, dlls, runtimes, extract.py, etc.)
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}";            Filename: "{app}\{#AppExe}"
Name: "{group}\Uninstall {#AppName}";  Filename: "{uninstallexe}"
Name: "{commondesktop}\{#AppName}";    Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent
