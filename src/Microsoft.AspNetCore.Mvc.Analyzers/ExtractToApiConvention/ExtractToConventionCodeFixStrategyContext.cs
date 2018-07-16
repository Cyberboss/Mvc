// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Mvc.Analyzers.ExtractToApiConvention
{
    internal class ExtractToConventionCodeFixStrategyContext
    {
        private Solution _solution;

        public ExtractToConventionCodeFixStrategyContext(
            Document methodDocument,
            SemanticModel semanticModel,
            ApiControllerSymbolCache symbolCache,
            IMethodSymbol method,
            MethodDeclarationSyntax methodSyntax,
            IList<DeclaredApiResponseMetadata> declaredMetadata,
            IList<ActualApiResponseMetadata> undocumentedMetadata,
            CancellationToken cancellationToken)
        {
            Document = methodDocument;
            SemanticModel = semanticModel;
            SymbolCache = symbolCache;
            Method = method;
            MethodSyntax = methodSyntax;
            DeclaredApiResponseMetadata = declaredMetadata;
            UndocumentedMetadata = undocumentedMetadata;
            CancellationToken = cancellationToken;
        }

        public Document Document { get; }
        public SemanticModel SemanticModel { get; }
        public ApiControllerSymbolCache SymbolCache { get; }
        public IMethodSymbol Method { get; }
        public MethodDeclarationSyntax MethodSyntax { get; }
        public IList<DeclaredApiResponseMetadata> DeclaredApiResponseMetadata { get; }
        public IList<ActualApiResponseMetadata> UndocumentedMetadata { get; }
        public CancellationToken CancellationToken { get; }

        public Document AnalyzerDocument { get; set; }

        public Solution ChangedSolution
        {
            get => _solution;
            set
            {
                _solution = value;
                Success = true;
            }
        }

        public bool Success { get; set; }
    }

}
