# PRD: PdfExtractToSkill — Background Watcher Service

**Status:** Draft  
**Date:** 2026-06-08  
**Project:** `C:\Users\CheeShanPang\source\repos\PdfExtractToSkill`

---

## Problem Statement

When working with technical specification PDFs (e.g. SEMI GEM300 specs, Rorze robot/EFEM communication specs), the user must manually discover new PDFs, remember to extract them with PyMuPDF, decide on skill names, write SKILL.md files, and install them into Claude Code's global skills directory. This is a multi-step, error-prone manual process that breaks flow — especially when new PDFs arrive incrementally over time and the user only notices them later when they need answers that the skill could have already provided.

Beyond the day-to-day friction, there is also a setup problem: the extraction script is hardcoded, Python and PyMuPDF must be separately installed, and the app itself has no distribution mechanism — making it hard to set up on a new machine or hand off to a colleague.

There is no automated handoff between "PDF arrives on disk" and "Claude Code skill is ready to answer questions from it," and no straightforward way to get the tool running in the first place.

---

## Solution

A persistent Windows system tray application that watches a user-configured root folder for newly dropped PDF files. When a new PDF is detected, a Windows Toast notification appears in the bottom-right corner. Clicking the notification opens a confirmation dialog where the user can review and edit the auto-generated skill name and description before confirming extraction. The app then calls the bundled Python extraction script (PyMuPDF), writes the `.txt` output to a configured output folder, and installs a Claude Code `SKILL.md` into the global skills directory — completing the entire pipeline without the user leaving their current task.

The application is distributed as an **MSIX package** that handles installation, uninstallation, prerequisite checking, and AUMID registration cleanly. A first-run configuration wizard guides the user through initial setup so the watcher is ready to use immediately after install.

---

## User Stories

### Installer & First Run

