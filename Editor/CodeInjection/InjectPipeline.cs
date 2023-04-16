using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine.Assertions;
using VentiCola.UI.Internals;
using VentiColaEditor.UI.CodeInjection.AssemblyInjectors;
using VentiColaEditor.UI.Settings;
using Debug = UnityEngine.Debug;

namespace VentiColaEditor.UI.CodeInjection
{
    public static class InjectPipeline
    {
        private const string k_ForceRecompileAndInjectKey = "com.stalo.venticola.ui.force-recompile-inject";

        private static bool ForceRecompileAndInject
        {
            get => SessionState.GetBool(k_ForceRecompileAndInjectKey, false);
            set
            {
                if (value)
                {
                    SessionState.SetBool(k_ForceRecompileAndInjectKey, true);
                }
                else
                {
                    SessionState.EraseBool(k_ForceRecompileAndInjectKey);
                }
            }
        }

        private static int s_RunningCompilationCount = 0;
        private static readonly HashSet<string> s_CompiledAssemblyPaths = new();


        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // we should inject codes before unity reloads domain

            CompilationPipeline.compilationStarted -= OnCompilationStarted; // just in case
            CompilationPipeline.compilationStarted += OnCompilationStarted;

            CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompilationFinished; // just in case
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;

            CompilationPipeline.compilationFinished -= OnCompilationFinished; // just in case
            CompilationPipeline.compilationFinished += OnCompilationFinished;
        }

        private static void OnCompilationStarted(object context)
        {
            s_RunningCompilationCount++;
        }

        private static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {
            if (messages.All(msg => msg.type != CompilerMessageType.Error))
            {
                s_CompiledAssemblyPaths.Add(assemblyPath);
            }
        }

        private static void OnCompilationFinished(object context)
        {
            s_RunningCompilationCount--;

            Assert.IsTrue(s_RunningCompilationCount >= 0, "RunningCompilationCount is non-negative.");

            if (s_RunningCompilationCount == 0)
            {
                bool forceRecompileAndInject = ForceRecompileAndInject;

                // Note: 如果是在打包时编译的，那无论如何都要注入代码！
                if (forceRecompileAndInject || UIProjectSettings.instance.AutoCodeInjection || BuildPipeline.isBuildingPlayer)
                {
                    InjectCodesForAssemblies(s_CompiledAssemblyPaths);
                }

                if (forceRecompileAndInject)
                {
                    // erase the value
                    ForceRecompileAndInject = false;
                }

                s_CompiledAssemblyPaths.Clear();
            }
        }


        [MenuItem("VentiCola/UI/Inject Codes (Reload)", true)]
        private static bool InjectCodesWithReloadValidator()
        {
            return !EditorApplication.isCompiling && !EditorApplication.isPlaying;
        }

        [MenuItem("VentiCola/UI/Inject Codes (Reload)")]
        public static void InjectCodesWithReload()
        {
            if (!InjectCodesWithReloadValidator())
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

        [MenuItem("VentiCola/UI/Inject Codes (Recompile)", true)]
        private static bool InjectCodesWithRecompilationValidator()
        {
            return !EditorApplication.isCompiling && !EditorApplication.isPlaying;
        }

        [MenuItem("VentiCola/UI/Inject Codes (Recompile)")]
        public static void InjectCodesWithRecompilation()
        {
            if (!InjectCodesWithRecompilationValidator())
            {
                return;
            }

            const string message = "This will force unity to recompile all script assemblies. Are you sure to continue?";

            if (EditorUtility.DisplayDialog("UI Code Injection", message, "Continue", "Cancel"))
            {
                ForceRecompileAndInject = true;
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

        private static List<AssemblyDefinition> ReadAssembliesAndAddToCache(IEnumerable<string> assemblyPaths, UnityAssemblyResolver assemblyResolver)
        {
            var results = new List<AssemblyDefinition>();

            foreach (var path in assemblyPaths)
            {
                var assembly = AssemblyDefinition.ReadAssembly(Path.GetFullPath(path), new ReaderParameters()
                {
                    ReadSymbols = true,
                    ReadWrite = true,
                    AssemblyResolver = assemblyResolver
                });

                assemblyResolver.AddAssemblyToCache(assembly);
                results.Add(assembly);
            }

            return results;
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
            var assemblyResolver = new UnityAssemblyResolver();
            var injectorTypes = TypeCache.GetTypesDerivedFrom<IAssemblyInjector>();
            var injectedAssemblyCount = 0;

            try
            {
                // 提前缓存新编译的程序集。因为后面可能会往里面注入代码，所以必须以 RW 权限打开。
                // 但是如果在写入程序集前，该程序集已经被 Resolve 的话，是以 R 权限打开的，无法写入，还占用了文件。
                List<AssemblyDefinition> assemblies = ReadAssembliesAndAddToCache(validAssemblyPaths, assemblyResolver);

                for (int i = 0; i < assemblies.Count; i++)
                {
                    var assembly = assemblies[i];
                    var assemblyName = assembly.Name.Name;

                    var injectedAnyCode = false;
                    var progressBarTitle = $"Injecting UI Codes ({i + 1}/{validAssemblyPaths.Count})";
                    EditorUtility.DisplayProgressBar(progressBarTitle, string.Empty, 0);

                    watch.Restart();

                    try
                    {
                        injectedAnyCode = InjectCodesForAssembly(assembly,
                            settings.CodeInjectionLogLevel, injectorTypes, progressBarTitle);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        Debug.LogError($"Failed to inject UI codes for assembly <b>'{assemblyName}'</b>!");
                    }

                    if (injectedAnyCode)
                    {
                        injectedAssemblyCount++;

                        if (settings.CodeInjectionLogLevel.HasFlag(LogLevel.Assembly))
                        {
                            var totalSeconds = watch.ElapsedMilliseconds / 1000f;
                            var asmDefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assemblyName);
                            var asmDefObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(asmDefPath);
                            Debug.Log($"Injected UI codes for assembly <b>'{assemblyName}'</b> in {totalSeconds:N4} seconds.", asmDefObj);
                        }
                    }
                }
            }
            catch (AccessViolationException)
            {
                Debug.LogWarning("AccessViolation, Retry!");
                EditorApplication.delayCall += InjectCodesWithReload;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("An error occurred when injecting UI codes.");
            }
            finally
            {
                assemblyResolver.Dispose();
                EditorUtility.ClearProgressBar();
            }

            return injectedAssemblyCount > 0;
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

        private static bool InjectCodesForAssembly(AssemblyDefinition assembly, LogLevel logLevel,
            TypeCache.TypeCollection injectorTypes, string progressBarTitle)
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

                // init properties
                injector.Assembly = assembly;
                injector.Methods = methodCache;
                injector.LogLevel = logLevel;
                injector.Progress = new SimpleProgress<float>(value =>
                {
                    string info = $"{injector.DisplayTitle} >>> {assembly.Name.Name}";
                    EditorUtility.DisplayProgressBar(progressBarTitle, info, (value + i) / injectorTypes.Count);
                });

                // execute
                injectedAnyCode |= injector.InjectAssembly();
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
    }
}