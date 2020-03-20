using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NetDoc
{
    class Program
    {
        static void Main()
        {
            const string sln = @"C:\Work\SQLCompareEngine\SQLCompare.sln";
            var nonObfuscatedBuildFolder = @"C:\Work\SQLDependencyTracker\Build\Debug\net472";
            //const string sln = @"C:\Users\Sam.Blackburn\source\repos\NetDoc\NetDoc.sln";
            //var nonObfuscatedBuildFolder = @"C:\Users\Sam.Blackburn\source\repos\NetDoc\bin\Debug\net472";
            var assemblies = RedgateAssembliesInFolder(nonObfuscatedBuildFolder, Path.GetDirectoryName(sln)).ToList();
            Console.WriteLine("Analysing {0} Assemblies", assemblies.Count());
            var calls = assemblies.SelectMany(AssemblyAnalyser.AnalyseAssembly).ToList();
            Console.WriteLine("Modifying solution...");
            //var modifier = new SolutionModifier(new [] {new Rewriter(calls)}, sln);
            //DumpErrors(modifier.ModifySolution);
            var contract = new ContractClassWriter();
            contract.ProcessCalls("DependencyTracker", calls);
        }

        private static IEnumerable<string> RedgateAssembliesInFolder(string include, string exclude)
        {
            var allAssemblies = Directory.EnumerateFiles(include, "RedGate.*.dll", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(include, "*.exe", SearchOption.AllDirectories));
            var consumedAssemblies = Directory.EnumerateFiles(exclude, "RedGate.*.dll", SearchOption.AllDirectories)
                .Select(Path.GetFileName).ToHashSet();
            return allAssemblies.Where(x => !consumedAssemblies.Contains(Path.GetFileName(x)));
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
}
