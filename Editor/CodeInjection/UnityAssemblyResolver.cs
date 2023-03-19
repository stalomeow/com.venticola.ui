using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace VentiColaEditor.UI.CodeInjection
{
    internal class UnityAssemblyResolver : BaseAssemblyResolver
    {
        private readonly Dictionary<string, AssemblyDefinition> m_Cache;

        public UnityAssemblyResolver()
        {
            m_Cache = new Dictionary<string, AssemblyDefinition>(StringComparer.Ordinal);

            // add search-directories of unity
            var directorys = new HashSet<string>(GetSearchDirectories());

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                {
                    continue;
                }

                string directory = Path.GetDirectoryName(assembly.Location);

                if (directorys.Add(directory))
                {
                    AddSearchDirectory(directory);
                }
            }
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            string fullName = name.FullName;

            if (!m_Cache.TryGetValue(fullName, out AssemblyDefinition assembly))
            {
                assembly = base.Resolve(name, parameters);
                m_Cache.Add(fullName, assembly);
            }

            return assembly;
        }

        public void AddAssemblyToCache(AssemblyDefinition assembly)
        {
            m_Cache.Add(assembly.FullName, assembly);
        }

        protected override void Dispose(bool disposing)
        {
            foreach (AssemblyDefinition assembly in m_Cache.Values)
            {
                assembly.Dispose();
            }

            m_Cache.Clear();
            base.Dispose(disposing);
        }
    }
}