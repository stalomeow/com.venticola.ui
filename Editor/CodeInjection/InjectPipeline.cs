using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using VentiCola.UI.Internal;
using VentiColaEditor.UI.Settings;
using Debug = UnityEngine.Debug;

namespace VentiColaEditor.UI.CodeInjection
{
    public static class InjectPipeline
    {
        // 在 Force Recompile 前设置字段为 true
        // 当 Recompilation 完全结束后（Injection 也结束力），会有一次 Domain Reload，该字段又自动变为 false
        private static bool s_IsForceRecompilng = false;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // we should inject codes before unity reloads domain
            CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompilationFinished; // just in case
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
        }

        private static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {
            if (messages.Any(msg => msg.type == CompilerMessageType.Error))
            {
                return;
            }

            // Note: 如果是在打包时编译的，那无论如何都要注入代码！
            if (s_IsForceRecompilng || UIProjectSettings.instance.AutoCodeInjection || BuildPipeline.isBuildingPlayer)
            {
                InjectCodesForAssemblies(new string[] { assemblyPath });
            }
        }

        [MenuItem("VentiCola/UI/Inject Codes (Reload)", true)]
        [MenuItem("VentiCola/UI/Inject Codes (Recompile)", true)]
        private static bool InjectCodesValidator()
        {
            return !EditorApplication.isCompiling && !EditorApplication.isPlaying;
        }

        [MenuItem("VentiCola/UI/Inject Codes (Reload)")]
        public static void InjectCodesWithReload()
        {
            if (!InjectCodesValidator())
            {
                return;
            }

            const string message = "This may force unity to reload all script assemblies (without new compilation) and reset the state of all the scripts. Are you sure to continue?";

            if (EditorUtility.DisplayDialog("UI Code Injection", message, "Continue", "Cancel"))
            {
                Assembly[] assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);

                if (InjectCodesForAssemblies(assemblies.Select(a => a.outputPath)))
                {
                    EditorUtility.RequestScriptReload();
                }
            }
        }

        [MenuItem("VentiCola/UI/Inject Codes (Recompile)")]
        public static void InjectCodesWithRecompilation()
        {
            if (!InjectCodesValidator())
            {
                return;
            }

            const string message = "This will force unity to recompile all script assemblies. Are you sure to continue?";

            if (EditorUtility.DisplayDialog("UI Code Injection", message, "Continue", "Cancel"))
            {
                s_IsForceRecompilng = true;
                CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
            }
        }

        private static List<string> FilterAssemblyPaths(IEnumerable<string> assemblyPaths, IEnumerable<string> assemblyNameWhiteList)
        {
            var results = new List<string>();
            var whiteList = new HashSet<string>(assemblyNameWhiteList);

            foreach (var path in assemblyPaths)
            {
                string name = Path.GetFileNameWithoutExtension(path);

                if (whiteList.Contains(name))
                {
                    results.Add(path);
                }
            }

            return results;
        }

        private static UnityEngine.Object GetAssemblyDefinitionAsset(AssemblyDefinition assembly)
        {
            var asmDefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assembly.Name.Name);
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(asmDefPath);
        }

        private static bool InjectCodesForAssemblies(IEnumerable<string> assemblyPaths)
        {
            var settings = UIProjectSettings.instance;
            List<string> validAssemblyPaths = FilterAssemblyPaths(assemblyPaths, settings.CodeInjectionAssemblyWhiteList);

            if (validAssemblyPaths.Count == 0)
            {
                return false;
            }

            var watch = new Stopwatch();
            var injectorTypes = TypeCache.GetTypesDerivedFrom<IAssemblyInjector>();
            var injectedAssemblyCount = 0;

            try
            {
                for (int i = 0; i < validAssemblyPaths.Count; i++)
                {
                    AssemblyDefinition assembly = null;

                    try
                    {
                        watch.Restart();

                        string assemblyFullPath = Path.GetFullPath(validAssemblyPaths[i]);
                        assembly = AssemblyDefinition.ReadAssembly(assemblyFullPath, new ReaderParameters()
                        {
                            ReadSymbols = true,
                            ReadWrite = true
                        });

                        if (InjectCodesForAssembly(assembly, injectorTypes, displayProgress))
                        {
                            injectedAssemblyCount++;

                            if (settings.EnableCodeInjectionLog)
                            {
                                var totalSeconds = watch.ElapsedMilliseconds / 1000f;
                                var asmDefObj = GetAssemblyDefinitionAsset(assembly);
                                Debug.Log($"Injected UI codes for assembly <b>'{assembly.Name.Name}'</b> in {totalSeconds:N4} seconds.", asmDefObj);
                            }
                        }

                        void displayProgress(float progress)
                        {
                            string title = $"Injecting UI Codes ({i + 1}/{validAssemblyPaths.Count})";
                            EditorUtility.DisplayProgressBar(title, assemblyFullPath, progress);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        Debug.LogError($"Failed to inject UI codes for assembly <b>'{assembly?.Name.Name ?? validAssemblyPaths[i]}'</b>!");
                    }
                    finally
                    {
                        assembly?.Dispose();
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return injectedAssemblyCount > 0;
        }

        private static bool InjectCodesForAssembly(AssemblyDefinition assembly, TypeCache.TypeCollection injectorTypes, Action<float> progressCallback)
        {
            if (IsAssemblyInjected(assembly))
            {
                return false;
            }

            var injectedAnyCode = false;
            var methodCache = new MethodReferenceCache(assembly.MainModule);

            // inject codes
            for (int i = 0; i < injectorTypes.Count; i++)
            {
                var injector = Activator.CreateInstance(injectorTypes[i]) as IAssemblyInjector;

                injector.Assembly = assembly;
                injector.Methods = methodCache;
                injector.ProgressCallback = (value => progressCallback((value + i) / injectorTypes.Count));

                injectedAnyCode |= injector.Execute();
            }

            // apply new codes
            if (injectedAnyCode)
            {
                MarkAssemblyInjected(assembly); // 标记一下，避免下次重复注入
                assembly.Write(new WriterParameters() { WriteSymbols = true });
            }

            // 不应该在这里释放 assembly

            return injectedAnyCode;
        }

        private static bool IsAssemblyInjected(AssemblyDefinition assembly)
        {
            return MetaDataUtility.HasCustomAttribute<VentiColaUICodesInjectedAttribute>(assembly.CustomAttributes);
        }

        private static void MarkAssemblyInjected(AssemblyDefinition assembly)
        {
            var ctorInfo = typeof(VentiColaUICodesInjectedAttribute).GetConstructor(Type.EmptyTypes);
            var ctorRef = assembly.MainModule.ImportReference(ctorInfo);
            assembly.CustomAttributes.Add(new CustomAttribute(ctorRef));
        }
    }
}