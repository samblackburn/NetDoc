using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace NetDoc
{
    internal class SolutionModifier
    {
        private readonly IEnumerable<Rewriter> m_ReWriters;
        private readonly string m_Sln;

        public SolutionModifier(IEnumerable<Rewriter> reWriters, string sln)
        {
            m_ReWriters = reWriters;
            m_Sln = sln;
        }

        /// <summary>
        /// https://stackoverflow.com/questions/32176651/visit-and-modify-all-documents-in-a-solution-using-roslyn
        /// </summary>
        internal async Task ModifySolution()
        {
            var msWorkspace = MSBuildWorkspace.Create();
            var solution = await msWorkspace.OpenSolutionAsync(m_Sln);

            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);
                foreach (var documentId in project.DocumentIds)
                {
                    var document = project.GetDocument(documentId);

                    if (document.SourceCodeKind != SourceCodeKind.Regular)
                        continue;

                    var doc = document;
                    foreach (var reWriter in m_ReWriters)
                    {
                        doc = await reWriter.Rewrite(doc);
                    }

                    project = doc.Project;
                }

                solution = project.Solution;
            }

            msWorkspace.TryApplyChanges(solution);
        }
    }
}