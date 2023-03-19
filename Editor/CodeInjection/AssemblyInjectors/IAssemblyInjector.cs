using Mono.Cecil;
using System;

namespace VentiColaEditor.UI.CodeInjection.AssemblyInjectors
{
    internal interface IAssemblyInjector
    {
        string DisplayTitle { get; }

        AssemblyDefinition Assembly { get; set; }

        MethodReferenceCache Methods { get; set; }

        InjectionTasks Tasks { get; set; }

        LogLevel LogLevel { get; set; }

        IProgress<float> Progress { get; set; }

        bool InjectAssembly();
    }
}