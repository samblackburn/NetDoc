﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NetDoc.Utils;

namespace NetDoc
{
    public class Call
    {
        private readonly MemberReference m_Operand;
        private IEnumerable<string> ReferencedDlls { get; }

        public Call(Instruction instruction, IEnumerable<string> referencedDlls)
        {
            ReferencedDlls = referencedDlls;
            m_Operand = (MemberReference) instruction.Operand;
            if (instruction.OpCode == OpCodes.Ldfld || instruction.OpCode == OpCodes.Stfld)
            {
                IsStatic = false;
            }
            else if (instruction.OpCode == OpCodes.Ldsfld || instruction.OpCode == OpCodes.Stsfld)
            {
                IsStatic = true;
            }
            else
            {
                IsStatic = !MethodReference!.HasThis;
            }
        }

        public bool IsStatic { get; }
        public string Method => IgnorePropertyPrefix(m_Operand.Name);
        public string ClassOrInstance
        {
            get
            {
                if (IsStatic) return GetTypeName(DeclaringType);
                return CallToFactory(DeclaringType);
            }
        }

        public TypeReference DeclaringType => m_Operand.DeclaringType;

        public string ContainingTypeName
        {
            get
            {
                var topLevelType = DeclaringType;
                while (topLevelType.DeclaringType != null) topLevelType = topLevelType.DeclaringType;
                return $"{topLevelType.Namespace}::{topLevelType.Name}";
            }
        }

        public string TypeWithGenerics => GetTypeName(DeclaringType);

        public string Invocation
        {
            get
            {
                try
                {
                    return BuildInvocation();
                }
                catch
                {
                    Console.Error.WriteLine($"Call being processed: {m_Operand}");
                    throw;
                }
            }
        }

        private string BuildInvocation()
        {
            if (FieldReference != null)
            {
                return AssignToRandomVariable(FieldReference.FieldType, $"{ClassOrInstance}.{FieldReference.Name}");
            }

            var parameters =
                string.Join(", ", Parameters(MethodReference!.Resolve()?.Parameters ?? MethodReference.Parameters));
            var indexerParameters = string.Join(", ",
                Parameters(MethodReference.Resolve()?.Parameters.SkipLast() ?? MethodReference.Parameters.SkipLast()));

            if (m_Operand.Name == ".ctor")
            {
                return AssignToRandomVariable(MethodReference.DeclaringType, $"new {TypeWithGenerics}({parameters})");
            }

            if (m_Operand.Name == "get_Item")
            {
                return AssignToRandomVariable(MethodReference.ReturnType, $"{ClassOrInstance}[{parameters}]");
            }

            if (m_Operand.Name == "set_Item")
            {
                return
                    $"{ClassOrInstance}[{indexerParameters}] = {CallToFactory(MethodReference.Parameters.Last().ParameterType)};";
            }

            if (m_Operand.Name.StartsWith("get_"))
            {
                return AssignToRandomVariable(MethodReference.ReturnType, $"{ClassOrInstance}.{Method}");
            }

            if (m_Operand.Name.StartsWith("set_"))
            {
                return $"{ClassOrInstance}.{Method} = {CallToFactory(MethodReference.Parameters.First().ParameterType)};";
            }

            if (MethodReference.ReturnType.FullName == "System.Void")
            {
                return $"{ClassOrInstance}.{m_Operand.Name}({parameters});";
            }

            return AssignToRandomVariable(MethodReference.ReturnType,
                $"{ClassOrInstance}.{m_Operand.Name}{GenericParams()}({parameters})");
        }

        private string GenericParams()
        {
            if (MethodReference is GenericInstanceMethod gim)
            {
                return "<" + String.Join(",", gim.GenericArguments.Select(x => GetTypeName(x))) + ">";
            }

            return "";
        }

        public override string ToString() => $"{TypeWithGenerics}{Invocation}";

        private MethodReference? MethodReference => m_Operand as MethodReference;
        private FieldReference? FieldReference => m_Operand as FieldReference;

        private string CallToFactory(TypeReference type) => $"Create<{GetTypeName(type)}>()";

        private IEnumerable<string> Parameters(IEnumerable<ParameterDefinition> parameterDefinitions) =>
            parameterDefinitions.Select(SyntaxForParameter);

        private string SyntaxForParameter(ParameterDefinition param)
        {
            if (param.IsOut)
            {
                return "out _";
            }
            if (param.ParameterType.IsByReference)
            {
                return $"ref new Ref<{GetTypeName(param.ParameterType)}>().Any";
            }
            return CallToFactory(param.ParameterType);
        }

        private string AssignToRandomVariable(TypeReference returnType, string expression) =>
            $"CheckReturnType<{GetTypeName(returnType)}>({expression});";

        private string IgnorePropertyPrefix(string name)
        {
            if (name.StartsWith("get_") || name.StartsWith("set_"))
            {
                return name.Substring(4);
            }

            return name;
        }

        private string GetTypeName(TypeReference type, GenericInstanceType? declaringType = null)
        {
            if (type is TypeDefinition def && !CanSeeFromAssertion(type) && CanSeeFromAssertion(def.BaseType))
            {
                return GetTypeName(def.BaseType);
            }

            declaringType ??= DeclaringType as GenericInstanceType;
            if (type.Name.StartsWith("!"))
            {
                var genericParamNumber = int.Parse(type.Name.TrimStart('!'));
                if (declaringType != null)
                {
                    type = declaringType.GenericArguments[genericParamNumber];
                    if (!CanSeeFromAssertion(type))
                    {
                        return "object";
                    }
                }
                else
                {
                    return "object";
                }
            }

            var className = type.Name.Split('`')[0];

            if (type is GenericParameter ofT && declaringType != null)
            {
                var declaringTypeGenericArgument = declaringType.GenericArguments[ofT.Position];
                if (declaringTypeGenericArgument != type)
                {
                    return GetTypeName(declaringTypeGenericArgument);
                }
                else
                {
                    // Avoid stack overflow
                    return "object";
                }
            }
            
            if (type is GenericParameter)
            {
                return "object";
            }

            var nameSpace = type.Namespace;

            if (type.DeclaringType != null)
            {
                nameSpace = GetTypeName(type.DeclaringType);
            }

            var generics = type is GenericInstanceType git
                ? $"<{String.Join(", ", git.GenericArguments.Select(x => GetTypeName(x)))}>"
                : "";
            if (!string.IsNullOrEmpty(nameSpace)) nameSpace += ".";
            var fullName = $"{nameSpace}{className}{generics}".TrimEnd('&');

            switch (fullName)
            {
                case "System.Void": return "void";
                case "System.String": return "string";
                case "System.Object": return "object";
                case "System.Boolean": return "bool";
                case "System.Int32": return "int";
                case "System.Int64": return "long";
                default: return fullName;
            }
        }

        /// <returns>
        /// True if the type is in the list of referenced dlls given to NetDoc
        /// True if the type is in the .NET Framework
        /// False if the type is in the referencing dll
        /// </returns>
        private bool CanSeeFromAssertion(TypeReference type)
        {
            var referencing = m_Operand.Module.Name.Replace(".dll", "");
            var candidate = type.Scope.Name.Replace(".dll", "");
            if (ReferencedDlls.Contains(candidate)) return true;
            if (candidate == referencing) return false;
            if (candidate == "mscorlib") return true;
            throw new NotImplementedException();
        }
    }
}