1. As a user, I want a single installable package (`.msix` or setup file) for the app, so that I can get up and running without manual xcopy or registry edits.
2. As a user, I want the installer to check whether Python is installed before completing setup, so that I know upfront if I need to install Python first.
3. As a user, I want the installer to automatically install the `pymupdf` Python package (via `pip`) if it is not already present, so that I don't have to remember to run a separate pip command.
4. As a user, I want the installer to inform me clearly if the `pip install pymupdf` step fails, so that I can resolve the issue manually and know the extraction pipeline won't work until I do.
5. As a user, I want the extraction Python script to be bundled inside the installation directory, so that the app works out of the box without me locating or copying the script separately.
6. As a user, I want a Start Menu shortcut created during installation, so that I can launch the app without navigating to the installation directory.
7. As a user, I want a first-run configuration wizard to open automatically after installation completes, so that I can set the watched root path and output path before the watcher starts.
8. As a user, I want the installer to register the app's AUMID so that Windows Toast notifications work immediately after install, without me needing to configure anything manually.
9. As a user, I want to uninstall the app cleanly via Windows Settings → Apps, so that no orphaned files or registry entries are left behind.
10. As a user, I want the uninstaller to optionally remove the `%APPDATA%\PdfExtractToSkill\` config folder, with a clear prompt during uninstall, so that I can choose to preserve or delete my settings.
11. As a user, I want the uninstaller to leave my extracted `.txt` files and installed Claude Code skills untouched, so that removing the app does not destroy work I've already produced.
12. As a user, I want the app to detect when it is already installed and offer an upgrade path rather than a parallel installation, so that I don't end up with two copies running simultaneously.

### Runtime — System Tray & Configuration

13. As a user, I want the app to appear as a system tray icon when running, so that it stays out of my way while monitoring continuously in the background.
14. As a user, I want the app to start automatically when Windows boots, so that I never have to remember to launch it before dropping PDFs.
15. As a user, I want to be able to toggle autostart on or off from the Settings dialog, so that I can choose when monitoring is active.
16. As a user, I want to configure the watched root path via a right-click Settings dialog on the tray icon, so that I can point it at any folder without editing config files.
17. As a user, I want to configure the output path for extracted `.txt` files via the Settings dialog, so that extracted files are organised where I expect them.
18. As a user, I want the app to auto-detect the Python executable from my PATH, so that I don't need to manually configure it in most cases.
19. As a user, I want to be able to manually override the Python executable path in Settings, so that I can use a specific virtual environment or non-default Python installation.
20. As a user, I want the Settings dialog to validate that required fields (root path, output path) are filled before allowing the watcher to start, so that I get a clear error rather than a silent failure.
21. As a user, I want Settings to persist across restarts, so that I only configure the app once.
22. As a user, I want a "Check prerequisites" action in the Settings dialog that re-validates Python and PyMuPDF, so that I can confirm the environment is healthy after making changes.

### Notifications

23. As a user, I want a Windows Toast notification to appear in the bottom-right corner when a new PDF is dropped into the watched folder, so that I am informed immediately without switching windows.
24. As a user, I want the notification to show the filename of the detected PDF, so that I know which file triggered it.
25. As a user, I want the notification to offer an "Extract Now" action button, so that I can initiate extraction with a single click.
26. As a user, I want the notification to offer a "Dismiss" button, so that I can skip extraction for PDFs I don't want to process.
27. As a user, I want a completion Toast notification to appear when extraction and skill installation finish, so that I know the skill is ready to use without checking manually.
28. As a user, I want the completion notification to name the installed skill, so that I can immediately invoke it in Claude Code.
29. As a user, I want an error Toast notification if extraction fails, with a brief reason, so that I can diagnose and retry without digging through logs.

### Confirmation Dialog

30. As a user, I want clicking "Extract Now" to open a confirmation dialog, so that I can review and adjust extraction settings before committing.
31. As a user, I want the confirmation dialog to pre-fill the skill name from the PDF filename in kebab-case, so that I have a sensible starting point without typing from scratch.
32. As a user, I want to edit the skill name in the confirmation dialog before extraction runs, so that I can give it a meaningful, memorable name.
33. As a user, I want the confirmation dialog to show a large, scrollable text area for the skill description, so that I can write or review a detailed description of what the skill covers.
34. As a user, I want the skill description to be pre-filled with an auto-generated default based on the document title extracted from the PDF, so that I have useful starting content even if I don't customise it.
35. As a user, I want the confirmation dialog to warn me if a skill with the entered name already exists, so that I can decide whether to overwrite or rename before it's too late.
36. As a user, I want an "Overwrite" checkbox in the confirmation dialog when a name conflict is detected, so that I can explicitly approve overwriting without a separate prompt.
37. As a user, I want to be able to cancel from the confirmation dialog without running extraction, so that I can defer processing or dismiss accidental drops.

### Extraction Pipeline

38. As a user, I want extraction to run silently in the background after I confirm, so that it doesn't block me from other work.
39. As a user, I want multiple PDFs dropped simultaneously to be queued and processed one at a time, so that confirmation dialogs don't pile up and overwhelm me.
40. As a user, I want each queued PDF to get its own notification and confirmation dialog in sequence, so that I can review each skill name and description individually.
41. As a user, I want the watcher to monitor only the top level of the configured root path (not subfolders), so that PDFs I intentionally place in subfolders for archival don't trigger false notifications.
42. As a user, I want extracted `.txt` files to follow the same header format as the existing SEMI and Rorze extraction pipeline (filename, version, source, date, page markers), so that existing skill lookup logic works without changes.
43. As a user, I want skills to be installed globally into `~/.claude/skills/<name>/SKILL.md`, so that they are available across all Claude Code sessions without project-specific configuration.
44. As a user, I want the installed SKILL.md to follow the same template as existing Rorze and SEMI skills (expert persona, config section, reference table, lookup workflow, answer format), so that skill behaviour is consistent.
45. As a user, I want the installed skill's config file (`~/.claude/skills/<name>-config`) to be written automatically pointing to the output folder, so that the skill works immediately without a manual first-use setup step.

---

## Implementation Decisions

### Module Breakdown

**InstallerPackage**
- Distributed as an MSIX package built via a Windows Application Packaging Project in Visual Studio.
- MSIX is chosen over Inno Setup because it: (a) automatically registers the Application User Model ID (AUMID) required for Windows Toast notifications to function correctly, (b) provides clean install/uninstall with no registry cruft, and (c) supports per-user installation without UAC elevation.
- Bundles: the WPF executable, the generalised Python extraction script (`extract.py`), and all .NET runtime dependencies (self-contained publish).
- The Python extraction script is installed to the app's installation directory alongside the executable.
- Creates a Start Menu shortcut.
- Post-install, launches the app with a `--first-run` flag to trigger the configuration wizard.

**PrerequisiteChecker**
- Run by the installer (post-install action) and on-demand from the Settings dialog.
- Checks: Python executable found on PATH (or at the configured override path), `pymupdf` importable by that Python.
- If `pymupdf` is missing, runs `pip install pymupdf` using the detected Python executable and reports success or failure.
- Interface: `CheckAll() → PrerequisiteReport { PythonFound, PythonPath, PyMuPdfInstalled, PyMuPdfInstallError }`.

**FirstRunWizard**
- A small WPF dialog shown automatically when the app launches with `--first-run` or when config is absent.
- Steps: (1) Python status summary from `PrerequisiteChecker`, (2) browse for watched root path, (3) browse for output path, (4) confirm and start watching.
- On completion writes the initial `AppConfig` via `AppConfigRepository`.

**FolderWatcherService**
- Wraps `FileSystemWatcher` targeting the configured root path, top-level only (no subdirectory recursion).
- Raises a `PdfDetected(filePath)` event for each `.pdf` file creation event.
- Deduplicates rapid duplicate events with a 500 ms debounce per file path (common with copy operations that fire multiple OS events for the same file).
- Interface: `Start(path)`, `Stop()`, event `PdfDetected`.

**PdfQueue**
- Thread-safe FIFO queue holding file paths of detected PDFs not yet processed.
- Ensures that when a confirmation dialog is open, subsequent PDF arrivals are held until the dialog is closed.
- Interface: `Enqueue(path)`, `TryDequeue() → string?`.

**SkillNameDeriver**
- Converts a PDF filename (minus extension) into a valid kebab-case skill name.
- Strips non-alphanumeric characters, lowercases, replaces spaces and underscores with hyphens, collapses repeated hyphens, trims leading/trailing hyphens.
- Interface: `DeriveFrom(fileName) → string` — pure function with no side effects.

**PythonDetector**
- Searches PATH for `py`, `python`, and `python3` executables in order.
- Returns the first found path, or `null` if none found.
- Interface: `TryDetect() → string?`.

**PythonRunner**
- Executes a Python script as a subprocess with provided arguments.
- Captures stdout, stderr, and exit code.
- Interface: `Run(executablePath, scriptPath, args[]) → ProcessResult { ExitCode, Stdout, Stderr }`.

**ExtractionOrchestrator**
- Coordinates the full extraction pipeline for a single PDF.
- Takes an `ExtractionRequest` (pdf path, output path, skill name, skill description, python executable path, overwrite flag).
- Resolves the extraction script path relative to the app's installation directory.
- Calls `PythonRunner` to execute the extraction script.
- On success, calls `SkillInstaller` to write `SKILL.md` and the config file.
- Returns `ExtractionResult { Success, SkillName, OutputFilePath, ErrorMessage }`.
- Interface: `Extract(ExtractionRequest) → ExtractionResult`.

**SkillInstaller**
- Creates `~/.claude/skills/<name>/` directory if it doesn't exist.
- Writes `SKILL.md` with the provided skill name, description, reference table row, and lookup workflow using the agreed template.
- Writes `~/.claude/skills/<name>-config` with a single line: the output folder path.
- Interface: `Install(SkillDefinition)`, `Exists(skillName) → bool`.

**AppConfigRepository**
- Reads and writes a JSON config file stored in `%APPDATA%\PdfExtractToSkill\config.json`.
- Config model: `WatchedRootPath`, `OutputPath`, `PythonExePath` (nullable), `AutostartEnabled`.
- Interface: `Load() → AppConfig`, `Save(AppConfig)`.

**StartupRegistrar**
- Manages a `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run` registry entry for the app.
- Interface: `Register(exePath)`, `Unregister()`, `IsRegistered() → bool`.

**NotificationService**
- Sends Windows Toast notifications using `Microsoft.Toolkit.Uwp.Notifications`.
- Toast actions are handled by activating the app with a URI scheme argument (e.g. `pdfextracttoskill://extract?path=...`) — requires AUMID to be registered, which MSIX handles automatically.
- Three notification types: new PDF detected (with "Extract Now" / "Dismiss" actions), extraction complete, extraction error.
- Interface: `ShowNewPdf(fileName, filePath)`, `ShowComplete(skillName)`, `ShowError(message)`.

