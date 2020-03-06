using System;
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
            Console.WriteLine("Analysing Assembly...");
            var nonObfuscatedBuildFolder = @"C:\Work\SQLDependencyTracker\Build\Debug\net472";
            var assemblies = Directory.EnumerateFiles(nonObfuscatedBuildFolder, "RedGate.*.dll")
                .Concat(Directory.EnumerateFiles(nonObfuscatedBuildFolder, "RedGate.*.exe"));
            var calls = assemblies.SelectMany(AssemblyAnalyser.AnalyseAssembly).ToList();
            Console.WriteLine("Modifying solution...");
            var modifier = new SolutionModifier(new [] {new Rewriter(calls)}, sln);

            DumpErrors(modifier.ModifySolution);
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
