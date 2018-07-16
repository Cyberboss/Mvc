// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    [Shared]
    public class AddResponseTypeAttributeCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            DiagnosticDescriptors.MVC1004_ActionReturnsUndocumentedStatusCode.Id,
            DiagnosticDescriptors.MVC1005_ActionReturnsUndocumentedSuccessResult.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (context.Diagnostics.Length == 0)
            {
                return Task.CompletedTask;
            }

            var diagnostic = context.Diagnostics[0];
            if ((diagnostic.Descriptor.Id != DiagnosticDescriptors.MVC1004_ActionReturnsUndocumentedStatusCode.Id) &&
                (diagnostic.Descriptor.Id != DiagnosticDescriptors.MVC1005_ActionReturnsUndocumentedSuccessResult.Id))
            {
                return Task.CompletedTask;
            }

            var codeFix = new MyCodeAction(context.Document, diagnostic, context.Span);

            context.RegisterCodeFix(codeFix, diagnostic);
            return Task.CompletedTask;
        }

        private sealed class MyCodeAction : CodeAction
        {
            private Document _document;
            private readonly Diagnostic _diagnostic;
            private readonly TextSpan _textSpan;

            public MyCodeAction(Document document, Diagnostic diagnostic, TextSpan textSpan)
            {
                _document = document;
                _diagnostic = diagnostic;
                _textSpan = textSpan;
            }

            public override string EquivalenceKey => _diagnostic.Id;

            public override string Title => "Add ProducesResponseType attributes to method";

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var statusCode = 200;
                if (_diagnostic.Properties.TryGetValue(ApiConventionAnalyzer.StatusCodeKey, out var statusCodeString))
                {
                    statusCode = int.Parse(statusCodeString, CultureInfo.InvariantCulture);
                }
                
                var attribute = SyntaxFactory.Attribute(
                    SyntaxFactory.ParseName(SymbolNames.ProducesResponseTypeAttribute)
                        .WithAdditionalAnnotations(Simplifier.Annotation),
                    SyntaxFactory.AttributeArgumentList().AddArguments(
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(statusCode)))));

                var root = await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var methodDeclaration = root.FindNode(_textSpan).FirstAncestorOrSelf<MethodDeclarationSyntax>();

                var documentEditor = await DocumentEditor.CreateAsync(_document, cancellationToken);
                documentEditor.AddAttribute(methodDeclaration, attribute);

                return documentEditor.GetChangedDocument();
            }
        }
    }
}
