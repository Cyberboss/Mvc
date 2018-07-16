// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.AspNetCore.Mvc.Analyzers.ExtractToApiConvention
{
    internal class CloneConventionCodeFixStrategy : ExtractToConventionCodeFixStrategy
    {
        public override async Task ExecuteAsync(ExtractToConventionCodeFixStrategyContext context)
        {
            var project = context.Document.Project;
            var solutionEditor = new SolutionEditor(project.Solution);
            var documentEditor = await solutionEditor.GetDocumentEditorAsync(context.Document.Id).ConfigureAwait(false);
            
            var method = CreateNewConventionMethod(context, out var methodAttributes);
            foreach (var attribute in methodAttributes)
            {
                documentEditor.RemoveNode(attribute);
            }

            var (syntaxNode, typeName, fullyQualifiedTypeName) = CreateNewConventionType(context);

            var attributeOnType = context.Method.ContainingType
                .GetAttributes(context.SymbolCache.ApiConventionTypeAttribute)
                .FirstOrDefault();

            var conventionTypeArguments = SyntaxFactory
                .AttributeArgumentList()
                .AddArguments(SyntaxFactory.AttributeArgument(
                    SyntaxFactory.TypeOfExpression(SimplifiedTypeName(fullyQualifiedTypeName))));

            if (attributeOnType != null)
            {
                var attributeSyntax = (AttributeSyntax)await attributeOnType.ApplicationSyntaxReference.GetSyntaxAsync(context.CancellationToken);
                documentEditor.ReplaceNode(attributeSyntax.ArgumentList, conventionTypeArguments);
            }
            else
            {
                documentEditor.AddAttribute(
                    context.MethodSyntax.FirstAncestorOrSelf<ClassDeclarationSyntax>(),
                    SyntaxFactory.Attribute(SimplifiedTypeName(SymbolNames.ApiConventionTypeAttribute))
                        .WithArgumentList(conventionTypeArguments));
            }

            var document = solutionEditor
                .GetChangedSolution()
                .GetProject(project.Id)
                .AddDocument(typeName, syntaxNode);

            context.ChangedSolution = document.Project.Solution;
        }

        private (SyntaxNode, string typeName, string fullyQualifiedName) CreateNewConventionType(ExtractToConventionCodeFixStrategyContext context)
        {
            var conventionMethod = CreateNewConventionMethod(context, out var clonedAttributes);

            var conventionTypeName = context.Document.Project.Name + "Conventions";
            var conventionType = SyntaxFactory.ClassDeclaration(conventionTypeName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                .AddMembers(conventionMethod);

            var conventionNamespace = context.MethodSyntax.FirstAncestorOrSelf<NamespaceDeclarationSyntax>();
            if (conventionNamespace != null)
            {
                conventionNamespace = SyntaxFactory.NamespaceDeclaration(conventionNamespace.Name).AddMembers(conventionType);
            }

            var compilationUnit = SyntaxFactory.CompilationUnit()
                .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Microsoft.AspNetCore.Mvc")))
                .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Microsoft.AspNetCore.Mvc.ApiExplorer")))
                .AddMembers((MemberDeclarationSyntax)conventionNamespace ?? conventionType);

            return (compilationUnit, conventionTypeName, conventionNamespace.Name.ToString() + "." + conventionTypeName);
        }
    }
}