**ConfirmationDialogViewModel**
- MVVM ViewModel for the per-PDF confirmation dialog.
- Properties: `SkillName` (editable), `SkillDescription` (editable, multiline with scrollbar), `SkillAlreadyExists` (bool), `OverwriteConfirmed` (bool).
- Commands: `ConfirmCommand`, `CancelCommand`.
- Validates that `SkillName` is non-empty and is valid kebab-case before enabling `ConfirmCommand`.
- Calls `SkillInstaller.Exists()` on `SkillName` change to update `SkillAlreadyExists`.

**SettingsViewModel**
- MVVM ViewModel for the Settings dialog, opened via right-click on the tray icon.
- Properties: `WatchedRootPath`, `OutputPath`, `PythonExePath`, `AutostartEnabled`, `IsValid`, `PrerequisiteStatus`.
- Commands: `SaveCommand`, `BrowseRootPathCommand`, `BrowseOutputPathCommand`, `BrowsePythonCommand`, `CheckPrerequisitesCommand`.
- `IsValid` is true when both `WatchedRootPath` and `OutputPath` are non-empty and point to existing directories.

### Extraction Script

The existing `C:\Work\Documents\pdf\extract.py` is refactored into a proper CLI script with `argparse`, accepting: `--pdf`, `--output-dir`, `--skill-name`, `--skill-description`. Version detection logic (regex scan of first 5 pages), header format, and page markers are preserved exactly. This script is bundled inside the MSIX package and installed alongside the WPF executable.

