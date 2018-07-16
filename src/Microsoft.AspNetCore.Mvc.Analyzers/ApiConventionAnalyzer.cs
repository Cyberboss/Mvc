// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ApiConventionAnalyzer : DiagnosticAnalyzer
    {
        internal const string ApiConventionInSourceKey = nameof(ApiConventionInSourceKey);
        internal const string StatusCodeKey = nameof(StatusCodeKey);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            DiagnosticDescriptors.MVC1004_ActionReturnsUndocumentedStatusCode,
            DiagnosticDescriptors.MVC1005_ActionReturnsUndocumentedSuccessResult,
            DiagnosticDescriptors.MVC1006_ActionDoesNotReturnDocumentedStatusCode);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(compilationStartAnalysisContext =>
            {
                var symbolCache = new ApiControllerSymbolCache(compilationStartAnalysisContext.Compilation);
                if (symbolCache.ApiConventionTypeAttribute == null || symbolCache.ApiConventionTypeAttribute.TypeKind == TypeKind.Error)
                {
                    // No-op if we can't find types we care about.
                    return;
                }

                InitializeWorker(compilationStartAnalysisContext, symbolCache);
            });
        }

        private void InitializeWorker(CompilationStartAnalysisContext compilationStartAnalysisContext, ApiControllerSymbolCache symbolCache)
        {
            compilationStartAnalysisContext.RegisterSyntaxNodeAction(syntaxNodeContext =>
            {
                var cancellationToken = syntaxNodeContext.CancellationToken;
                var methodSyntax = (MethodDeclarationSyntax)syntaxNodeContext.Node;
                var semanticModel = syntaxNodeContext.SemanticModel;
                var method = semanticModel.GetDeclaredSymbol(methodSyntax, syntaxNodeContext.CancellationToken);

                if (!ShouldEvaluateMethod(symbolCache, method))
                {
                    return;
                }

                var declaredResponseMetadata = SymbolApiResponseMetadataProvider.GetDeclaredResponseMetadata(symbolCache, method);
                var hasUnreadableStatusCodes = !SymbolApiResponseMetadataProvider.TryGetActualResponseMetadata(symbolCache, semanticModel, methodSyntax, cancellationToken, out var actualResponseMetadata);

                var additionalLocations = Enumerable.Empty<Location>();
                var diagnosticProperties = ImmutableDictionary<string, string>.Empty;
                if (!hasUnreadableStatusCodes)
                {
                    var conventionTypes = SymbolApiResponseMetadataProvider.GetConventionTypes(symbolCache, method);

                    var (updateableConvention, updateableConventionLocation) = GetConventionInSourceLocation(conventionTypes, cancellationToken);
                    if (updateableConventionLocation != Location.None)
                    {
                        additionalLocations = new[] { updateableConventionLocation };
                        diagnosticProperties = diagnosticProperties.Add(ApiConventionInSourceKey, updateableConvention.Name);
                    }
                }

                var hasUndocumentedStatusCodes = false;
                foreach (var actualMetadata in actualResponseMetadata)
                {
                    var location = actualMetadata.ReturnStatement.GetLocation();

                    if (!DeclaredApiResponseMetadata.HasStatusCode(declaredResponseMetadata, actualMetadata))
                    {
                        hasUndocumentedStatusCodes = true;
                        if (actualMetadata.IsDefaultResponse)
                        {

                            syntaxNodeContext.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.MVC1005_ActionReturnsUndocumentedSuccessResult,
                                location,
                                additionalLocations,
                                diagnosticProperties));
                        }
                        else
                        {
                            diagnosticProperties = diagnosticProperties.Add(StatusCodeKey, actualMetadata.StatusCode.ToString(CultureInfo.InvariantCulture));

                            syntaxNodeContext.ReportDiagnostic(Diagnostic.Create(
                               DiagnosticDescriptors.MVC1004_ActionReturnsUndocumentedStatusCode,
                               location,
                               additionalLocations,
                               diagnosticProperties,
                               actualMetadata.StatusCode));
                        }
                    }
                }

                if (hasUndocumentedStatusCodes || hasUnreadableStatusCodes)
                {
                    // If we produced analyzer warnings about undocumented status codes, don't attempt to determine
                    // if there are documented status codes that are missing from the method body.
                    return;
                }

                for (var i = 0; i < declaredResponseMetadata.Count; i++)
                {
                    var expectedStatusCode = declaredResponseMetadata[i].StatusCode;
                    if (!HasStatusCode(actualResponseMetadata, expectedStatusCode))
                    {
                        syntaxNodeContext.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.MVC1006_ActionDoesNotReturnDocumentedStatusCode,
                            methodSyntax.Identifier.GetLocation(),
                            expectedStatusCode));
                    }
                }

            }, SyntaxKind.MethodDeclaration);
        }

        private (ITypeSymbol, Location) GetConventionInSourceLocation(IReadOnlyList<ITypeSymbol> conventionTypes, CancellationToken cancellationToken)
        {
            for (var i = 0; i < conventionTypes.Count; i++)
            {
                var conventionType = conventionTypes[i];
                if (conventionType.DeclaringSyntaxReferences.IsDefaultOrEmpty)
                {
                    continue;
                }

                var syntaxReference = conventionType.DeclaringSyntaxReferences[0];
                var syntax = syntaxReference.GetSyntax(cancellationToken);
                var location = syntax.GetLocation();
                if (location.IsInSource)
                {
                    return (conventionType, location);
                }
            }

            return (null, Location.None);
        }

        internal static bool ShouldEvaluateMethod(ApiControllerSymbolCache symbolCache, IMethodSymbol method)
        {
            if (method == null)
            {
                return false;
            }

            if (method.ReturnsVoid || method.ReturnType.TypeKind == TypeKind.Error)
            {
                return false;
            }

            if (!MvcFacts.IsController(method.ContainingType, symbolCache.ControllerAttribute, symbolCache.NonControllerAttribute))
            {
                return false;
            }

            if (!method.ContainingType.HasAttribute(symbolCache.IApiBehaviorMetadata, inherit: true))
            {
                return false;
            }

            if (!MvcFacts.IsControllerAction(method, symbolCache.NonActionAttribute, symbolCache.IDisposableDispose))
            {
                return false;
            }

            return true;
        }

        internal static bool HasStatusCode(IList<ActualApiResponseMetadata> actualResponseMetadata, int statusCode)
        {
            for (var i = 0; i < actualResponseMetadata.Count; i++)
            {
                if (actualResponseMetadata[i].IsDefaultResponse)
                {
                    return statusCode == 200 || statusCode == 201;
                }

                else if (actualResponseMetadata[i].StatusCode == statusCode)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
