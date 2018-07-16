// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Mvc.Analyzers.ExtractToApiConvention
{
    internal class ExtractToConventionCodeFix : CodeAction
    {
        private readonly Document _document;
        private readonly Diagnostic _diagnostic;
        private readonly ExtractToConventionCodeFixStrategy[] _strategies;
        private bool _fixExecuted;
        private Solution _changedSolution;

        public ExtractToConventionCodeFix(
            Document document, 
            Diagnostic diagnostic, 
            ExtractToConventionCodeFixStrategy[] strategies,
            string title)
        {
            _document = document;
            _diagnostic = diagnostic;
            _strategies = strategies;

            Title = title;
        }

        public override string Title { get; }

        public Document AnalyzerDocument { get; set; }

        protected override async Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
        {
            await CalculateFixAsync(cancellationToken);
            return _changedSolution ?? _document.Project.Solution;
        }

        private async Task CalculateFixAsync(CancellationToken cancellationToken)
        {
            if (_fixExecuted)
            {
                return;
            }

            var root = await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await _document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var methodReturnStatement = (ReturnStatementSyntax)root.FindNode(_diagnostic.Location.SourceSpan);
            var methodSyntax = methodReturnStatement.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            var method = semanticModel.GetDeclaredSymbol(methodSyntax, cancellationToken);

            var symbolCache = new ApiControllerSymbolCache(semanticModel.Compilation);
            var declaredResponseMetadata = SymbolApiResponseMetadataProvider.GetDeclaredResponseMetadata(symbolCache, method);

            if (!SymbolApiResponseMetadataProvider.TryGetActualResponseMetadata(symbolCache, semanticModel, methodSyntax, cancellationToken, out var actualResponseMetadata))
            {
                // If we cannot parse metadata correctly, don't offer a code fix.
                return;
            }

            var undocumentedMetadata = new List<ActualApiResponseMetadata>();
            foreach (var metadata in actualResponseMetadata)
            {
                if (!DeclaredApiResponseMetadata.HasStatusCode(declaredResponseMetadata, metadata))
                {
                    undocumentedMetadata.Add(metadata);
                }
            }

            var context = new ExtractToConventionCodeFixStrategyContext(
                _document,
                semanticModel,
                symbolCache,
                method,
                methodSyntax,
                declaredResponseMetadata,
                undocumentedMetadata,
                cancellationToken);

            context.AnalyzerDocument = AnalyzerDocument;

            foreach (var strategy in _strategies)
            {
                await strategy.ExecuteAsync(context).ConfigureAwait(false);
                if (context.Success)
                {
                    _changedSolution = context.ChangedSolution;
                    break;
                }
            }

            _fixExecuted = true;
        }
    }
}
