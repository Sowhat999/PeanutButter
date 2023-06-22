using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
// ReSharper disable MemberCanBePrivate.Global

namespace PeanutButter.Utils
{
    /// <summary>
    /// Find loaded types by name
    /// </summary>
    public static class TypeFinder
    {
        /// <summary>
        /// Attempt to find a type by short or namespaced-name, in all
        /// loaded assemblies
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Type TryFind(
            string name
        )
        {
            return TryFind(name, null);
        }

        /// <summary>
        /// Attempt to find a type by name or namespaced name across provided assemblies
        /// </summary>
        /// <param name="name"></param>
        /// <param name="assembly">assembly to search</param>
        /// <param name="moreAssemblies">(Optional) more assemblies to scan for the type</param>
        /// <returns></returns>
        public static Type TryFind(
            string name,
            Assembly assembly,
            params Assembly[] moreAssemblies
        )
        {
            return TryFind(
                name,
                StringComparison.Ordinal,
                new[] { assembly }
                    .Concat(moreAssemblies)
                    .ToArray()
            );
        }

        /// <summary>
        /// Attempt to find a type by name in the specified assemblies
        /// </summary>
        /// <param name="name"></param>
        /// <param name="assemblies"></param>
        /// <param name="stringComparison"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static Type TryFind(
            string name,
            StringComparison stringComparison,
            params Assembly[] assemblies
        )
        {
            assemblies ??= Array.Empty<Assembly>();
            assemblies = assemblies.Where(a => a is not null).ToArray();
            if (assemblies.Length == 0)
            {
                assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .OrderBy(a => a.FullName)
                    .ToArray();
            }

            return TypeLookup.FindOrAdd(
                name,
                () =>
                {
                    foreach (var asm in assemblies)
                    {
                        try
                        {
                            var types = FindTypesIn(asm);
                            var potential = FindBestNameMatch(
                                types,
                                name,
                                stringComparison
                            );
                            if (potential is not null)
                            {
                                return potential;
                            }
                        }
                        catch
                        {
                            // suppress: can't read from assembly?
                        }
                    }

                    return null;
                }
            );
        }

        private static Type FindBestNameMatch(
            Type[] types,
            string name,
            StringComparison stringComparison
        )
        {
            foreach (var t in types)
            {
                try
                {
                    if (t.Name.Equals(name, stringComparison))
                    {
                        return t;
                    }

                    if (t.FullName?.Equals(name, stringComparison) ?? false)
                    {
                        return t;
                    }
                }
                catch
                {
                    // suppress: can't read from type
                }
            }

            return null;
        }

        private static readonly ConcurrentDictionary<string, Type> TypeLookup = new();

        private static readonly ConcurrentDictionary<Assembly, Type[]> AssemblyTypes = new();

        /// <summary>
        /// Returns all the exported types from an assembly, if possible
        /// (some assemblies will throw when queried - in this case you'll
        /// get back an empty array)
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static Type[] FindTypesIn(Assembly assembly)
        {
            return AssemblyTypes.FindOrAdd(
                assembly,
                () =>
                {
                    try
                    {
                        return assembly.GetExportedTypes()
                            .OrderBy(t => t.FullName)
                            .ToArray();
                    }
                    catch
                    {
                        return Array.Empty<Type>();
                    }
                }
            );
        }
    }
}