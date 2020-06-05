using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using NetDoc.Tests;
using NUnit.Framework;

namespace RedGate.SQLCompare.Engine.TestUtils
{
    public enum NetFrameworkVersion
    {
        Net35,
        Net45
    }

    public static class ClrAssemblyCompiler
    {
        public static string[] CompileDlls(string testAssemblySource, string referencedAssemblySource)
        {
            var referencedDll = CompileDll(TempDir.Get(), NetFrameworkVersion.Net35, "ReferencedAssembly",
                referencedAssemblySource);
            var testDll = CompileDll(TempDir.Get(), NetFrameworkVersion.Net35, "TestAssembly", testAssemblySource,
                referencedDll);
            return new[] {referencedDll, testDll};
        }

        private static string Net4Compiler { get; } = Path.Combine(PackagesFolder, "microsoft.net.compilers", "3.6.0", "tools", "csc.exe");

        private static string GetReferenceAssemblyPath(NetFrameworkVersion frameworkVersion)
        {
            string moniker, version;

            switch (frameworkVersion)
            {
                case NetFrameworkVersion.Net35:
                    moniker = "net20";
                    version = "v2.0";
                    break;
                case NetFrameworkVersion.Net45:
                    moniker = "net45";
                    version = "v4.5";
                    break;
                default:
                    throw new AssertionException($"frameworkVersion {frameworkVersion} is not Net35 or Net45");
            }

            var packageDir = new DirectoryInfo(Path.Combine(PackagesFolder, "microsoft.netframework.referenceassemblies." + moniker));
            Assert.That(packageDir.Exists, $"Directory {packageDir.FullName} does not exist");
            var versions = packageDir.GetDirectories();
            Assert.That(versions.Length, Is.EqualTo(1), $"Directory {packageDir.FullName} does not contain exactly one version");
            var referenceDir = new DirectoryInfo(Path.Combine(versions.Single().FullName, "build", ".NETFramework", version));
            Assert.That(referenceDir.Exists, $"Directory {referenceDir.FullName} does not exist");

            return referenceDir.FullName;
        }

        public static string PackagesFolder => Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), ".nuget", "packages");

        // Copied from what dotnet build references,
        // with DLLs that aren't present in the NuGet reference DLLs package removed,
        // with System.Transactions.dll added to net45, as we use it in one test.
        private static readonly Dictionary<NetFrameworkVersion, string[]> BaseReferenceAssemblies =
            new Dictionary<NetFrameworkVersion, string[]>
            {
                [NetFrameworkVersion.Net35] = new[] { "mscorlib.dll", "System.Data.dll", "System.dll", "System.Drawing.dll", "System.Xml.dll" },
                [NetFrameworkVersion.Net45] = new[] { "mscorlib.dll", "System.Core.dll", "System.Data.dll", "System.dll", "System.Drawing.dll", "System.IO.Compression.FileSystem.dll", "System.Numerics.dll", "System.Runtime.Serialization.dll", "System.Transactions.dll", "System.Xml.dll", "System.Xml.Linq.dll" }
            };

        private static IEnumerable<string> GetReferenceAssemblies(NetFrameworkVersion frameworkVersion)
        {
            var path = GetReferenceAssemblyPath(frameworkVersion);
            return BaseReferenceAssemblies[frameworkVersion].Select(r => Path.Combine(path, r));
        }

        private static string CompileDll(string tempDir, NetFrameworkVersion frameworkVersion, string assemblyName, string sourceCode, string referencedDll = null)
        {
            var outputDll = Path.Combine(tempDir, $"{assemblyName}.dll");
            var sourcePath = Path.Combine(tempDir, $"code{assemblyName}.cs");
            File.WriteAllText(sourcePath, sourceCode);

            string compiler;
            var arguments = new StringBuilder();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.That(File.Exists(Net4Compiler), $"File {Net4Compiler} not found");
                compiler = Net4Compiler;
            }
            else
            {
                compiler = "dotnet";
                var cscPath = Directory.GetFiles("/usr/share/dotnet/sdk", "csc.dll", SearchOption.AllDirectories).First();
                arguments.Append(cscPath);
                arguments.Append(' ');
            }

            arguments.Append("/noconfig /nostdlib /out:\"");
            arguments.Append(outputDll);
            arguments.Append("\" /target:library /utf8output /deterministic /platform:anycpu /nologo /subsystemversion:6.01");

            foreach (var referenceAssembly in GetReferenceAssemblies(frameworkVersion))
            {
                arguments.Append(" /reference:\"");
                arguments.Append(referenceAssembly);
                arguments.Append('"');
            }

            if (referencedDll != null)
            {
                arguments.Append(" /reference:\"");
                arguments.Append(referencedDll);
                arguments.Append('"');
            }

            arguments.Append(" \"");
            arguments.Append(sourcePath);
            arguments.Append('"');

            if (frameworkVersion == NetFrameworkVersion.Net45)
            {
                var net45AttributesPath = Path.Combine(tempDir, "net45.cs");
                // This file is an exact copy of what MSBuild injects into .NET Framework 4.5 projects
                File.WriteAllText(net45AttributesPath, @"// <autogenerated />
using System;
using System.Reflection;
[assembly: global::System.Runtime.Versioning.TargetFrameworkAttribute("".NETFramework,Version=v4.5"", FrameworkDisplayName = "".NET Framework 4.5"")]");
                arguments.Append(" \"");
                arguments.Append(net45AttributesPath);
                arguments.Append('"');
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = compiler,
                Arguments = arguments.ToString(),
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                process.Start();
                process.WaitForExit();
                var stderr = process.StandardError.ReadToEnd();
                var stdout = process.StandardOutput.ReadToEnd();
                var exitCode = process.ExitCode;
                if (exitCode != 0 || !string.IsNullOrEmpty(stderr))
                {
                    Console.WriteLine($"Output of {startInfo.FileName} {startInfo.Arguments}");
                }
                if (!string.IsNullOrEmpty(stderr))
                {
                    Console.WriteLine(stderr);
                }
                if (exitCode != 0 && !string.IsNullOrEmpty(stdout))
                {
                    Console.WriteLine(stdout);
                }
                Assert.That(exitCode, Is.Zero, "Failed to compile CLR assembly (exit code non-zero)");
            }

            return outputDll;
        }
    }
}