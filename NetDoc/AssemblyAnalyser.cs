using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace NetDoc
{
    internal class AssemblyAnalyser
    {
        public AssemblyAnalyser(IEnumerable<string> referencedDlls)
        {
            ReferencedDlls = referencedDlls;
        }

        private IEnumerable<string> ReferencedDlls { get; }

        internal IEnumerable<Call> AnalyseAssembly(string path, IAssemblyResolver resolver)
        {
            try
            {
                using var referencingAssembly = AssemblyDefinition.ReadAssembly(path, new ReaderParameters{AssemblyResolver = resolver});

                var types = referencingAssembly.Modules
                    .SelectMany(a => a.Types).ToHashSet();
                return types
                    .SelectMany(GetAllBodies)
                    .SelectMany(GetAllCalls)
                    .Where(x => !types.Contains(DeclaringType(x)))
                    .ToList();
            }
            catch (BadImageFormatException ex)
            {
                Console.WriteLine($"{ex.Message}: {path}");
                return new Call[0];
            }
        }

        /// <summary>
        /// For nested classes, returns the outer class
        /// </summary>
        private static TypeReference DeclaringType(Call call)
        {
            var dt = call.DeclaringType;
            while (dt.DeclaringType != null)
            {
                dt = dt.DeclaringType;
            }

            return dt;
        }

        private IEnumerable<Call> GetAllCalls(MethodDefinition definition)
        {
            if (definition?.Body == null) yield break;
            foreach (var instruction in definition.Body.Instructions.Where(IsCall).Where(x => !IsBaseClass(x, definition)))
            {
                yield return new Call(instruction, ReferencedDlls);
            }
        }

        private static bool IsBaseClass(Instruction instruction, MethodDefinition definition)
        {
            var type = definition.DeclaringType.BaseType;
            return type?.FullName == ((MemberReference) instruction.Operand).DeclaringType.FullName;
        }

        private static bool IsCall(Instruction x)
        {
            return x.OpCode == OpCodes.Call ||
                   x.OpCode == OpCodes.Callvirt ||
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