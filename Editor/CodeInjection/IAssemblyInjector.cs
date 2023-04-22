using Mono.Cecil;
using System;

namespace VentiColaEditor.UI.CodeInjection
{
    public interface IAssemblyInjector
    {
        AssemblyDefinition Assembly { get; set; }

        MethodReferenceCache Methods { get; set; }

        Action<float> ProgressCallback { get; set; }

        bool Execute();
    }
}