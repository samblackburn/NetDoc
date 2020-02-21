using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace NetDoc
{
    class Program
    {
        static void Main()
        {
            var referencingAssembly = AssemblyDefinition.ReadAssembly("netdoc.exe");

            var calls = referencingAssembly.Modules
                .SelectMany(a => a.Types)
                .SelectMany(GetAllBodies)
                .SelectMany(y => y.Body.Instructions)
                .Where(IsCall)
                .Select(GetNameOfTarget);

            foreach (var call in calls)
            {
                Console.WriteLine(call);
            }
        }

        private static bool IsCall(Instruction x)
        {
            return x.OpCode == OpCodes.Call || x.OpCode == OpCodes.Callvirt || x.OpCode == OpCodes.Calli;
        }

        /// <usage></usage>
        private static string GetNameOfTarget(Instruction instruction)
        {
            var operand = (MethodReference) instruction.Operand;
            var parameters = string.Join(", ", operand.Parameters.Select(p => p.ParameterType).Select(GetTypeName));
            return $"{GetTypeName(operand.DeclaringType)}.{operand.Name}({parameters})";
        }

        private static string GetTypeName(TypeReference type)
        {
            var nameSpace = type.Namespace;
            var className = type.Name.Split('`')[0];
            var generics = type is GenericInstanceType git
                ? $"<{String.Join(", ", git.GenericArguments.Select(x => x.Name.Replace("!!", "T")))}>"
                : "";
            return $"{nameSpace}.{className}{generics}";
        }

        private static IEnumerable<MethodDefinition> GetAllBodies(TypeDefinition type)
        {
            foreach (var method in type.Methods)
            {
                yield return method;
            }

            foreach (var property in type.Properties)
            {
                yield return property.GetMethod;
                yield return property.SetMethod;
            }
        }
    }
}
