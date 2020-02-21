using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace NetDoc
{
    internal class Rewriter
    {
        public Rewriter(IEnumerable<Call> calls)
        {
            
        }

        internal async Task<Document> Rewrite(Document doc)
        {
            var tree = await doc.GetSyntaxRootAsync();

            // For each method or property


            return doc;
        }
    }
}