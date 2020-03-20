using System;
using System.Collections.Generic;
using System.Linq;

namespace NetDoc
{
    internal class ContractClassWriter
    {
        public IEnumerable<string> ProcessCalls(string consumerName, IReadOnlyCollection<Call> calls)
        {
            yield return $"private void UsedBy{consumerName}(";
            var parameters = calls
                .Where(c => !c.IsStatic)
                .Where(c => !string.IsNullOrEmpty(c.Namespace))
                .Select(c => $"        {c.TypeWithGenerics} {c.VariableName}")
                .ToHashSet();
            Console.WriteLine(string.Join(",\r\n", parameters));
            Console.WriteLine("    )");
            Console.WriteLine("{");
            List<int>.Enumerator foo;
            foreach (var call in calls)
            {
                Console.WriteLine($"    {call.VariableName}{call.Invocation};");
            }

            Console.WriteLine("}");
        }
    }
}