using CSharpGuidelinesAnalyzer.Naming;
using CSharpGuidelinesAnalyzer.Test.TestDataBuilders;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace CSharpGuidelinesAnalyzer.Test.Specs.Naming
{
    public class NamePropertiesWithAnAffirmativePhraseSpecs : CSharpGuidelinesAnalysisTestFixture
    {
        protected override string DiagnosticId => NamePropertiesWithAnAffirmativePhraseAnalyzer.DiagnosticId;

        protected override DiagnosticAnalyzer CreateAnalyzer()
        {
            return new NamePropertiesWithAnAffirmativePhraseAnalyzer();
        }
    }
}