using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ImGuiNET;

namespace Editor.Graphics;


public class BuildTab : IEditorTab {

    private const int MaxOutputLines = 500;
    private readonly ConcurrentQueue<string> _pendingOutput = new();
    private readonly List<string> _outputLines = new();
    private string _projectPath = FindProjectFile();
    private string _outputPath = string.Empty;
    private string _runtimeIdentifier = RuntimeInformation.RuntimeIdentifier;
    private bool _singleFile = true;
    private volatile bool _showBuildLogs;
    private string? _settingsWarning;
    private volatile string _status = "Ready";
    private volatile bool _isPublishing;

    public string Name { get; set; } = "Build";
    public bool isActive { get; set; } = true;


    public BuildTab () {
        if (!string.IsNullOrEmpty(_projectPath))
            _outputPath = Path.Combine(Path.GetDirectoryName(_projectPath)!, "Build");

        try {
            if (File.Exists(SettingsPath)) {
                string savedPath = File.ReadAllText(SettingsPath).Trim();
                if (!string.IsNullOrEmpty(savedPath)) _outputPath = savedPath;
            }
        } catch (IOException ex) {
            _settingsWarning = $"Could not load the last build path: {ex.Message}";
        } catch (UnauthorizedAccessException ex) {
            _settingsWarning = $"Could not load the last build path: {ex.Message}";
        }
    }


    public void Draw () {
        ImGui.Begin(Name);
        EditorUI.DrawTabContext(this);

        ImGui.BeginDisabled(_isPublishing);
        ImGui.InputText("Game project (.csproj)", ref _projectPath, 1024);
        ImGui.InputText("Output directory", ref _outputPath, 1024);
        ImGui.InputText("Runtime identifier", ref _runtimeIdentifier, 128);
        ImGui.Checkbox("Publish as single file", ref _singleFile);
        if (ImGui.Button("Publish Game"))
            StartPublish();
        ImGui.EndDisabled();

        ImGui.SameLine();
        if (ImGui.Button("Open Build Folder"))
            OpenBuildFolder();
        ImGui.SameLine();
        ImGui.Checkbox("Show build logs", ref _showBuildLogs);

        if (_settingsWarning is not null)
            ImGui.TextWrapped(_settingsWarning);
        ImGui.Text($"Status: {_status}");
        while (_pendingOutput.TryDequeue(out string? line)) {
            if (_showBuildLogs) {
                _outputLines.Add(line);
                if (_outputLines.Count > MaxOutputLines)
                    _outputLines.RemoveAt(0);
            }
        }

        if (_showBuildLogs) {
            ImGui.BeginChild("##BuildOutput", new Vector2(0, 0));
            foreach (string line in _outputLines)
                ImGui.TextWrapped(line);
            ImGui.EndChild();
        } else {
            _outputLines.Clear();
        }

        ImGui.End();
    }


    private void StartPublish () {
        string projectPath;
        string outputPath;
        try {
            projectPath = Path.GetFullPath(_projectPath);
            outputPath = Path.GetFullPath(_outputPath);
        } catch (ArgumentException ex) {
            _status = $"Invalid path: {ex.Message}";
            return;
        } catch (NotSupportedException ex) {
            _status = $"Invalid path: {ex.Message}";
            return;
        } catch (PathTooLongException ex) {
            _status = $"Invalid path: {ex.Message}";
            return;
        }

        if (!string.Equals(Path.GetExtension(projectPath), ".csproj", StringComparison.OrdinalIgnoreCase) || !File.Exists(projectPath)) {
            _status = "Select an existing .csproj file.";
            return;
        }
        if (string.IsNullOrWhiteSpace(_outputPath)) {
            _status = "Choose an output directory.";
            return;
        }
        if (string.IsNullOrWhiteSpace(_runtimeIdentifier)) {
            _status = "Enter a runtime identifier, such as win-x64.";
            return;
        }

        while (_pendingOutput.TryDequeue(out _)) { }
        _outputLines.Clear();
        _settingsWarning = null;
        try {
            SaveOutputPath(outputPath);
        } catch (IOException ex) {
            _settingsWarning = $"Could not remember the build path: {ex.Message}";
        } catch (UnauthorizedAccessException ex) {
            _settingsWarning = $"Could not remember the build path: {ex.Message}";
        }
        _isPublishing = true;
        _status = "Starting publish...";
        _ = PublishAsync(projectPath, outputPath, _runtimeIdentifier.Trim(), _singleFile);
    }


