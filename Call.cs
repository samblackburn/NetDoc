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
        public string ClassOrInstance
        {
            get
            {
                if (IsStatic) return GetTypeName(m_Operand.DeclaringType);
                return CallToFactory(m_Operand.DeclaringType);
            }
        }
        public bool IsStatic => !m_Operand.HasThis;
        public string TypeWithGenerics => GetTypeName(m_Operand.DeclaringType);

        public string Invocation {
            get
            {
                var parameters = string.Join(", ", Parameters(m_Operand.Parameters));
                var indexerParameters = string.Join(", ", Parameters(m_Operand.Parameters.SkipLast()));

                if (m_Operand.Name == ".ctor")
                {
                    return $"new {TypeWithGenerics}({parameters});";
                }

                if (m_Operand.Name == "get_Item")
                {
                    return AssignToRandomVariable(m_Operand,$"{ClassOrInstance}[{parameters}]");
                }

                if (m_Operand.Name == "set_Item")
                {
                    return $"{ClassOrInstance}[{indexerParameters}] = {CallToFactory(m_Operand.Parameters.Last().ParameterType)};";
                }

                if (m_Operand.Name.StartsWith("get_"))
                {
                    return AssignToRandomVariable(m_Operand, $"{ClassOrInstance}.{Method}");
                }

                if (m_Operand.Name.StartsWith("set_"))
                {
                    return $"{ClassOrInstance}.{Method} = {CallToFactory(m_Operand.Parameters.First().ParameterType)};";
                }

                if (m_Operand.ReturnType.FullName == "System.Void")
                {
                    return $"{ClassOrInstance}.{m_Operand.Name}({parameters});";
                }

                return AssignToRandomVariable(m_Operand, $"{ClassOrInstance}.{m_Operand.Name}({parameters})");
            }
        }

        public override string ToString() => $"{TypeWithGenerics}{Invocation}";

        private string CallToFactory(TypeReference type) => $"Create<{GetTypeName(type)}>()";

        private IEnumerable<string> Parameters(IEnumerable<ParameterDefinition> parameterDefinitions) =>
            parameterDefinitions.Select(param => CallToFactory(param.ParameterType));

        private string AssignToRandomVariable(MethodReference method, string expression) =>
            $"CheckReturnType<{GetTypeName(method.ReturnType)}>({expression});";

        private string IgnorePropertyPrefix(string name)
        {
            if (name.StartsWith("get_") || name.StartsWith("set_"))
            {
                return name.Substring(4);
            }

            return name;
        }

        private string GetTypeName(TypeReference type)
        {
            if (type.Name.StartsWith("!"))
            {
                var genericParamNumber = int.Parse(type.Name.TrimStart('!'));
                var declaringType = (GenericInstanceType)m_Operand.DeclaringType;
                type = declaringType.GenericArguments[genericParamNumber];
            }

            if (type is GenericParameter ofT)
            {
                return GetTypeName(ofT.Constraints.Select(c => c.ConstraintType).FirstOrDefault()) ?? "object";
            }

            var nameSpace = type.Namespace;

            if (type.DeclaringType != null)
            {
                nameSpace = GetTypeName(type.DeclaringType);
            }

            var className = type.Name.Split('`')[0];

            if (className.StartsWith("!!")) return className.Replace("!!", "T");
            var generics = type is GenericInstanceType git
                ? $"<{String.Join(", ", git.GenericArguments.Select(GetTypeName))}>"
                : "";
            if (!string.IsNullOrEmpty(nameSpace)) nameSpace += ".";
            var fullName = $"{nameSpace}{className}{generics}";

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
    }
}