using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using rcx_parse_cli;

namespace NetDoc
{
    public class Program
    {
        static void Main(string[] args)
        {
            if (!args.Any()) args = new[]
            {
                "--referencedFile", @"C:\Work\SQLCompareEngine\Engine\SQLCompareEngine\Engine\bin\Debug\net472\RedGate.SQLCompare.Engine.dll",
                "--referencedFile", @"C:\Work\SQLCompareEngine\Engine\SQLDataCompareEngine\Engine\bin\Debug\net472\RedGate.SQLDataCompare.Engine.dll",
                "--referencingDir", @"C:\Work\SQLPrompt",
                "--referencingDir", @"C:\Work\SQLSourceControl",
                "--referencingDir", @"C:\Work\SQLDataGenerator",
                "--referencingDir", @"C:\Work\SQLDoc",
                "--excludeDir",     @"C:\Work\SQLCompareEngine",
                "--outDir",         @"C:\Work\SQLCompareEngine\Engine\SQLDataCompareEngine\Testing\UnitTests\ContractAssertions\"
            };

            var consumers = new List<string?>();
            var consumed = new List<string?>();
            string? assertionsOut = null;
            var exclude = new List<string?>();

            new ParserBuilder()
                .WithNameAndVersion("NetDoc", Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown")
                .SetUi(Console.WriteLine, Environment.Exit)
                .Add(new Option("--referencingDir", consumers.Add))
                .Add(new Option("--referencedFile", consumed.Add))
                .Add(new Option("--excludeDir", exclude.Add))
                .Add(new Option("--outDir", x => assertionsOut = x))
                .Build().Parse(args);

            foreach (var repoPath in consumers)
            {
                var repoName = Path.GetFileName(repoPath);
                var assemblies =
                    AssembliesInFolder(repoPath ?? ".", exclude.Concat(consumed.Select(Path.GetDirectoryName))).ToList();
                Console.WriteLine("Generating assertions for {0} assemblies in {1}...", assemblies.Count, repoName);

                Directory.CreateDirectory(assertionsOut);
                using var outFile = File.Open(Path.Combine(assertionsOut, $"{repoName}.cs"), FileMode.Create);
                using var writer = new StreamWriter(outFile);
                CreateContractAssertions(writer, repoName, consumed, assemblies);
            }
        }

        public static void CreateContractAssertions(TextWriter writer, string referencing, IEnumerable<string> referenced, IEnumerable<string> assemblies)
        {
            var contract = new ContractClassWriter();

            writer.Write($@"// ReSharper disable UnusedMember.Local
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
// ReSharper disable once UnusedType.Global
internal abstract class {referencing}ContractAssertions
{{
    protected abstract T Create<T>();
    protected abstract void CheckReturnType<T>(T param);
    private class Ref<T> {{ public T Any = default; }}

");
            using var resolver = new DefaultAssemblyResolver();
            foreach (var r in referenced)
            {
                resolver.AddSearchDirectory(Path.GetDirectoryName(r));
            }

            var assemblyDefinitions = referenced.Select(AssemblyDefinition.ReadAssembly).ToList();
            var referencedTypes = assemblyDefinitions.SelectMany(d => d.Modules).SelectMany(a => a.Types)
                .Where(t => t.IsPublic)
                .Select(x => $"{x.Namespace}::{x.Name}")
                .Where(x => x != "::<Module>")
                .ToHashSet();
            foreach (var assembly in assemblies)
            {
                var calls = AssemblyAnalyser.AnalyseAssembly(assembly, resolver)
                    .Where(call => TargetsReferencedAssembly(call, referencedTypes));
                var assemblyName = Path.GetFileNameWithoutExtension(assembly).Replace(".", "");
                foreach (var x in contract.ProcessCalls(assemblyName, calls))
                {
                    writer.WriteLine($"    {x}");
                }
            }

            writer.Write(@"}");
            writer.Flush();

            foreach (var ad in assemblyDefinitions)
            {
                ad.Dispose();
            }
        }

        private static bool TargetsReferencedAssembly(Call arg, ICollection<string> candidateTypes) =>
            candidateTypes.Contains(arg.ContainingTypeName);

        private static IEnumerable<string> AssembliesInFolder(string include, IEnumerable<string> exclude)
        {
            var allAssemblies = Directory.EnumerateFiles(include, "*.dll", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(include, "*.exe", SearchOption.AllDirectories))
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "packages" + Path.DirectorySeparatorChar))
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "."));
            var consumedAssemblies = exclude
                .SelectMany(ex => Directory.EnumerateFiles(ex, "*.dll", SearchOption.AllDirectories))
                .Select(Path.GetFileName).ToHashSet();
            return allAssemblies.Where(x => !consumedAssemblies.Contains(Path.GetFileName(x)))
                .OrderBy(Path.GetFileName)
                .Distinct(new FileNameOnlyComparer());
        }
    }

    internal class FileNameOnlyComparer : IEqualityComparer<string>
    {
        public bool Equals(string left, string right)
        {
            if (left == null) return right == null;
            return Path.GetFileName(left).Equals(Path.GetFileName(right), StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(string obj)
        {
            return Path.GetFileName(obj).GetHashCode();
        }
    }
}
