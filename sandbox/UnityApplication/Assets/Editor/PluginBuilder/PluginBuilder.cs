using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;

namespace UnityApplication.Editor
{
    [InitializeOnLoad]
    public static class PluginBuilder
    {
        private const string PluginFileName = "GroveGames.Serialization.dll";
        private const string PluginPath = "Assets/Plugins/" + PluginFileName;
        private const string ProjectPath = "../../src/GroveGames.Serialization/GroveGames.Serialization.csproj";
        private const string GeneratorFileName = "GroveGames.Serialization.Generator.dll";
        private const string GeneratorPath = "Assets/Plugins/Analyzers/" + GeneratorFileName;
        private const string GeneratorProjectPath = "../../src/GroveGames.Serialization.Generator/GroveGames.Serialization.Generator.csproj";
        private const string GeneratorMeta = "fileFormatVersion: 2\nguid: 6f1f5a3c9b2d4e8a8c7d1e2f3a4b5c6d\nlabels:\n- RoslynAnalyzer\nPluginImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  iconMap: {}\n  executionOrder: {}\n  defineConstraints: []\n  isPreloaded: 0\n  isOverridable: 0\n  isExplicitlyReferenced: 0\n  validateReferences: 1\n  platformData:\n  - first:\n      Any: \n    second:\n      enabled: 0\n      settings: {}\n  - first:\n      Editor: Editor\n    second:\n      enabled: 0\n      settings:\n        DefaultValueInitialized: true\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n";

        static PluginBuilder()
        {
            string projectRoot = Path.Combine(Application.dataPath, "..");

            if (!File.Exists(Path.Combine(projectRoot, PluginPath)) || !File.Exists(Path.Combine(projectRoot, GeneratorPath)))
            {
                BuildPlugin();
            }
        }

        [MenuItem("Grove Games/Plugin Builder/Build")]
        private static void BuildPlugin()
        {
            UnityEngine.Debug.Log($"Building {PluginFileName}...");

            string dotnetPath = GetDotNetPath();
            if (string.IsNullOrEmpty(dotnetPath))
            {
                UnityEngine.Debug.LogError("dotnet CLI not found. Please ensure .NET SDK is installed.");
                return;
            }

            string projectRoot = Path.Combine(Application.dataPath, "..");
            string pluginsDir = Path.Combine(projectRoot, "Assets/Plugins");
            if (!Directory.Exists(pluginsDir))
            {
                Directory.CreateDirectory(pluginsDir);
            }

            if (!RunDotNet(dotnetPath, $"build {ProjectPath} -c Release -f netstandard2.1 -o ./Assets/Plugins", projectRoot))
            {
                return;
            }

            if (!RunDotNet(dotnetPath, $"build {GeneratorProjectPath} -c Release -o ./Assets/Plugins/Analyzers", projectRoot))
            {
                return;
            }

            string generatorMetaPath = Path.Combine(projectRoot, GeneratorPath + ".meta");

            if (!File.Exists(generatorMetaPath))
            {
                File.WriteAllText(generatorMetaPath, GeneratorMeta);
            }

            UnityEngine.Debug.Log("Plugin build completed successfully");
            AssetDatabase.Refresh();
        }

        private static bool RunDotNet(string dotnetPath, string arguments, string projectRoot)
        {
            var process = new Process();
            process.StartInfo.FileName = dotnetPath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.WorkingDirectory = projectRoot;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            try
            {
                process.Start();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    return true;
                }

                string error = process.StandardError.ReadToEnd();
                UnityEngine.Debug.LogError($"Plugin build failed: {error}");
                return false;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to start build process: {ex.Message}");
                return false;
            }
            finally
            {
                process?.Dispose();
            }
        }

        private static string GetDotNetPath()
        {
            string[] commonPaths;

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                commonPaths = new[]
                {
                    "dotnet.exe",
                    @"C:\Program Files\dotnet\dotnet.exe",
                    @"C:\Program Files (x86)\dotnet\dotnet.exe"
                };
            }
            else
            {
                commonPaths = new[]
                {
                    "dotnet",
                    "/usr/local/share/dotnet/dotnet",
                    "/usr/local/bin/dotnet",
                    "/usr/bin/dotnet"
                };
            }

            foreach (string path in commonPaths)
            {
                if (TryExecuteDotNet(path))
                {
                    return path;
                }
            }

            return null;
        }

        private static bool TryExecuteDotNet(string path)
        {
            try
            {
                var process = new Process();
                process.StartInfo.FileName = path;
                process.StartInfo.Arguments = "--version";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                process.WaitForExit();

                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
