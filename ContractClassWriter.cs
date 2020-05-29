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

            yield return $"        private void UsedBy{consumerName}()";
            yield return @"        {";

            foreach (var invocation in relevantCalls.Select(c => c.Invocation).ToHashSet())
            {
                yield return invocation;
            }

            yield return @"        }";
        }

        private static readonly HashSet<string> Exclusions = new HashSet<string>
        {
            "op_Equality",
            "op_Inequality",
            "ComputeStringHash",
            "Binding",
            "Command",
        };
    }
}