    private async Task PublishAsync (string projectPath, string outputPath, string runtimeIdentifier, bool singleFile) {
        using Process process = new();
        process.StartInfo = new ProcessStartInfo {
            FileName = "dotnet",
            WorkingDirectory = Path.GetDirectoryName(projectPath)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        List<string> args = new() {
            "publish", projectPath,
            "-c", "Release",
            "-o", outputPath,
            "-r", runtimeIdentifier,
            "-p:SelfContained=true",
            "-p:GameBuild=true",
            "-p:DebugType=none",
            "-p:DebugSymbols=false",
            "-p:GenerateDocumentationFile=false",
            "-p:SatelliteResourceLanguages=en",
            "-p:PublishSingleFile=" + singleFile.ToString().ToLowerInvariant(),
        };

        if (singleFile) {
            args.Add("-p:EnableCompressionInSingleFile=true");
            args.Add("-p:IncludeNativeLibrariesForSelfExtract=true");
        }

        foreach (string arg in args)
            process.StartInfo.ArgumentList.Add(arg);

        try {
            if (!process.Start())
                throw new InvalidOperationException("The dotnet publish process could not be started.");

            _status = "Publishing...";
            Task outputTask = ReadOutputAsync(process.StandardOutput);
            Task errorTask = ReadOutputAsync(process.StandardError);
            await process.WaitForExitAsync();
            await Task.WhenAll(outputTask, errorTask);

            string settingsNotice = _settingsWarning is null ? string.Empty : $" {_settingsWarning}";
            _status = (process.ExitCode == 0
                ? $"Publish succeeded: {outputPath}"
                : $"Publish failed with exit code {process.ExitCode}.") + settingsNotice;
        } catch (System.ComponentModel.Win32Exception ex) {
            _status = $"Could not start dotnet: {ex.Message}";
        } catch (IOException ex) {
            _status = $"Publish I/O error: {ex.Message}";
        } catch (InvalidOperationException ex) {
            _status = $"Publish failed: {ex.Message}";
        } finally {
            _isPublishing = false;
        }
    }


    private async Task ReadOutputAsync (StreamReader reader) {
        string? line;
        while ((line = await reader.ReadLineAsync()) != null) {
            if (_showBuildLogs)
                _pendingOutput.Enqueue(line);
        }
    }

    private void OpenBuildFolder () {
        if (string.IsNullOrWhiteSpace(_outputPath)) {
            _status = "Choose an output directory first.";
            return;
        }

        try {
            string outputPath = Path.GetFullPath(_outputPath);
            Directory.CreateDirectory(outputPath);
            Process? explorer = Process.Start(new ProcessStartInfo {
                FileName = outputPath,
                UseShellExecute = true,
            });
            if (explorer is null) {
                _status = "Could not open the build folder.";
                return;
            }
            explorer.Dispose();
            SaveOutputPath(outputPath);
            _status = $"Opened build folder: {outputPath}";
        } catch (ArgumentException ex) {
            _status = $"Invalid build folder path: {ex.Message}";
        } catch (NotSupportedException ex) {
            _status = $"Invalid build folder path: {ex.Message}";
        } catch (PathTooLongException ex) {
            _status = $"Invalid build folder path: {ex.Message}";
        } catch (IOException ex) {
            _status = $"Could not open build folder: {ex.Message}";
        } catch (UnauthorizedAccessException ex) {
            _status = $"Could not open build folder: {ex.Message}";
        } catch (System.ComponentModel.Win32Exception ex) {
            _status = $"Could not open build folder: {ex.Message}";
        } catch (InvalidOperationException ex) {
            _status = $"Could not open build folder: {ex.Message}";
        }
    }

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "EngineGame",
        "BuildTabOutputPath.txt");

    private static void SaveOutputPath (string outputPath) {
        string? settingsDirectory = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrEmpty(settingsDirectory))
            Directory.CreateDirectory(settingsDirectory);
        File.WriteAllText(SettingsPath, outputPath);
    }

    private static string FindProjectFile () {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory != null; directory = directory.Parent) {
            string? projectPath = directory.EnumerateFiles("*.csproj", SearchOption.TopDirectoryOnly)
                .Select(file => file.FullName)
                .FirstOrDefault();
            if (projectPath != null)
                return projectPath;
        }

        return string.Empty;
    }

}