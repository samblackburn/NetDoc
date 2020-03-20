using System;
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
            var lowerCase = type.Name.Substring(0, 1).ToLower() +
                            type.Name.Substring(1);
            switch (lowerCase)
            {
                case "object":
                case "string":
                    return "@" + lowerCase;
                default:
                    return lowerCase.Replace("`", "_");
            }
        }

        public string Invocation {
            get
            {
                var parameters = string.Join(", ", m_Operand.Parameters.Select(p => p.ParameterType).Select(MakeVariableName));
                return $".{m_Operand.Name}({parameters})";
            }
        }

        public bool IsStatic => !m_Operand.HasThis;
        public string TypeWithGenerics => GetTypeName(m_Operand.DeclaringType);


        private string IgnorePropertyPrefix(string name)
        {
            if (name.StartsWith("get_") || name.StartsWith("set_"))
            {
                return name.Substring(4);
            }

            return name;
        }

        public override string ToString() => $"{TypeWithGenerics}{Invocation}";

        private  string GetTypeName(TypeReference type)
        {
            var nameSpace = type.Namespace;
            var className = type.Name.Split('`')[0];
            var generics = type is GenericInstanceType git
                ? $"<{String.Join(", ", git.GenericArguments.Select(x => x.FullName.Replace("!!", "T")))}>"
                : "";
            return $"{nameSpace}.{className}{generics}";
        }
    }
}