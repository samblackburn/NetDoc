using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace NetDoc
{
    internal class TypeNameResolver
    {
        private readonly string m_ReferencingModuleName;
        private readonly IEnumerable<string> m_ReferencedDlls;

        public TypeNameResolver(string referencingModuleName, IEnumerable<string> referencedDlls)
        {
            m_ReferencingModuleName = referencingModuleName.Replace(".dll", "");
            m_ReferencedDlls = referencedDlls;
        }

        public string GetTypeName(TypeReference type, GenericInstanceType? declaringType = null, GenericInstanceMethod? methodContext = null)
        {
            if (type is TypeDefinition def && !CanSeeFromAssertion(type))
            {
                return StandIn(def, declaringType, methodContext);
            }
            else if (type is not TypeDefinition && type is not GenericParameter && !type.Name.StartsWith("!") && !CanSeeFromAssertion(type))
            {
                // Defined in a dll the assertion doesn't reference, e.g. another dll in the consuming product
                return "object";
            }

            if (type.Name.StartsWith("!"))
            {
                var isMethodParameter = type.Name.StartsWith("!!");
                var genericParamNumber = int.Parse(type.Name.TrimStart('!'));
                if (isMethodParameter && methodContext != null)
                {
                    type = methodContext.GenericArguments[genericParamNumber];
                }
                else if (!isMethodParameter && declaringType != null)
                {
                    type = declaringType.GenericArguments[genericParamNumber];
                }
                else
                {
                    return "object";
                }

                if (!CanSeeFromAssertion(type))
                {
                    // Fall back the same way as the declaring type does, so the two agree
                    return type is TypeDefinition invisible ? StandIn(invisible, declaringType, methodContext) : "object";
                }
            }

            var className = type.Name.Split('`')[0];

            if (type is GenericParameter ofT)
            {
                if (ofT.Type == GenericParameterType.Method && methodContext != null)
                {
                    var methodGenericArgument = methodContext.GenericArguments[ofT.Position];
                    if (methodGenericArgument != type && CanSeeFromAssertion(methodGenericArgument))
                    {
                        return GetTypeName(methodGenericArgument, declaringType, methodContext);
                    }
                    if (methodGenericArgument is TypeDefinition invisible)
                    {
                        return StandIn(invisible, declaringType, methodContext);
                    }
                }
                else if (declaringType != null)
                {
                    var declaringTypeGenericArgument = declaringType.GenericArguments[ofT.Position];
                    if (declaringTypeGenericArgument != type)
                    {
                        return GetTypeName(declaringTypeGenericArgument, declaringType, methodContext);
                    }
                    else if ((declaringTypeGenericArgument as GenericParameter)?.Constraints.FirstOrDefault() is {} constraint)
                    {
                        return GetTypeName(constraint.ConstraintType, declaringType, methodContext);
                    }
                }

                // Avoid stack overflow
                return "object";
            }

            var nameSpace = type.Namespace;

            if (type.DeclaringType != null)
            {
                nameSpace = GetTypeName(type.DeclaringType, declaringType, methodContext);
            }

            var generics = type is GenericInstanceType git
                ? $"<{String.Join(", ", git.GenericArguments.Select(x => GetTypeName(x, declaringType, methodContext)))}>"
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
        /// The name of a type the assertion can see that can stand in for one it can't
        /// </returns>
        private string StandIn(TypeDefinition invisible, GenericInstanceType? declaringType, GenericInstanceMethod? methodContext)
        {
            // Any enum satisfies the same generic constraints (struct, System.Enum) as the consumer's enum
            if (invisible.IsEnum) return "System.DayOfWeek";
            // Interfaces have no base type
            if (invisible.BaseType == null) return "object";
            if (CanSeeFromAssertion(invisible.BaseType)) return GetTypeName(invisible.BaseType, declaringType, methodContext);

            // The base type is also invisible, e.g. another of the consumer's own types, so keep looking up the hierarchy
            TypeDefinition? baseDefinition;
            try
            {
                baseDefinition = invisible.BaseType.Resolve();
            }
            catch (AssemblyResolutionException)
            {
                baseDefinition = null;
            }
            return baseDefinition == null ? "object" : StandIn(baseDefinition, declaringType, methodContext);
        }

        /// <returns>
        /// True if the type is in the list of referenced dlls given to NetDoc, or one of their dependencies
        /// True if the type is in the .NET Framework
        /// False if the type is in the referencing dll, or any other dll the assertion doesn't reference
        /// </returns>
        private bool CanSeeFromAssertion(TypeReference type)
        {
            var candidate = type.Scope.Name.Replace(".dll", "");
            if (m_ReferencedDlls.Contains(candidate)) return true;
            if (candidate == m_ReferencingModuleName) return false;
            if (candidate is "mscorlib" or "netstandard" or "System" || candidate.StartsWith("System.")) return true;
            return false;
        }
    }
}
