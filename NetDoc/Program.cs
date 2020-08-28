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
            var consumers = new List<string>();
            var consumed = new List<string>();
            string? assertionsOut = null;
            bool help = false;
            var exclude = new List<string>();

            new ParserBuilder()
                .WithNameAndVersion("NetDoc",
                    Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown")
                .SetUi(Console.WriteLine, Environment.Exit)
                .Add(new Option("--referencingDir", s => consumers.Add(s!)))
                .Add(new Option("--referencedFile", s => consumed.Add(s!)))
                .Add(new Option("--excludeDir", s => exclude.Add(s!)))
                .Add(new Option("--outDir", x => assertionsOut = x))
                .Add(new Option("--help", x => help = true))
                .Build().Parse(args);

            if (help || assertionsOut == null)
            {
                PrintHelp();
                return;
            }

            var utilsFile = Path.Combine(assertionsOut, "ContractAssertionUtils.cs");
            File.WriteAllText(utilsFile, new ContractClassWriter().UtilsSource);

            foreach (var repoPath in consumers)
            {
                var repoName = Path.GetFileName(repoPath).ToTitleCase();
                var assemblies =
                    AssembliesInFolder(repoPath ?? ".", exclude.Concat(consumed.Select(Path.GetDirectoryName))).ToList();
                Console.WriteLine("Generating assertions for {0} assemblies in {1}...", assemblies.Count, repoName);

                Directory.CreateDirectory(assertionsOut);
                using var outFile = File.Open(Path.Combine(assertionsOut, $"{repoName}.cs"), FileMode.Create);
                using var writer = new StreamWriter(outFile);
                CreateContractAssertions(writer, repoName, consumed, assemblies);
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("    --referencingDir  The folder containing your referencing dlls.");
            Console.WriteLine("    --referencedFile  Your referencing dll.");
            Console.WriteLine("    --excludeDir      Files in the referencing dir will be excluded if there");
            Console.WriteLine("                      is a file with the same name in the exclude dir.");
            Console.WriteLine("    --outDir          Where to output the.cs files containing the contract");
            Console.WriteLine("                      assertions. This switch should only be used once.");
        }

        public static void CreateContractAssertions(TextWriter writer, string referencing, IEnumerable<string> referenced, IEnumerable<string> assemblies)
        {
            var contract = new ContractClassWriter();

            writer.Write($@"using static ContractAssertionUtils;
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable once CheckNamespace
internal abstract class {referencing}ContractAssertions
{{
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
                var assemblyName = Path.GetFileNameWithoutExtension(assembly).ToTitleCase();
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

        private static IEnumerable<string> AssembliesInFolder(string include, IEnumerable<string?> exclude)
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

    internal class FileNameOnlyComparer : IEqualityComparer<string?>
    {
        public bool Equals(string? left, string? right)
        {
            if (left == null) return right == null;
            return Path.GetFileName(left).Equals(Path.GetFileName(right), StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(string? obj)
        {
            return Path.GetFileName(obj)?.GetHashCode() ?? -1;
        }
    }
}
