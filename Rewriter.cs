using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace NetDoc
{
    internal class Rewriter
    {
        private readonly IEnumerable<Call> m_Calls;

        public Rewriter(IEnumerable<Call> calls)
        {
            m_Calls = calls;
        }

        internal async Task<Document> Rewrite(Document doc)
        {
            var model = await doc.GetSyntaxRootAsync();
            var toReplace = new Dictionary<SyntaxNode, SyntaxNode>();

            //foreach (var method in model.DescendantNodes())
            //{
            //    Debug(method);
            //}

            foreach (var method in model.DescendantNodes().Where(CanBeCallTarget))
            {
                var containingClass = method.Ancestors().FirstOrDefault(IsType); // Can be null as delegates can live outside a class
                var namespaceParts = method.Ancestors().SingleOrDefault(IsNamespace)?.ChildNodes().First().DescendantNodes()
                    .Where(t => t.Kind() == SyntaxKind.IdentifierName).Select(Name);
                var containingNamespace = namespaceParts != null ? string.Join(".", namespaceParts) : "global::";

                var isPublic = (IsPublic(containingClass) != false) && (IsPublic(method) != false);
                if (!isPublic)
                {
                    continue;
                }

                var name = Name(method);
                if (method.Kind() == SyntaxKind.FieldDeclaration)
                {
                    name = method
                        .ChildNodes().Single(n => n.Kind() == SyntaxKind.VariableDeclaration)
                        .ChildNodes().Single(n => n.Kind() == SyntaxKind.VariableDeclarator)
                        .ChildTokens().Single(n => n.Kind() == SyntaxKind.IdentifierToken).Text;
                }

                var containingType = Name(containingClass);
                //Console.WriteLine($"{method.Kind()} {containingNamespace}.{containingType}.{name}()");

                var matchingCalls = m_Calls
                    .Where(c => c.Namespace == containingNamespace)
                    .Where(c => c.Type == containingType)
                    .Where(c => c.Method == name)
                    .ToList();

                var existingComment = method.GetLeadingTrivia();
                var newComment = DocumentUsages(existingComment, matchingCalls);
                if (!existingComment.Equals(newComment))
                {
                    toReplace[method] = method.WithLeadingTrivia(newComment);
                }
            }

            return doc.WithSyntaxRoot(model.ReplaceNodes(toReplace.Keys, (key, _) => toReplace[key]));
        }

        private SyntaxTriviaList DocumentUsages(SyntaxTriviaList existingComment, IEnumerable<Call> matchingCalls)
        {
            var probablyBlankLine = existingComment.FirstOrDefault(t => t.Kind() == SyntaxKind.WhitespaceTrivia).ToFullString();
            var consumers = matchingCalls.Select(c => c.Consumer);
            var newComment = CommentUpdater.UpdateXmlComment(consumers, existingComment.ToFullString(), probablyBlankLine);
            var newTrivia = SyntaxFactory.ParseLeadingTrivia(newComment);
            if (newTrivia.ToFullString() != newComment)
            {
                if (newComment == existingComment.ToFullString())
                {
                    return existingComment;
                }

                throw new Exception("Failed to add XML doc");
            }

            return newTrivia;
        }

        private static void CreateContractClass(HashSet<string> typesUsed, List<Call> listedCalls)
        {
            Console.WriteLine("public class ZzzzzContractAssertions");
            Console.WriteLine("{");
            Console.Write("    public void FooUsesTheseApiPoints(");
            Console.Write(String.Join(", ", typesUsed.Select(x => $"{x} {x}")));
            Console.WriteLine(")");
            Console.WriteLine("    {");
            foreach (var call in listedCalls)
            {
                Console.WriteLine($"        {call.Type}.{call.Method}(); // Used by {call.Consumer}");
            }

            Console.WriteLine("    }");
            Console.WriteLine("}");
        }

        private static void Debug(SyntaxNode method)
        {
            var spaces = new string(' ', method.Ancestors().Count());
            var tokens = method.ChildTokens().Select(t => t.Kind() + ":" + t.Text);
            Console.WriteLine($"{spaces}{method.Kind()} => {string.Join(", ", tokens)}");
        }

        private static bool? IsPublic(SyntaxNode containingClass)
        {
            return containingClass?.ChildTokens().Any(t => t.Kind() == SyntaxKind.PublicKeyword);
        }

        private static string Name(SyntaxNode node)
        {
            return node?.ChildTokens().SingleOrDefault(t => t.Kind() == SyntaxKind.IdentifierToken).Text;
        }

        private static bool IsNamespace(SyntaxNode n)
        {
            return n.Kind() == SyntaxKind.NamespaceDeclaration;
        }

        private static bool IsType(SyntaxNode n)
        {
            switch (n.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.EnumDeclaration:
                    return true;
                default:
                    return false;
            }
        }

        private static bool CanBeCallTarget(SyntaxNode n)
        {
            switch (n.Kind())
            {
                case SyntaxKind.DelegateDeclaration:
                case SyntaxKind.EnumMemberDeclaration:
                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.EventFieldDeclaration:
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.EventDeclaration:
                case SyntaxKind.IndexerDeclaration:
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                case SyntaxKind.UnknownAccessorDeclaration:
                    return true;
                default: return false;
            }
        }
    }
}