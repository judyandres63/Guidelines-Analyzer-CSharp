using System.Collections.Immutable;
using CSharpGuidelinesAnalyzer.Extensions;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpGuidelinesAnalyzer.Rules.ClassDesign
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MembersShouldDoASingleThingAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AV1115";

        private const string Title = "Member contains the word 'and'";
        private const string MessageFormat = "{0} '{1}' contains the word 'and'.";
        private const string Description = "A method or property should do only one thing.";
        private const string Category = "Class Design";

        [NotNull]
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat,
            Category, DiagnosticSeverity.Warning, true, Description, HelpLinkUris.GetForCategory(Category, DiagnosticId));

        [ItemNotNull]
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        private static readonly ImmutableArray<SymbolKind> MemberSymbolKinds =
            new[] { SymbolKind.Property, SymbolKind.Method, SymbolKind.Field, SymbolKind.Event }.ToImmutableArray();

        [ItemNotNull]
        private static readonly ImmutableArray<string> WordsBlacklist = new[] { "And" }.ToImmutableArray();

        public override void Initialize([NotNull] AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSymbolAction(AnalyzeMember, MemberSymbolKinds);
        }

        private void AnalyzeMember(SymbolAnalysisContext context)
        {
            if (context.Symbol.IsPropertyOrEventAccessor())
            {
                return;
            }

            if (context.Symbol.IsUnitTestMethod())
            {
                return;
            }

            if (
                context.Symbol.Name.GetFirstWordInSetFromIdentifier(WordsBlacklist, TextMatchMode.AllowLowerCaseMatch) !=
                null)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, context.Symbol.Locations[0], context.Symbol.Kind,
                    context.Symbol.Name));
            }
        }
    }
}