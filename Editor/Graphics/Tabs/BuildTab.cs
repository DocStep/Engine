using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using ImGuiNET;

namespace Editor.Graphics;


public class BuildTab : IEditorTab {

    private enum BuildType {
        None,
        Grouped,
        SingleFile,
    }

    private const int MaxOutputLines = 500;
    private readonly ConcurrentQueue<string> _pendingOutput = new();
    private readonly List<string> _outputLines = new();
    private string _projectPath = FindProjectFile();
    private string _outputPath = string.Empty;
    private string _runtimeIdentifier = RuntimeInformation.RuntimeIdentifier;
    private BuildType _buildType = BuildType.Grouped;
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
        int buildTypeIndex = (int)_buildType;
        string[] buildTypeNames = { "None", "Grouped", "Single file" };
        if (ImGui.Combo("Build type", ref buildTypeIndex, buildTypeNames, buildTypeNames.Length))
            _buildType = (BuildType)buildTypeIndex;
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
        _ = PublishAsync(projectPath, outputPath, _runtimeIdentifier.Trim(), _buildType);
    }


    private async Task PublishAsync (string projectPath, string outputPath, string runtimeIdentifier, BuildType buildType) {
        string stagingPath = Path.Combine(Path.GetTempPath(), "EngineGamePublish-" + Guid.NewGuid().ToString("N"));
        try {
            Directory.CreateDirectory(stagingPath);
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
                "-o", stagingPath,
                "-r", runtimeIdentifier,
                "-p:SelfContained=true",
                "-p:GameBuild=true",
                "-p:DebugType=none",
                "-p:DebugSymbols=false",
                "-p:GenerateDocumentationFile=false",
                "-p:SatelliteResourceLanguages=en",
                "-p:PublishSingleFile=" + (buildType == BuildType.SingleFile).ToString().ToLowerInvariant(),
            };

            if (buildType == BuildType.SingleFile) {
                args.Add("-p:EnableCompressionInSingleFile=true");
                args.Add("-p:IncludeNativeLibrariesForSelfExtract=true");
            }

            foreach (string arg in args)
                process.StartInfo.ArgumentList.Add(arg);

            if (!process.Start())
                throw new InvalidOperationException("The dotnet publish process could not be started.");

            _status = "Publishing...";
            Task outputTask = ReadOutputAsync(process.StandardOutput);
            Task errorTask = ReadOutputAsync(process.StandardError);
            await process.WaitForExitAsync();
            await Task.WhenAll(outputTask, errorTask);

            if (process.ExitCode != 0) {
                _status = $"Publish failed with exit code {process.ExitCode}.";
                return;
            }

            CopyPublishOutput(stagingPath, outputPath, buildType, runtimeIdentifier);
            _status = $"Publish succeeded: {outputPath}";
        } catch (System.ComponentModel.Win32Exception ex) {
            _status = $"Could not start dotnet: {ex.Message}";
        } catch (IOException ex) {
            _status = $"Publish I/O error: {ex.Message}";
        } catch (UnauthorizedAccessException ex) {
            _status = $"Publish access error: {ex.Message}";
        } catch (InvalidOperationException ex) {
            _status = $"Publish failed: {ex.Message}";
        } catch (InvalidDataException ex) {
            _status = $"Invalid publish metadata: {ex.Message}";
        } catch (System.Text.Json.JsonException ex) {
            _status = $"Invalid publish metadata: {ex.Message}";
        } finally {
            try {
                if (Directory.Exists(stagingPath))
                    Directory.Delete(stagingPath, recursive: true);
            } catch (IOException ex) {
                _status = $"Could not clean publish staging files: {ex.Message}";
            } catch (UnauthorizedAccessException ex) {
                _status = $"Could not clean publish staging files: {ex.Message}";
            }
            _isPublishing = false;
        }
    }


    private static void CopyPublishOutput (string stagingPath, string outputPath, BuildType buildType, string runtimeIdentifier) {
        Directory.CreateDirectory(outputPath);
        foreach (string pattern in new[] { "*.dll", "*.exe", "*.deps.json", "*.runtimeconfig.json", "*.pdb" }) {
            foreach (string oldArtifact in Directory.GetFiles(outputPath, pattern, SearchOption.TopDirectoryOnly))
                File.Delete(oldArtifact);
        }

        string runtimesPath = Path.Combine(outputPath, "runtimes");
        if (Directory.Exists(runtimesPath))
            Directory.Delete(runtimesPath, recursive: true);

        string libsPath = Path.Combine(outputPath, "libs");
        if (Directory.Exists(libsPath))
            Directory.Delete(libsPath, recursive: true);

        string mainAssemblyName = Directory.GetFiles(stagingPath, "*.runtimeconfig.json", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .FirstOrDefault(name => name is not null) is string runtimeConfigName
                ? Path.GetFileNameWithoutExtension(runtimeConfigName)
                : string.Empty;

        HashSet<string> movedLibraries = new(StringComparer.OrdinalIgnoreCase);
        foreach (string stagedFile in Directory.GetFiles(stagingPath, "*", SearchOption.TopDirectoryOnly)) {
            string fileName = Path.GetFileName(stagedFile);
            bool isDll = Path.GetExtension(stagedFile).Equals(".dll", StringComparison.OrdinalIgnoreCase);
            bool isManagedLibrary = isDll
                && !Path.GetFileNameWithoutExtension(stagedFile).Equals(mainAssemblyName, StringComparison.OrdinalIgnoreCase)
                && buildType != BuildType.None
                && IsManagedAssembly(stagedFile);
            bool isGroupedNativeLibrary = isDll
                && !Path.GetFileNameWithoutExtension(stagedFile).Equals(mainAssemblyName, StringComparison.OrdinalIgnoreCase)
                && buildType == BuildType.Grouped
                && runtimeIdentifier.StartsWith("win-", StringComparison.OrdinalIgnoreCase)
                && !IsRuntimeHostLibrary(fileName)
                && !isManagedLibrary;
            string destination = isManagedLibrary || isGroupedNativeLibrary
                ? Path.Combine(libsPath, fileName)
                : Path.Combine(outputPath, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(stagedFile, destination, overwrite: true);
            if (isManagedLibrary || isGroupedNativeLibrary)
                movedLibraries.Add(fileName);
        }

        foreach (string stagedDirectory in Directory.GetDirectories(stagingPath, "*", SearchOption.TopDirectoryOnly)) {
            string destination = Path.Combine(outputPath, Path.GetFileName(stagedDirectory));
            CopyDirectory(stagedDirectory, destination);
        }

        if (movedLibraries.Count > 0)
            UpdateDependencyPaths(Path.Combine(outputPath, mainAssemblyName + ".deps.json"), movedLibraries);
    }


    private static void UpdateDependencyPaths (string depsPath, HashSet<string> movedLibraries) {
        JsonObject dependencies = JsonNode.Parse(File.ReadAllText(depsPath))?.AsObject()
            ?? throw new InvalidDataException($"Could not parse dependency manifest: {depsPath}");
        if (dependencies["targets"] is not JsonObject targets)
            throw new InvalidDataException($"Dependency manifest has no targets: {depsPath}");

        foreach (JsonNode? target in targets.Select(property => property.Value)) {
            if (target is not JsonObject libraries)
                continue;
            foreach (JsonNode? library in libraries.Select(property => property.Value)) {
                if (library is not JsonObject assets)
                    continue;
                foreach (string assetKind in new[] { "runtime", "runtimeTargets", "native" }) {
                    if (assets[assetKind] is not JsonObject runtimeAssets)
                        continue;
                    foreach (KeyValuePair<string, JsonNode?> asset in runtimeAssets.ToArray()) {
                        string fileName = Path.GetFileName(asset.Key);
                        if (!movedLibraries.Contains(fileName))
                            continue;
                        runtimeAssets.Remove(asset.Key);
                        runtimeAssets["libs/" + fileName] = asset.Value?.DeepClone();
                    }
                }
            }
        }

        File.WriteAllText(depsPath, dependencies.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    }


    private static bool IsManagedAssembly (string path) {
        try {
            _ = System.Reflection.AssemblyName.GetAssemblyName(path);
            return true;
        } catch (BadImageFormatException) {
            return false;
        }
    }


    private static bool IsRuntimeHostLibrary (string fileName) {
        string name = Path.GetFileNameWithoutExtension(fileName);
        return name.Equals("coreclr", StringComparison.OrdinalIgnoreCase)
            || name.Equals("hostfxr", StringComparison.OrdinalIgnoreCase)
            || name.Equals("hostpolicy", StringComparison.OrdinalIgnoreCase)
            || name.Equals("clrjit", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("clrgc", StringComparison.OrdinalIgnoreCase)
            || name.Equals("clretwrc", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("mscordaccore", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("mscordbi", StringComparison.OrdinalIgnoreCase)
            || name.Equals("mscorrc", StringComparison.OrdinalIgnoreCase)
            || name.Equals("msquic", StringComparison.OrdinalIgnoreCase)
            || name.Equals("System.IO.Compression.Native", StringComparison.OrdinalIgnoreCase);
    }


    private static void CopyDirectory (string sourcePath, string destinationPath) {
        Directory.CreateDirectory(destinationPath);
        foreach (string file in Directory.GetFiles(sourcePath, "*", SearchOption.TopDirectoryOnly))
            File.Copy(file, Path.Combine(destinationPath, Path.GetFileName(file)), overwrite: true);
        foreach (string directory in Directory.GetDirectories(sourcePath, "*", SearchOption.TopDirectoryOnly))
            CopyDirectory(directory, Path.Combine(destinationPath, Path.GetFileName(directory)));
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