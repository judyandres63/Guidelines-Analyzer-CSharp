using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using CSharpGuidelinesAnalyzer.Extensions;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace CSharpGuidelinesAnalyzer.Rules.Maintainability
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DoNotAssignToParameterAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AV1568";

        private const string Title = "Parameter value should not be overwritten in method body";
        private const string MessageFormat = "The value of parameter '{0}' is overwritten in its method body.";
        private const string Description = "Don't use parameters as temporary variables.";

        [NotNull]
        private static readonly AnalyzerCategory Category = AnalyzerCategory.Maintainability;

        [NotNull]
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat,
            Category.DisplayName, DiagnosticSeverity.Info, true, Description, Category.GetHelpLinkUri(DiagnosticId));

        [ItemNotNull]
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        private static readonly ImmutableArray<SpecialType> SimpleTypes = ImmutableArray.Create(SpecialType.System_Boolean,
            SpecialType.System_Char, SpecialType.System_SByte, SpecialType.System_Byte, SpecialType.System_Int16,
            SpecialType.System_UInt16, SpecialType.System_Int32, SpecialType.System_UInt32, SpecialType.System_Int64,
            SpecialType.System_UInt64, SpecialType.System_Decimal, SpecialType.System_Single, SpecialType.System_Double,
            SpecialType.System_IntPtr, SpecialType.System_UIntPtr, SpecialType.System_DateTime);

        public override void Initialize([NotNull] AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSymbolAction(c => c.SkipEmptyName(AnalyzeMethod), SymbolKind.Method);
            context.RegisterSymbolAction(c => c.SkipEmptyName(AnalyzeProperty), SymbolKind.Property);
            context.RegisterSymbolAction(c => c.SkipEmptyName(AnalyzeEvent), SymbolKind.Event);

            context.RegisterOperationAction(c => c.SkipInvalid(AnalyzeLocalFunction), OperationKind.LocalFunction);
            context.RegisterOperationAction(c => c.SkipInvalid(AnalyzeAnonymousFunction), OperationKind.AnonymousFunction);
        }

        private void AnalyzeMethod(SymbolAnalysisContext context)
        {
            var method = (IMethodSymbol)context.Symbol;

            if (ShouldSkip(method) || method.IsPropertyOrEventAccessor())
            {
                return;
            }

            using (var collector = new DiagnosticCollector(context.ReportDiagnostic))
            {
                InnerAnalyzeMethod(method, collector, context.Compilation, context.CancellationToken);
            }
        }

        private void AnalyzeProperty(SymbolAnalysisContext context)
        {
            var property = (IPropertySymbol)context.Symbol;

            using (var collector = new DiagnosticCollector(context.ReportDiagnostic))
            {
                AnalyzeAccessorMethod(property.GetMethod, collector, context);
                AnalyzeAccessorMethod(property.SetMethod, collector, context);

                FilterDuplicateLocations(collector.Diagnostics);
            }
        }

        private void AnalyzeEvent(SymbolAnalysisContext context)
        {
            var evnt = (IEventSymbol)context.Symbol;

            using (var collector = new DiagnosticCollector(context.ReportDiagnostic))
            {
                AnalyzeAccessorMethod(evnt.AddMethod, collector, context);
                AnalyzeAccessorMethod(evnt.RemoveMethod, collector, context);

                FilterDuplicateLocations(collector.Diagnostics);
            }
        }

        private void AnalyzeAccessorMethod([CanBeNull] IMethodSymbol accessorMethod, [NotNull] DiagnosticCollector collector,
            SymbolAnalysisContext context)
        {
            if (accessorMethod == null || ShouldSkip(accessorMethod))
            {
                return;
            }

            InnerAnalyzeMethod(accessorMethod, collector, context.Compilation, context.CancellationToken);
        }

        private void FilterDuplicateLocations([NotNull] [ItemNotNull] IList<Diagnostic> diagnostics)
        {
            for (int index = 0; index < diagnostics.Count; index++)
            {
                Diagnostic diagnostic = diagnostics[index];

                Diagnostic[] duplicates = diagnostics
                    .Where(d => !ReferenceEquals(d, diagnostic) && d.Location == diagnostic.Location).ToArray();
                if (duplicates.Any())
                {
                    foreach (Diagnostic duplicate in duplicates)
                    {
                        diagnostics.Remove(duplicate);
                    }

                    index = 0;
                }
            }
        }

        private void AnalyzeLocalFunction(OperationAnalysisContext context)
        {
            var localFunction = (ILocalFunctionOperation)context.Operation;

            if (ShouldSkip(localFunction.Symbol))
            {
                return;
            }

            using (var collector = new DiagnosticCollector(context.ReportDiagnostic))
            {
                InnerAnalyzeMethod(localFunction.Symbol, collector, context.Compilation, context.CancellationToken);
            }
        }

        private void AnalyzeAnonymousFunction(OperationAnalysisContext context)
        {
            var anonymousFunction = (IAnonymousFunctionOperation)context.Operation;

            if (ShouldSkip(anonymousFunction.Symbol))
            {
                return;
            }

            using (var collector = new DiagnosticCollector(context.ReportDiagnostic))
            {
                InnerAnalyzeMethod(anonymousFunction.Symbol, collector, context.Compilation, context.CancellationToken);
            }
        }

        private static bool ShouldSkip([NotNull] IMethodSymbol method)
        {
            return method.IsAbstract || method.IsSynthesized() || !method.Parameters.Any();
        }

        private void InnerAnalyzeMethod([NotNull] IMethodSymbol method, [NotNull] DiagnosticCollector collector,
            [NotNull] Compilation compilation, CancellationToken cancellationToken)
        {
            SyntaxNode bodySyntax = method.TryGetBodySyntaxForMethod(cancellationToken);
            if (bodySyntax == null)
            {
                return;
            }

            IGrouping<bool, IParameterSymbol>[] parameterGrouping = method.Parameters
                .Where(p => p.RefKind == RefKind.None && !p.IsSynthesized()).GroupBy(IsUserDefinedStruct).ToArray();

            IParameterSymbol[] ordinaryParameters = parameterGrouping.Where(x => !x.Key).SelectMany(x => x).ToArray();
            if (ordinaryParameters.Any())
            {
                AnalyzeOrdinaryParameters(ordinaryParameters, bodySyntax, collector, compilation);
            }

            IParameterSymbol[] structParameters = parameterGrouping.Where(x => x.Key).SelectMany(x => x).ToArray();
            if (structParameters.Any())
            {
                AnalyzeStructParameters(structParameters, bodySyntax, collector, compilation, cancellationToken);
            }
        }

        private bool IsUserDefinedStruct([NotNull] IParameterSymbol parameter)
        {
            return parameter.Type.TypeKind == TypeKind.Struct && !IsSimpleType(parameter.Type);
        }

        private bool IsSimpleType([NotNull] ITypeSymbol type)
        {
            return type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T || SimpleTypes.Contains(type.SpecialType);
        }

        private void AnalyzeOrdinaryParameters([NotNull] [ItemNotNull] ICollection<IParameterSymbol> parameters,
            [NotNull] SyntaxNode bodySyntax, [NotNull] DiagnosticCollector collector, [NotNull] Compilation compilation)
        {
            DataFlowAnalysis dataFlowAnalysis = TryAnalyzeDataFlow(bodySyntax, compilation);
            if (dataFlowAnalysis == null)
            {
                return;
            }

            foreach (IParameterSymbol parameter in parameters)
            {
                if (dataFlowAnalysis.WrittenInside.Contains(parameter))
                {
                    collector.Add(Diagnostic.Create(Rule, parameter.Locations[0], parameter.Name));
                }
            }
        }

        [CanBeNull]
        private DataFlowAnalysis TryAnalyzeDataFlow([NotNull] SyntaxNode bodySyntax, [NotNull] Compilation compilation)
        {
            SemanticModel model = compilation.GetSemanticModel(bodySyntax.SyntaxTree);
            DataFlowAnalysis dataFlowAnalysis = model.AnalyzeDataFlow(bodySyntax);

            return dataFlowAnalysis.Succeeded ? dataFlowAnalysis : null;
        }

        private void AnalyzeStructParameters([NotNull] [ItemNotNull] ICollection<IParameterSymbol> parameters,
            [NotNull] SyntaxNode bodySyntax, [NotNull] DiagnosticCollector collector, [NotNull] Compilation compilation,
            CancellationToken cancellationToken)
        {
            // A user-defined struct can reassign its 'this' parameter on invocation. That's why the compiler dataflow
            // analysis reports all access as writes. Because that's not very practical, we run our own assignment analysis.

            SemanticModel model = compilation.GetSemanticModel(bodySyntax.SyntaxTree);
            IOperation bodyOperation = model.GetOperation(bodySyntax);

            if (bodyOperation == null || bodyOperation.HasErrors(compilation, cancellationToken))
            {
                return;
            }

            var walker = new AssignmentWalker(parameters);
            walker.Visit(bodyOperation);

            foreach (IParameterSymbol parameter in walker.ParametersAssigned)
            {
                collector.Add(Diagnostic.Create(Rule, parameter.Locations[0], parameter.Name));
            }
        }

        private sealed class AssignmentWalker : OperationWalker
        {
            [NotNull]
            private readonly IDictionary<IParameterSymbol, bool> seenAssignmentPerParameter =
                new Dictionary<IParameterSymbol, bool>();

            [NotNull]
            [ItemNotNull]
            public ICollection<IParameterSymbol> ParametersAssigned =>
                seenAssignmentPerParameter.Where(pair => pair.Value).Select(pair => pair.Key).ToArray();

            public AssignmentWalker([NotNull] [ItemNotNull] ICollection<IParameterSymbol> parameters)
            {
                Guard.NotNull(parameters, nameof(parameters));

                foreach (IParameterSymbol parameter in parameters)
                {
                    seenAssignmentPerParameter[parameter] = false;
                }
            }

            public override void VisitSimpleAssignment([NotNull] ISimpleAssignmentOperation operation)
            {
                RegisterAssignmentToParameter(operation.Target);

                base.VisitSimpleAssignment(operation);
            }

            public override void VisitCompoundAssignment([NotNull] ICompoundAssignmentOperation operation)
            {
                RegisterAssignmentToParameter(operation.Target);

                base.VisitCompoundAssignment(operation);
            }

            public override void VisitIncrementOrDecrement([NotNull] IIncrementOrDecrementOperation operation)
            {
                RegisterAssignmentToParameter(operation.Target);

                base.VisitIncrementOrDecrement(operation);
            }

            public override void VisitDeconstructionAssignment([NotNull] IDeconstructionAssignmentOperation operation)
            {
                if (operation.Target is ITupleOperation tuple)
                {
                    foreach (IOperation element in tuple.Elements)
                    {
                        RegisterAssignmentToParameter(element);
                    }
                }

                base.VisitDeconstructionAssignment(operation);
            }

            public override void VisitArgument([NotNull] IArgumentOperation operation)
            {
                if (operation.Parameter.RefKind == RefKind.Ref || operation.Parameter.RefKind == RefKind.Out)
                {
                    RegisterAssignmentToParameter(operation.Value);
                }

                base.VisitArgument(operation);
            }

            private void RegisterAssignmentToParameter([NotNull] IOperation operation)
            {
                if (operation is IParameterReferenceOperation parameterReference)
                {
                    IParameterSymbol parameter = parameterReference.Parameter;

                    if (seenAssignmentPerParameter.ContainsKey(parameter))
                    {
                        seenAssignmentPerParameter[parameter] = true;
                    }
                }
            }
        }
    }
}
