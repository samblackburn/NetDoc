﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace NetDoc
{
    internal class ContractClassWriter
    {
        public IEnumerable<string> ProcessCalls(string consumerName, IEnumerable<Call> calls)
        {
            var relevantCalls = calls
                .Where(c => c.Method != "op_Equality")
                .Where(c => c.Method != "op_Inequality")
                .ToList();

            yield return $"private void UsedBy{consumerName}<T0, T1>(";
            var parameters = relevantCalls
                .Where(c => !c.IsStatic)
                .Where(c => !string.IsNullOrEmpty(c.Namespace))
                .Select(c => c.TypeAsParameter)
                .Concat(relevantCalls.SelectMany(c => c.ParameterTypes))
                .Select(string.Intern)
                .ToHashSet();
            Console.WriteLine(string.Join(",\r\n", parameters));
            Console.WriteLine("    )");
            Console.WriteLine("{");

            foreach (var invocation in relevantCalls.Select(c => c.Invocation).ToHashSet())
            {
                Console.WriteLine(invocation);
            }

            Console.WriteLine("}");
        }
    }
}