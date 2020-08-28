using System.Collections.Generic;
using System.Linq;

namespace NetDoc
{
    internal class ContractClassWriter
    {
        public IEnumerable<string> ProcessCalls(string consumerName, IEnumerable<Call> calls)
        {
            var relevantCalls = calls
                .Where(c => !Exclusions.Contains(c.Method))
                .ToList();
            if (!relevantCalls.Any()) yield break;

            yield return $"private void UsedBy{consumerName}()";
            yield return @"{";

            foreach (var invocation in relevantCalls.Select(c => c.Invocation).ToHashSet())
            {
                yield return $"    {invocation}";
            }

            yield return @"}";
        }

        private static readonly HashSet<string> Exclusions = new HashSet<string>
        {
            "op_Equality",
            "op_Inequality",
            "ComputeStringHash",
            "Binding",
            "Command",
        };

        public string UtilsSource => @"// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
// ReSharper disable once UnusedType.Global

internal static class ContractAssertionUtils
{
    internal static T Create<T>() => default;
    internal static void CheckReturnType<T>(T param) {}
}

internal class Ref<T> { public T Any = default; }
";

        public string Header(string referencingClassName) => $@"using static ContractAssertionUtils;
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable once CheckNamespace
internal abstract class {referencingClassName}ContractAssertions
{{
";

        public string Footer => @"}";

    }
}