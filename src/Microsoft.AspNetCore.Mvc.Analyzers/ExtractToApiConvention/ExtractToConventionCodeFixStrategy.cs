// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.AspNetCore.Mvc.Analyzers.ExtractToApiConvention
{
    internal abstract class ExtractToConventionCodeFixStrategy
    {
        public abstract Task ExecuteAsync(ExtractToConventionCodeFixStrategyContext context);

        protected static NameSyntax SimplifiedTypeName(string typeName)
        {
            return SyntaxFactory.ParseName(typeName).WithAdditionalAnnotations(Simplifier.Annotation);
        }

        protected static MethodDeclarationSyntax CreateNewConventionMethod(ExtractToConventionCodeFixStrategyContext context, out IList<AttributeSyntax> methodAttributes)
        {
            var semanticModel = context.SemanticModel;
            var statusCodes = new HashSet<int>();
            methodAttributes = new List<AttributeSyntax>();

            foreach (var metadata in context.DeclaredApiResponseMetadata)
            {
                statusCodes.Add(metadata.StatusCode);

                if (metadata.IsImplicit)
                {
                    // Attribute is implicitly defined (and does not appear in source)
                    continue;
                }

                if (metadata.AttributeSource != context.Method)
                {
                    // Attribute isn't defined on a method.
                    continue;
                }

                var attributeSyntax = (AttributeSyntax)metadata.Attribute.ApplicationSyntaxReference.GetSyntax(context.CancellationToken);
                methodAttributes.Add(attributeSyntax);
            }

            var producesResponseTypeName = SimplifiedTypeName(SymbolNames.ProducesResponseTypeAttribute);
            foreach (var metadata in context.UndocumentedMetadata)
            {
                var statusCode = metadata.IsDefaultResponse ? 200 : metadata.StatusCode;
                statusCodes.Add(statusCode);
            }

            var conventionMethodAttributes = new SyntaxList<AttributeListSyntax>();
            foreach (var statusCode in statusCodes.OrderBy(s => s))
            {
                var producesResponseTypeAttribute = CreateProducesResponseTypeAttribute(statusCode);
                conventionMethodAttributes = conventionMethodAttributes.Add(SyntaxFactory.AttributeList().AddAttributes(producesResponseTypeAttribute));
            }

            var nameMatchBehaviorAttribute = CreateNameMatchAttribute(SymbolNames.ApiConventionNameMatchBehavior_Prefix);
            conventionMethodAttributes = conventionMethodAttributes.Add(SyntaxFactory.AttributeList().AddAttributes(nameMatchBehaviorAttribute));

            var voidType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
            var methodName = GetConventionMethodName(context.Method.Name);

            var conventionParameterList = SyntaxFactory.ParameterList();
            foreach (var parameter in context.Method.Parameters)
            {
                var parameterName = GetConventionParameterName(parameter.Name);
                var parameterType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword));

                var parameterNameMatchBehaviorAttribute = CreateNameMatchAttribute(SymbolNames.ApiConventionNameMatchBehavior_Suffix);
                var parameterTypeMatchBehaviorAttribute = CreateTypeMatchAttribute(SymbolNames.ApiConventionTypeMatchBehavior_Any);

                var conventionParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameterName))
                    .WithType(parameterType.WithAdditionalAnnotations(Simplifier.Annotation))
                    .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(parameterNameMatchBehaviorAttribute, parameterTypeMatchBehaviorAttribute));

                conventionParameterList = conventionParameterList.AddParameters(conventionParameter);
            }

            var method = SyntaxFactory.MethodDeclaration(voidType, methodName)
               .WithAttributeLists(conventionMethodAttributes)
               .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
               .WithBody(SyntaxFactory.Block())
               .WithParameterList(conventionParameterList);
            return method;
        }

        protected static AttributeSyntax CreateProducesResponseTypeAttribute(ActualApiResponseMetadata metadata)
        {
            var statusCode = metadata.IsDefaultResponse ? 200 : metadata.StatusCode;
            return CreateProducesResponseTypeAttribute(statusCode);
        }

        private static AttributeSyntax CreateProducesResponseTypeAttribute(int statusCode)
        {
            return SyntaxFactory.Attribute(
                SimplifiedTypeName(SymbolNames.ProducesResponseTypeAttribute),
                SyntaxFactory.AttributeArgumentList().AddArguments(
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(statusCode)))));
        }

        protected static AttributeSyntax CreateNameMatchAttribute(string nameMatchBehavior)
        {
            var attribute = SyntaxFactory.Attribute(
                SimplifiedTypeName(SymbolNames.ApiConventionNameMatchAttribute),
                SyntaxFactory.AttributeArgumentList().AddArguments(
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SimplifiedTypeName(SymbolNames.ApiConventionNameMatchBehavior),
                            SyntaxFactory.IdentifierName(nameMatchBehavior)))));
            return attribute;
        }

        protected static AttributeSyntax CreateTypeMatchAttribute(string typeMatchBehavior)
        {
            var attribute = SyntaxFactory.Attribute(
                SimplifiedTypeName(SymbolNames.ApiConventionTypeMatchAttribute),
                SyntaxFactory.AttributeArgumentList().AddArguments(
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SimplifiedTypeName(SymbolNames.ApiConventionTypeMatchBehavior),
                            SyntaxFactory.IdentifierName(typeMatchBehavior)))));
            return attribute;
        }

        protected internal static string GetConventionMethodName(string methodName)
        {
            // PostItem -> Post

            if (methodName.Length < 2)
            {
                return methodName;
            }

            for (var i = 1; i < methodName.Length; i++)
            {
                if (char.IsUpper(methodName[i]) && char.IsLower(methodName[i - 1]))
                {
                    return methodName.Substring(0, i);
                }
            }

            return methodName;
        }

        protected internal static string GetConventionParameterName(string parameterName)
        {
            // userName -> name

            if (parameterName.Length < 2)
            {
                return parameterName;
            }

            for (var i = parameterName.Length - 2; i > 0; i--)
            {
                if (char.IsUpper(parameterName[i]) && char.IsLower(parameterName[i - 1]))
                {
                    return char.ToLower(parameterName[i]) + parameterName.Substring(i + 1);
                }
            }

            return parameterName;
        }
    }
}
