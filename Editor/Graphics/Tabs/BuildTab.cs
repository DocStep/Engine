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
    private bool _nativeAot;
    private volatile string _status = "Ready";
    private volatile bool _isPublishing;

    public string Name { get; set; } = "Build";
    public bool isActive { get; set; } = true;


    public BuildTab () {
        if (!string.IsNullOrEmpty(_projectPath))
            _outputPath = Path.Combine(Path.GetDirectoryName(_projectPath)!, "Build");
    }


    public void Draw () {
        ImGui.Begin(Name);
        EditorUI.DrawTabContext(this);

        ImGui.BeginDisabled(_isPublishing);
        ImGui.InputText("Game project (.csproj)", ref _projectPath, 1024);
        ImGui.InputText("Output directory", ref _outputPath, 1024);
        ImGui.InputText("Runtime identifier", ref _runtimeIdentifier, 128);
        ImGui.Checkbox("Native AOT (requires C++ tools)", ref _nativeAot);
        if (ImGui.Button("Publish Game"))
            StartPublish();
        ImGui.EndDisabled();

        ImGui.Text($"Status: {_status}");
        while (_pendingOutput.TryDequeue(out string? line)) {
            _outputLines.Add(line);
            if (_outputLines.Count > MaxOutputLines)
                _outputLines.RemoveAt(0);
        }

        ImGui.BeginChild("##BuildOutput", new Vector2(0, 0));
        foreach (string line in _outputLines)
            ImGui.TextWrapped(line);
        ImGui.EndChild();

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
        _isPublishing = true;
        _status = "Starting publish...";
        _ = PublishAsync(projectPath, outputPath, _runtimeIdentifier.Trim(), _nativeAot);
    }


    private async Task PublishAsync (string projectPath, string outputPath, string runtimeIdentifier, bool nativeAot) {
        using Process process = new();
        process.StartInfo = new ProcessStartInfo {
            FileName = "dotnet",
            WorkingDirectory = Path.GetDirectoryName(projectPath)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        process.StartInfo.ArgumentList.Add("publish");
        process.StartInfo.ArgumentList.Add(projectPath);
        process.StartInfo.ArgumentList.Add("--configuration");
        process.StartInfo.ArgumentList.Add("Release");
        process.StartInfo.ArgumentList.Add("--output");
        process.StartInfo.ArgumentList.Add(outputPath);
        process.StartInfo.ArgumentList.Add("--runtime");
        process.StartInfo.ArgumentList.Add(runtimeIdentifier);
        process.StartInfo.ArgumentList.Add("--self-contained");
        process.StartInfo.ArgumentList.Add("true");
        process.StartInfo.ArgumentList.Add("-p:PublishAot=" + nativeAot.ToString().ToLowerInvariant());
        process.StartInfo.ArgumentList.Add("-p:GameBuild=true");

        try {
            if (!process.Start())
                throw new InvalidOperationException("The dotnet publish process could not be started.");

            _status = "Publishing...";
            Task outputTask = ReadOutputAsync(process.StandardOutput);
            Task errorTask = ReadOutputAsync(process.StandardError);
            await process.WaitForExitAsync();
            await Task.WhenAll(outputTask, errorTask);

            _status = process.ExitCode == 0
                ? $"Publish succeeded: {outputPath}"
                : $"Publish failed with exit code {process.ExitCode}.";
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
        while ((line = await reader.ReadLineAsync()) != null)
            _pendingOutput.Enqueue(line);
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