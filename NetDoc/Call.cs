using System;
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
        private readonly MethodReference? m_Delegate;
        private readonly TypeNameResolver m_TypeNames;

        public Call(Instruction instruction, IEnumerable<string> referencedDlls)
        {
            m_Operand = (MemberReference) instruction.Operand;
            m_TypeNames = new TypeNameResolver(m_Operand.Module.Name, referencedDlls);
            if (instruction.OpCode == OpCodes.Newobj &&
                MethodReference?.Parameters.Select(x => x.ParameterType.Name).SequenceEqual([nameof(Object), nameof(IntPtr)]) == true &&
                instruction.Previous.OpCode == OpCodes.Ldftn)
            {
                m_Delegate = (MethodReference)instruction.Previous.Operand;
            }
            else if (instruction.OpCode == OpCodes.Ldfld || instruction.OpCode == OpCodes.Stfld)
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
                return BuildFieldAccess();
            }

            var parameterDefs = MethodReference!.Resolve()?.Parameters ?? MethodReference.Parameters;
            var parameters = string.Join(", ", Parameters(parameterDefs));
            var indexerParameters = string.Join(", ",
                Parameters(MethodReference.Resolve()?.Parameters.SkipLast() ?? MethodReference.Parameters.SkipLast()));

            if (m_Operand.Name == ".ctor")
            {
                return BuildConstructorCall(parameters);
            }

            if (m_Operand.Name == "get_Item")
            {
                return BuildIndexerGet(parameters);
            }

            if (m_Operand.Name == "set_Item")
            {
                return BuildIndexerSet(indexerParameters);
            }

            if (m_Operand.Name.StartsWith("get_"))
            {
                return BuildPropertyGet();
            }

            if (m_Operand.Name.StartsWith("set_"))
            {
                return BuildPropertySet();
            }

            if (MethodReference.ReturnType.FullName == "System.Void")
            {
                return BuildVoidMethodCall(parameters);
            }

            return BuildValueMethodCall(parameters);
        }

        private string BuildFieldAccess() =>
            AssignToRandomVariable(FieldReference!.FieldType, $"{ClassOrInstance}.{FieldReference.Name}");

        private string BuildConstructorCall(string parameters)
        {
            if (m_Delegate != null)
            {
                return BuildDelegateConstructor();
            }

            return AssignToRandomVariable(MethodReference!.DeclaringType, $"new {TypeWithGenerics}({parameters})");
        }

        private string BuildDelegateConstructor()
        {
            var args = string.Join(", ", m_Delegate!.Parameters.Select((p, i) => $"{GetTypeName(p.ParameterType)} arg{i}"));
            var expression = m_Delegate.ReturnType.FullName == "System.Void"
                ? "{}"
                : CallToFactory(m_Delegate.ReturnType);
            return AssignToRandomVariable(MethodReference!.DeclaringType, $"({args}) => {expression}");
        }

        private string BuildIndexerGet(string parameters) =>
            AssignToRandomVariable(MethodReference!.ReturnType, $"{ClassOrInstance}[{parameters}]");

        private string BuildIndexerSet(string indexerParameters) =>
            $"{ClassOrInstance}[{indexerParameters}] = {CallToFactory(MethodReference!.Parameters.Last().ParameterType)};";

        private string BuildPropertyGet() =>
            AssignToRandomVariable(MethodReference!.ReturnType, $"{ClassOrInstance}.{Method}");

        private string BuildPropertySet()
        {
            var valueType = MethodReference!.Parameters.First().ParameterType;
            if (!IsInitOnly)
            {
                return $"{ClassOrInstance}.{Method} = {CallToFactory(valueType)};";
            }

            var ctorParams = DeclaringType.Resolve()?.Methods
                .Where(m => m.IsConstructor && !m.IsStatic && m.IsPublic)
                .OrderBy(m => m.Parameters.Count)
                .FirstOrDefault()?.Parameters ?? Enumerable.Empty<ParameterDefinition>();
            var ctorArgs = string.Join(", ", Parameters(ctorParams));
            return AssignToRandomVariable(DeclaringType, $"new {TypeWithGenerics}({ctorArgs}) {{ {Method} = {CallToFactory(valueType)} }}");
        }

        private bool IsInitOnly =>
            MethodReference!.ReturnType is RequiredModifierType required &&
            required.ModifierType.FullName == "System.Runtime.CompilerServices.IsExternalInit";

        private string BuildVoidMethodCall(string parameters) =>
            $"{ClassOrInstance}.{m_Operand.Name}({parameters});";

        private string BuildValueMethodCall(string parameters) =>
            AssignToRandomVariable(MethodReference!.ReturnType,
                $"{ClassOrInstance}.{m_Operand.Name}{GenericParams()}({parameters})");

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

        private string GetTypeName(TypeReference type) =>
            m_TypeNames.GetTypeName(type, DeclaringType as GenericInstanceType, MethodReference as GenericInstanceMethod);
    }
}
