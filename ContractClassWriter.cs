using System;
using System.Collections.Generic;

namespace NetDoc
{
    internal class ContractClassWriter
    {
        public void ProcessCalls(string consumerName, IEnumerable<Call> calls)
        {
            Console.WriteLine($"public void UsedBy{consumerName}");
            Console.WriteLine("{");

            foreach (var call in calls)
            {
                Console.WriteLine(call);
            }

            Console.WriteLine("}");
        }
    }
}