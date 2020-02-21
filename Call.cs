using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace NetDoc
{
    internal class Call
    {
        private readonly MethodReference m_Operand;

        public Call(Instruction instruction)
        {
            m_Operand = (MethodReference)instruction.Operand;
        }

        public string Namespace => m_Operand.DeclaringType.Namespace;
        public string Type => m_Operand.DeclaringType.Name.Split('`')[0];
        public string Method => m_Operand.Name;

        public override string ToString()
        {
            var parameters = string.Join(", ", m_Operand.Parameters.Select(p => p.ParameterType).Select(GetTypeName));
            return $"{GetTypeName(m_Operand.DeclaringType)}.{m_Operand.Name}({parameters})";
        }

        private  string GetTypeName(TypeReference type)
        {
            var nameSpace = type.Namespace;
            var className = type.Name.Split('`')[0];
            var generics = type is GenericInstanceType git
                ? $"<{String.Join(", ", git.GenericArguments.Select(x => x.Name.Replace("!!", "T")))}>"
                : "";
            return $"{nameSpace}.{className}{generics}";
        }
    }
}