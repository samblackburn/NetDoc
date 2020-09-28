using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using NetDoc.Utils;

namespace NetDoc
{
    public static class ContractClassWriter
    {
        public static void CreateContractAssertions(TextWriter writer, string referencing, IEnumerable<string> referenced, IEnumerable<string> assemblies)
        {
            var assemblyDefinitions = referenced.Select(AssemblyDefinition.ReadAssembly).ToList();
            try
            {
                writer.Write(Header(referencing));
                using var resolver = new DefaultAssemblyResolver();
                foreach (var r in referenced)
                {
                    resolver.AddSearchDirectory(Path.GetDirectoryName(r));
                }

                var referencedTypes = assemblyDefinitions.SelectMany(d => d.Modules).SelectMany(a => a.Types)
                    .Where(t => t.IsPublic)
                    .Select(x => $"{x.Namespace}::{x.Name}")
                    .Where(x => x != "::<Module>")
                    .ToHashSet();
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var referencedDlls = assemblyDefinitions.Select(a => a.Name.Name).ToHashSet();
                        var calls = new AssemblyAnalyser(referencedDlls).AnalyseAssembly(assembly, resolver)
                            .Where(call => TargetsReferencedAssembly(call, referencedTypes));
                        var assemblyName = Path.GetFileNameWithoutExtension(assembly).ToTitleCase();
                        foreach (var x in ProcessCalls(assemblyName, calls))
                        {
                            writer.WriteLine($"    {x}");
                        }
                    }
                    catch
                    {
                        Console.Error.WriteLine($"Referencing assembly: {assembly}");
                        throw;
                    }
                }

                writer.Write(Footer);
            }
            finally
            {
                writer.Flush();

                foreach (var ad in assemblyDefinitions)
                {
                    ad.Dispose();
                }
            }
        }

        private static bool TargetsReferencedAssembly(Call arg, ICollection<string> candidateTypes) =>
            candidateTypes.Contains(arg.ContainingTypeName);

        private static IEnumerable<string> ProcessCalls(string consumerName, IEnumerable<Call> calls)
        {
            var relevantCalls = calls
                .Where(c => !Exclusions.Contains(c.Method))
                .ToList();
            if (!relevantCalls.Any()) yield break;

            yield return $"private void UsedBy{consumerName}()";
            yield return @"{";

            foreach (var invocation in relevantCalls.Select(c => c.Invocation).ToHashSet())
            {
                yield return $"    {invocation}";
            }

            yield return @"}";
        }

        private static readonly HashSet<string> Exclusions = new HashSet<string>
        {
            "op_Equality",
            "op_Inequality",
            "ComputeStringHash",
            "Binding",
            "Command",
        };

        public static string UtilsSource => @"// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
// ReSharper disable once UnusedType.Global

internal static class ContractAssertionUtils
{
    internal static T Create<T>() => default;
    internal static void CheckReturnType<T>(T param) {}
}

internal class Ref<T> { public T Any = default; }
";

        private static string Header(string referencingClassName) => $@"using static ContractAssertionUtils;
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable once CheckNamespace
internal abstract class {referencingClassName}ContractAssertions
{{
";

        private static string Footer => "}\r\n";
    }
}