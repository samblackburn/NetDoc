using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace NetDoc
{
    internal class Call
    {
        private readonly MethodDefinition m_Consumer;
        private readonly MethodReference m_Operand;

        public Call(Instruction instruction, MethodDefinition consumer)
        {
            m_Consumer = consumer;
            m_Operand = (MethodReference)instruction.Operand;
        }

        public string Namespace => m_Operand.DeclaringType.Namespace;
        public string Type => m_Operand.DeclaringType.Name.Split('`')[0];
        public string Method => IgnorePropertyPrefix(m_Operand.Name);
        public string Consumer => $"{m_Consumer.DeclaringType.Name}.{m_Consumer.Name}";
        public string VariableName
        {
            get
            {
                if (IsStatic) return GetTypeName(m_Operand.DeclaringType);
                return MakeVariableName(m_Operand.DeclaringType);
            }
        }

        private static string MakeVariableName(TypeReference type)
        {
            var lowerCase = type.FullName.Substring(0, 1).ToLower() +
                            type.FullName.Substring(1);
            var noDots = lowerCase.Replace(".", "_").Replace("!", "_bang_");
            var noGenerics = noDots.Replace("<", "_lt_").Replace(">", "_gt_");
            var noSlashes = noGenerics.Replace(" / ", "_");
            var withoutIndexer = noSlashes.Replace("[", "_array").Replace(",", "_comma_").Replace("]", "");
            switch (withoutIndexer)
            {
                case "object":
                case "string":
                case "char":
                    return "@" + withoutIndexer;
                default:
                    return withoutIndexer.Replace("`", "");
            }
        }

        public string Invocation {
            get
            {
                var parameters = string.Join(", ", m_Operand.Parameters.Select(p => p.ParameterType).Select(MakeVariableName));
                var indexerParameters = string.Join(", ", m_Operand.Parameters.Skip(1).Select(p => p.ParameterType).Select(MakeVariableName));

                if (m_Operand.Name == ".ctor")
                {
                    return $"            new {TypeWithGenerics}({parameters});";
                }

                if (m_Operand.Name == "get_Item")
                {
                    return AssignToRandomVariable(m_Operand.ReturnType, $"{VariableName}[{parameters}];");
                }

                if (m_Operand.Name == "set_Item")
                {
                    return $"            {VariableName}[{indexerParameters}] = {MakeVariableName(m_Operand.Parameters.First().ParameterType)};";
                }

                if (m_Operand.Name.StartsWith("get_"))
                {
                    return AssignToRandomVariable(m_Operand.ReturnType, $"{VariableName}.{Method};");
                }

                if (m_Operand.Name.StartsWith("set_"))
                {
                    return $"            {VariableName}.{Method} = {MakeVariableName(m_Operand.Parameters.First().ParameterType)};";
                }

                return $"            {VariableName}.{m_Operand.Name}({parameters});";
            }
        }

        private string AssignToRandomVariable(TypeReference returnType, string expression)
        {
            return $"            {GetTypeNameOrVar(returnType)} v{Math.Abs(expression.GetHashCode() % 10000):D4} = {expression}";
        }

        private static string GetTypeNameOrVar(TypeReference returnType)
        {
            var typeName = GetTypeName(returnType);
            return typeName.StartsWith("!") ? "var" : typeName;
        }

        public bool IsStatic => !m_Operand.HasThis;
        public string TypeWithGenerics => GetTypeName(m_Operand.DeclaringType);

        public IEnumerable<string> ParameterTypes
        {
            get
            {
                var result = m_Operand.Parameters.Select(p =>
                    $"            {GetTypeName(p.ParameterType)} {MakeVariableName(p.ParameterType)}");
                if (result.Any(x => x.Contains("&")))
                {

                }
                return result;
            }
        }

        public string TypeAsParameter => $"            {TypeWithGenerics} {MakeVariableName(m_Operand.DeclaringType)}";

        private string IgnorePropertyPrefix(string name)
        {
            if (name.StartsWith("get_") || name.StartsWith("set_"))
            {
                return name.Substring(4);
            }

            return name;
        }

        public override string ToString() => $"{TypeWithGenerics}{Invocation}";

        private static string GetTypeName(TypeReference type)
        {
            var nameSpace = type.Namespace;
            var className = type.Name.Split('`')[0];
            if (className.StartsWith("!!")) return className.Replace("!!", "T");
            var generics = type is GenericInstanceType git
                ? $"<{String.Join(", ", git.GenericArguments.Select(x => GetTypeName(x)))}>"
                : "";
            if (!string.IsNullOrEmpty(nameSpace)) nameSpace += ".";
            return $"{nameSpace}{className}{generics}";
        }
    }
}