### Architecture Pattern

- WPF application with `ShutdownMode="OnExplicitShutdown"` — no main window; the system tray icon IS the app's UI entry point.
- MVVM throughout all dialogs (Confirmation, Settings, FirstRunWizard).
- Service layer (FolderWatcherService, NotificationService, ExtractionOrchestrator, PrerequisiteChecker) is decoupled from ViewModels via interfaces, registered via a simple DI container (e.g. `Microsoft.Extensions.DependencyInjection`).
- Toast action activation handled by registering a URI scheme in the MSIX manifest, routed to the running instance via single-instance enforcement.

### Configuration Storage

- `%APPDATA%\PdfExtractToSkill\config.json` — persistent app settings. The uninstaller prompts whether to delete this folder.
- `~/.claude/skills/<name>-config` — one file per installed skill; left intact on uninstall.

### Skill Installation Target

- Global: `~/.claude/skills/<name>/SKILL.md` — consistent with the existing SEMI and Rorze skill packages.

### Package & Distribution

- Visual Studio Windows Application Packaging Project (`.wapproj`) produces a signed or unsigned `.msix`.
- For internal/personal use, the package can be self-signed; Windows must have "Sideload apps" or "Developer Mode" enabled to install unsigned MSIX.
- CI can produce the `.msix` as a build artifact for easy redistribution.

---

## Testing Decisions

### What makes a good test

