// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.AspNetCore.Mvc.Analyzers.ExtractToApiConvention
{
    internal sealed class AddNewConventionMethodToExistingConventionCodeFixStrategy : ExtractToConventionCodeFixStrategy
    {
        public override async Task ExecuteAsync(ExtractToConventionCodeFixStrategyContext context)
        {
            var (result, conventionTypeSyntax) = await TryGetExistingConventionType(context).ConfigureAwait(false);
            if (!result)
            {
                return;
            }

            var solutionEditor = new SolutionEditor(context.Document.Project.Solution);
            var documentEditor = await solutionEditor.GetDocumentEditorAsync(context.Document.Id).ConfigureAwait(false);
            var conventionDocumentEditor = await solutionEditor.GetDocumentEditorAsync(context.AnalyzerDocument.Id).ConfigureAwait(false);

            var method = CreateNewConventionMethod(context, out var clonedAttributes);
            foreach (var attribute in clonedAttributes)
            {
                documentEditor.RemoveNode(attribute);
            }

            conventionDocumentEditor.AddMember(conventionTypeSyntax, method);

            context.ChangedSolution = solutionEditor.GetChangedSolution();
        }

        private async Task<(bool, TypeDeclarationSyntax)> TryGetExistingConventionType(ExtractToConventionCodeFixStrategyContext context)
        {
            var conventionTypes = SymbolApiResponseMetadataProvider.GetConventionTypes(context.SymbolCache, context.Method);
            foreach (var conventionType in conventionTypes)
            {
                var syntaxReferences = conventionType.DeclaringSyntaxReferences;
                if (syntaxReferences.Length == 0)
                {
                    continue;
                }

                var syntaxReference = syntaxReferences[0];
                var typeDeclarationSyntax = (TypeDeclarationSyntax)await syntaxReference.GetSyntaxAsync(context.CancellationToken).ConfigureAwait(false);
                if (typeDeclarationSyntax.GetLocation().IsInSource)
                {
                    return (true, typeDeclarationSyntax);
                }
            }

            return (false, null);
        }
    }
}
