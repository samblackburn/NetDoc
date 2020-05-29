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

        private static string CallToFactory(TypeReference type) => $"A<{GetTypeName(type)}>()";

        public string Invocation {
            get
            {
                var parameters = string.Join(", ", m_Operand.Parameters.Select(p => p.ParameterType).Select(CallToFactory));
                var indexerParameters = string.Join(", ", m_Operand.Parameters.Skip(1).Select(p => p.ParameterType).Select(CallToFactory));

                if (m_Operand.Name == ".ctor")
                {
                    return $"            new {TypeWithGenerics}({parameters});";
                }

                if (m_Operand.Name == "get_Item")
                {
                    return AssignToRandomVariable(m_Operand.ReturnType, $"{ClassOrInstance}[{parameters}];");
                }

                if (m_Operand.Name == "set_Item")
                {
                    return $"            {ClassOrInstance}[{indexerParameters}] = {CallToFactory(m_Operand.Parameters.First().ParameterType)};";
                }

                if (m_Operand.Name.StartsWith("get_"))
                {
                    return AssignToRandomVariable(m_Operand.ReturnType, $"{ClassOrInstance}.{Method};");
                }

                if (m_Operand.Name.StartsWith("set_"))
                {
                    return $"            {ClassOrInstance}.{Method} = {CallToFactory(m_Operand.Parameters.First().ParameterType)};";
                }

                return $"            {ClassOrInstance}.{m_Operand.Name}({parameters});";
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
                    $"            {GetTypeName(p.ParameterType)} {CallToFactory(p.ParameterType)}");
                if (result.Any(x => x.Contains("&")))
                {

                }
                return result;
            }
        }

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
            if (className.StartsWith("!")) return "object";
            var generics = type is GenericInstanceType git
                ? $"<{String.Join(", ", git.GenericArguments.Select(x => GetTypeName(x)))}>"
                : "";
            if (!string.IsNullOrEmpty(nameSpace)) nameSpace += ".";
            return $"{nameSpace}{className}{generics}";
        }
    }
}