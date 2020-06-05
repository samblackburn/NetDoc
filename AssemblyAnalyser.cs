using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace NetDoc
{
    internal class AssemblyAnalyser
    {
        internal static IEnumerable<Call> AnalyseAssembly(string path)
        {
            using var referencingAssembly = AssemblyDefinition.ReadAssembly(path);

            return referencingAssembly.Modules
                .SelectMany(a => a.Types)
                .SelectMany(GetAllBodies)
                .SelectMany(GetAllCalls)
                .ToList();
        }

        private static IEnumerable<Call> GetAllCalls(MethodDefinition definition)
        {
            if (definition?.Body == null) yield break;
            foreach (var instruction in definition.Body.Instructions.Where(IsCall))
            {
                yield return new Call(instruction, definition);
            }
        }

        private static bool IsCall(Instruction x)
        {
            return x.OpCode == OpCodes.Call ||
                   x.OpCode == OpCodes.Callvirt ||
                   x.OpCode == OpCodes.Calli ||
                   x.OpCode == OpCodes.Newobj ||
                   x.OpCode == OpCodes.Ldfld ||
                   x.OpCode == OpCodes.Ldsfld ||
                   x.OpCode == OpCodes.Stfld ||
                   x.OpCode == OpCodes.Stsfld;
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