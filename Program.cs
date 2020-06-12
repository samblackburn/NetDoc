using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Mono.Cecil;

namespace NetDoc
{
    class Program
    {
        static void Main()
        {
            const string repoRoot = @"C:\Work\";
            const string consumedRepo = repoRoot + @"SQLCompareEngine\";
            const string referenced = consumedRepo + @"Engine\SQLCompareEngine\Engine\bin\Debug\net472\RedGate.SQLCompare.Engine.dll";
            var repos = new[] {"SQLDoc", "SQLDataGenerator", "SQLTest", "SQLPrompt", "SQLSourceControl"};

            foreach (var repoName in repos)
            {
                var assemblies = RedgateAssembliesInFolder(Path.Combine(repoRoot, repoName), consumedRepo).ToList();
                Console.WriteLine("Analysing {0} Assemblies", assemblies.Count());
                Console.WriteLine("Modifying solution...");

                var assertionsOut = Path.Combine(repoRoot, @"SQLCompareEngine\Engine\SQLCompareEngine\Testing\UnitTests\ContractAssertions\", $"{repoName}.cs");
                Directory.CreateDirectory(Path.GetDirectoryName(assertionsOut));
                using var outFile = File.Open(assertionsOut, FileMode.Create);
                using var writer = new StreamWriter(outFile);
                CreateContractAssertions(writer, repoName, referenced, assemblies);
            }
        }

        public static void CreateContractAssertions(TextWriter writer, string referencing, string referenced, IEnumerable<string> assemblies)
        {
            var contract = new ContractClassWriter();
            using var assemblyDefinition = AssemblyDefinition.ReadAssembly(referenced);
            var referencedTypes = assemblyDefinition.Modules.SelectMany(a => a.Types)
                .Select(x => $"{x.Namespace}::{x.Name.Split('`')[0]}").ToHashSet();
            writer.Write($@"// ReSharper disable UnusedMember.Local
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
// ReSharper disable once UnusedType.Global
internal abstract class {referencing}ContractAssertions
{{
    protected abstract T Create<T>();
    protected abstract void CheckReturnType<T>(T param);

");
            foreach (var assembly in assemblies)
            {
                var calls = AssemblyAnalyser.AnalyseAssembly(assembly)
                    .Where(call => TargetsReferencedAssembly(call, referencedTypes));
                var assemblyName = Path.GetFileNameWithoutExtension(assembly).Replace(".", "");
                foreach (var x in contract.ProcessCalls(assemblyName, calls))
                {
                    writer.WriteLine($"    {x}");
                }
            }

            writer.Write(@"}");
            writer.Flush();
        }

        private static bool TargetsReferencedAssembly(Call arg, ICollection<string> candidateTypes) =>
            candidateTypes.Contains(arg.ContainingTypeName);

        private static IEnumerable<string> RedgateAssembliesInFolder(string include, string exclude)
        {
            var allAssemblies = Directory.EnumerateFiles(include, "RedGate.*.dll", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(include, "*.exe", SearchOption.AllDirectories))
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "packages" + Path.DirectorySeparatorChar))
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "."));
            var consumedAssemblies = Directory.EnumerateFiles(exclude, "RedGate.*.dll", SearchOption.AllDirectories)
                .Select(Path.GetFileName).ToHashSet();
            return allAssemblies.Where(x => !consumedAssemblies.Contains(Path.GetFileName(x)))
                .OrderBy(Path.GetFileName)
                .Distinct(new FileNameOnlyComparer());
        }

        private static void DumpErrors(Func<Task> asyncMethod)
        {
            try
            {
                asyncMethod().Wait();
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is ReflectionTypeLoadException rtle)
                {
                    foreach (var loader in rtle.LoaderExceptions)
                    {
                        Console.WriteLine(loader);
                    }
                }
                else
                {
                    Console.WriteLine(ex);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
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
