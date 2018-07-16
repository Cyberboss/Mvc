// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.AspNetCore.Mvc.Analyzers.ExtractToApiConvention
{
    internal sealed class UpdateExistingConventionMethodCodeFixStrategy : ExtractToConventionCodeFixStrategy
    {
        public override async Task ExecuteAsync(ExtractToConventionCodeFixStrategyContext context)
        {
            var (result, methodSyntax) = await TryGetExistingConventionMethod(context).ConfigureAwait(false);

            if (!result)
            {
                return;
            }

            var solutionEditor = new SolutionEditor(context.Document.Project.Solution);
            var documentEditor = await solutionEditor.GetDocumentEditorAsync(context.Document.Id).ConfigureAwait(false);
            var conventionDocumentEditor = await solutionEditor.GetDocumentEditorAsync(context.AnalyzerDocument.Id).ConfigureAwait(false);

            foreach (var metadata in context.UndocumentedMetadata)
            {
                var attribute = CreateProducesResponseTypeAttribute(metadata);
                conventionDocumentEditor.AddAttribute(methodSyntax, attribute);
            }

            context.ChangedSolution = solutionEditor.GetChangedSolution();
        }

        internal async Task<(bool, MethodDeclarationSyntax)> TryGetExistingConventionMethod(ExtractToConventionCodeFixStrategyContext context)
        {
            if (context.DeclaredApiResponseMetadata.Count == 0 || context.DeclaredApiResponseMetadata[0].IsImplicit)
            {
                return (false, null);
            }

            var sourceMethod = context.DeclaredApiResponseMetadata[0].AttributeSource;
            if (context.Method == sourceMethod)
            {
                // The attribute is defined on the method. In this case, we need to create a new convention.
                return (false, null);
            }

            // Ensure that the convention exists in code.
            if (sourceMethod.DeclaringSyntaxReferences.Length != 1)
            {
                return (false, null);
            }

            var syntaxReference = sourceMethod.DeclaringSyntaxReferences[0];
            var syntaxToUpdate = (MethodDeclarationSyntax)await syntaxReference.GetSyntaxAsync(context.CancellationToken).ConfigureAwait(false);
            return (syntaxToUpdate.GetLocation().IsInSource, syntaxToUpdate);
        }
    }
}
