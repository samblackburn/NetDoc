﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace NetDoc
{
    internal class AssemblyAnalyser
    {
        internal static IEnumerable<Call> AnalyseAssembly(string path, IAssemblyResolver resolver)
        {
            try
            {
                using var referencingAssembly = AssemblyDefinition.ReadAssembly(path, new ReaderParameters{AssemblyResolver = resolver});

                return referencingAssembly.Modules
                    .SelectMany(a => a.Types)
                    .SelectMany(GetAllBodies)
                    .SelectMany(GetAllCalls)
                    .ToList();
            }
            catch (BadImageFormatException ex)
            {
                Console.WriteLine($"{ex.Message}: {path}");
                return new Call[0];
            }
        }

        private static IEnumerable<Call> GetAllCalls(MethodDefinition definition)
        {
            if (definition?.Body == null) yield break;
            foreach (var instruction in definition.Body.Instructions.Where(IsCall).Where(x => !IsBaseClass(x, definition)))
            {
                yield return new Call(instruction);
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