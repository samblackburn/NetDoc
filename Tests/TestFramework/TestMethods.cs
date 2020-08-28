﻿using System;
using System.IO;
using NetDoc;
using NUnit.Framework;

namespace Tests.TestFramework
{
    abstract class TestMethods
    {
        protected static void ContractAssertionShouldCompile(string referencing, string referenced)
        {
            var (referencedDll, referencingDll) = ClrAssemblyCompiler.CompileDlls(referencing, referenced);
            using var writer = new StringWriter();
            Program.CreateContractAssertions(writer, "", new[] {referencedDll}, new[] {referencingDll});
            Console.WriteLine(writer.ToString());
            ClrAssemblyCompiler.CompileDlls(writer + new ContractClassWriter().UtilsSource, referenced);
            StringAssert.Contains("private void UsedByTestAssembly()", writer.ToString(),
                "We should have created a method to contain the assertions for this assembly");
        }

        protected static void ContractAssertionShouldBeEmpty(string referencing, string referenced)
        {
            var (referencedDll, referencingDll) = ClrAssemblyCompiler.CompileDlls(referencing, referenced);
            using var writer = new StringWriter();
            Program.CreateContractAssertions(writer, "", new[] {referencedDll}, new[] {referencingDll});
            Console.WriteLine(writer.ToString());
            StringAssert.DoesNotContain("private void UsedByTestAssembly()", writer.ToString(),
                "There shouldn't be a contract assertion");
        }

        [TestCase("int x;", "Class2", null, ExpectedResult = "public class Class2 {int x;}")]
        [TestCase("int x;", "Class2", "ns", ExpectedResult = "namespace ns {public class Class2 {int x;}}")]
        public static string Class(string contents, string className = "Class1", string? ns = "Name.Space")
        {
            return string.IsNullOrEmpty(ns)
                ? $"public class {className} {{{contents}}}"
                : $"namespace {ns} {{{Class(contents, className, null)}}}";
        }

        [TearDown]
        public void TearDown()
        {
            TempDir.CleanUp();
        }
    }
}
