﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mono.Cecil;

namespace NetDoc
{
    class Program
    {
        static void Main()
        {
            const string repoRoot = @"C:\Users\Sam\source\repos\";
            const string sln = repoRoot + @"SQLCompareEngine\SQLCompare.sln";
            const string referenced = repoRoot + @"SQLCompareEngine\Engine\SQLCompareEngine\Engine\bin\Debug\net472\RedGate.SQLCompare.Engine.dll";
            const string assertionsOut = repoRoot + @"SQLCompareEngine\Engine\SQLCompareEngine\Testing\UnitTests\ContractAssertions.cs";
            var nonObfuscatedBuildFolder = repoRoot + @"SQLCompareEngine\UI\SqlServerGUI\Schema\SchemaGUI\bin\Debug\net472";
            var assemblies = RedgateAssembliesInFolder(nonObfuscatedBuildFolder, Path.GetDirectoryName(sln)).ToList();
            Console.WriteLine("Analysing {0} Assemblies", assemblies.Count());
            //var calls = assemblies.SelectMany(AssemblyAnalyser.AnalyseAssembly).ToList();
            Console.WriteLine("Modifying solution...");
            //var modifier = new SolutionModifier(new [] {new Rewriter(calls)}, sln);
            //DumpErrors(modifier.ModifySolution);
            CreateContractAssertions(assertionsOut, referenced, assemblies);
        }

        public static void CreateContractAssertions(string assertionsOut, string referenced, IEnumerable<string> assemblies)
        {
            var contract = new ContractClassWriter();

            using var outFile = File.Open(assertionsOut, FileMode.Create);
            using var writer = new StreamWriter(outFile);
            var referencedTypes = AssemblyDefinition.ReadAssembly(referenced).Modules.SelectMany(a => a.Types)
                .Select(x => $"{x.Namespace}::{x.Name.Split('`')[0]}").ToHashSet();
            writer.Write(@"// ReSharper disable UnusedMember.Local
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
// ReSharper disable once UnusedType.Global
internal abstract class ContractAssertions
{
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
            candidateTypes.Contains($"{arg.Namespace}::{arg.Type}");

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
