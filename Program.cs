using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NetDoc
{
    class Program
    {
        static void Main()
        {
            const string sln = @"C:\Users\Sam.Blackburn\source\repos\NetDoc\NetDoc.sln";

            var calls = AssemblyAnalyser.AnalyseAssembly("netdoc.exe");

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
