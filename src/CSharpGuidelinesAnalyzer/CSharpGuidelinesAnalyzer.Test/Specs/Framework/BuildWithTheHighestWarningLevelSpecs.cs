using CSharpGuidelinesAnalyzer.Framework;
using CSharpGuidelinesAnalyzer.Test.TestDataBuilders;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace CSharpGuidelinesAnalyzer.Test.Specs.Framework
{
    public class BuildWithTheHighestWarningLevelSpecs : CSharpGuidelinesAnalysisTestFixture
    {
        protected override string DiagnosticId => BuildWithTheHighestWarningLevelAnalyzer.DiagnosticId;

        [Fact]
        public void When_warning_level_is_set_to_four_it_must_be_skipped()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .CompileAtWarningLevel(4)
                .AllowingDiagnosticsOutsideSourceTree()
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source);
        }

        [Fact]
        public void When_warning_level_is_set_to_three_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .CompileAtWarningLevel(3)
                .AllowingDiagnosticsOutsideSourceTree()
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Build with warning level 4.");
        }

        [Fact]
        public void When_warning_level_is_set_to_two_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .CompileAtWarningLevel(2)
                .AllowingDiagnosticsOutsideSourceTree()
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Build with warning level 4.");
        }

        [Fact]
        public void When_warning_level_is_set_to_one_it_must_be_reported()
        {
            // Arrange
            ParsedSourceCode source = new ClassSourceCodeBuilder()
                .CompileAtWarningLevel(1)
                .AllowingDiagnosticsOutsideSourceTree()
                .Build();

            // Act and assert
            VerifyGuidelineDiagnostic(source,
                "Build with warning level 4.");
        }

        // Note: at warning level 0, analyzers do not even run. So a test for that is omitted here.

        protected override DiagnosticAnalyzer CreateAnalyzer()
        {
            return new BuildWithTheHighestWarningLevelAnalyzer();
        }
    }
}