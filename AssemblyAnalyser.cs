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
            var referencingAssembly = AssemblyDefinition.ReadAssembly(path);

            return referencingAssembly.Modules
                .SelectMany(a => a.Types)
                .SelectMany(GetAllBodies)
                .SelectMany(GetAllCalls);
        }

        private static IEnumerable<Call> GetAllCalls(MethodDefinition definition)
        {
            if (definition == null) yield break;
            foreach (var instruction in definition.Body.Instructions.Where(IsCall))
            {
                yield return new Call(instruction);
            }
        }

        private static bool IsCall(Instruction x)
        {
            return x.OpCode == OpCodes.Call || x.OpCode == OpCodes.Callvirt || x.OpCode == OpCodes.Calli;
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