Tests should verify **observable external behaviour**, not implementation details. A good test sets up inputs, calls the public interface, and asserts on outputs or side effects (files written, events raised, process results). Tests must not assert on private method calls, internal state, or execution order.

### Modules to test

**SkillNameDeriver — Unit tests**
- Input: raw PDF filenames with spaces, underscores, mixed case, special characters, version strings.
- Output: valid kebab-case strings with no leading/trailing hyphens, no double hyphens.
- Examples: `"Rorze_EFEM_CommSpec_basic.pdf"` → `"rorze-efem-commspec-basic"`, `"RHS16-093-003E CommSpec (v3).pdf"` → `"rhs16-093-003e-commspec-v3"`.
- No mocks needed — pure function.

**ExtractionOrchestrator — Integration tests**
- Requires: Python installed with PyMuPDF, a small real test PDF fixture (checked into the test project).
- Test: given a real PDF and a temp output directory, `Extract()` produces a `.txt` file in the correct format and a `SKILL.md` at the correct path.
- Assert on file existence, file header content (version line present, page markers present), and `SKILL.md` containing the provided skill name and description.
- Teardown: delete temp output dir and skill directory after each test.

**PrerequisiteChecker — Integration tests**
- Test in an environment where Python is present: `CheckAll()` returns `PythonFound = true` and correct path.
- Test `pymupdf` detection: if `pymupdf` is importable, `PyMuPdfInstalled = true`; if not, the auto-install path is exercised (use a throwaway venv in the test).
- Do not mock subprocess — these tests validate the real detection logic.

---

## Out of Scope

- Subfolder monitoring — only top-level files in the root path are watched.
- Watching multiple root paths simultaneously — single root path per configuration.
- PDF table extraction in C# — extraction is delegated entirely to the Python/PyMuPDF script.
- Cloud storage integration (OneDrive, SharePoint) as watched paths — local filesystem only.
- Version control or history of extracted skills.
- Skill uninstall / management UI — skills are managed directly in the filesystem; the app uninstaller does not touch installed skills.
- Support for non-PDF document types.
- Watching for file modifications or deletions — only new file creation events trigger the pipeline.
- Auto-update mechanism — distribution of new versions is manual (replace the MSIX).
- Windows Store publication — package is intended for personal/internal sideloading.

---

## Further Notes

- **MSIX solves the AUMID problem.** Windows Toast notifications with interactive buttons require the app to have a registered Application User Model ID. MSIX packages register the AUMID automatically via the package manifest — this is the primary reason MSIX is chosen over Inno Setup or xcopy deployment.
- **Single-instance enforcement is required.** Toast action callbacks (e.g. "Extract Now") re-activate the running app via the registered URI scheme. If a second instance launches instead, the tray icon duplicates and the queue state is lost. Use a `Mutex` or named pipe to enforce single-instance and route activation arguments to the existing process.
- **Python is a prerequisite, not a bundled dependency.** The installer does not bundle Python. If Python is not found, the installer completes but marks the prerequisite as failed and guides the user to install Python before the extraction pipeline will work.
- **The extraction script path is resolved at runtime** relative to the directory containing the WPF executable, not hardcoded. This ensures the script is found correctly whether the app is run from the MSIX install location, a development build output directory, or a test harness.
- **`FileSystemWatcher` edge cases.** On Windows, a single file copy can fire `Created`, `Changed`, and `Renamed` events for the same file within milliseconds. The 500 ms debounce in `FolderWatcherService` and the file-path deduplication in `PdfQueue` together prevent the same PDF from generating multiple notifications.
- The existing `C:\Work\Documents\pdf\extract.py` is the reference implementation. Its core logic (PyMuPDF open, version detection regex, header format, page markers) must be preserved exactly during the `argparse` refactor.
- The confirmed SKILL.md template (expert persona, Configuration section, Reference Files table, Lookup Workflow, Answer Format, Not Found section) must be reproduced faithfully by `SkillInstaller` to maintain compatibility with existing skill-invoking patterns in Claude Code.
