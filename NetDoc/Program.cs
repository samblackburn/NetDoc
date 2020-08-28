using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
                var assertionFileName = Path.Combine(assertionsOut, $"{repoName}.cs");
                var oldContractAssertion = File.Exists(assertionFileName) ? File.ReadAllText(assertionFileName) : "";
                using var outFile = File.Open(assertionFileName, FileMode.Create);
                using var writer2 = new StreamWriter(outFile);
                using var writer = new StringWriter();
                ContractClassWriter.CreateContractAssertions(writer, repoName, consumed, assemblies);
                IgnorancePreserver.PreserveIgnoredAssertions(oldContractAssertion, writer.ToString(), writer2);
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
}
