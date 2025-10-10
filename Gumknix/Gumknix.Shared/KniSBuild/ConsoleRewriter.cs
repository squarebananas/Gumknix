using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gumknix.KniSBuild
{
    internal class ConsoleRewriter
    {
        private readonly HashSet<MethodDeclarationSyntax> _methodsToMakeAsync = [];
        private readonly HashSet<LocalFunctionStatementSyntax> _localFunctionsToMakeAsync = [];

        internal SyntaxNode Rewrite(SyntaxNode root)
        {
            RewriterInnerPass innerPass = new(this);
            SyntaxNode rewrittenRoot = innerPass.Visit(root);

            RewriterOuterPass outerPass = new(this);
            rewrittenRoot = outerPass.Visit(rewrittenRoot);
            return rewrittenRoot;
        }

        private class RewriterInnerPass : CSharpSyntaxRewriter
        {
            private readonly ConsoleRewriter _consoleRewriter;

            public RewriterInnerPass(ConsoleRewriter consoleRewriter)
            {
                _consoleRewriter = consoleRewriter;
            }

            public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                if (node.Expression is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Expression is IdentifierNameSyntax identifierName &&
                    identifierName.Identifier.Text == "Console")
                {
                    string methodName = memberAccess.Name.Identifier.Text;
                    if (methodName == "ReadKey" || methodName == "Read" || methodName == "ReadLine" ||
                        methodName == "Beep")
                    {
                        if (node.Parent is AwaitExpressionSyntax)
                            return base.VisitInvocationExpression(node);

                        if (node.Parent is MemberAccessExpressionSyntax)
                        {
                            ParenthesizedExpressionSyntax awaitExpressionInParentheses = SyntaxFactory.ParenthesizedExpression(
                                SyntaxFactory.AwaitExpression(node)
                                    .WithLeadingTrivia(node.GetLeadingTrivia())
                                    .WithTrailingTrivia(node.GetTrailingTrivia())
                                    .NormalizeWhitespace());
                            CheckContainingMethodIsAsync(node);
                            return awaitExpressionInParentheses;
                        }

                        AwaitExpressionSyntax awaitExpression = SyntaxFactory.AwaitExpression(node)
                            .WithLeadingTrivia(node.GetLeadingTrivia())
                            .WithTrailingTrivia(node.GetTrailingTrivia())
                            .NormalizeWhitespace();
                        CheckContainingMethodIsAsync(node);
                        return awaitExpression;
                    }
                }
                return base.VisitInvocationExpression(node);
            }

            public void CheckContainingMethodIsAsync(InvocationExpressionSyntax node)
            {
                MethodDeclarationSyntax containingMethod = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                if (containingMethod != null)
                {
                    if (containingMethod.Identifier.Text == "GumknixEntryPoint")
                        return;
                    if (containingMethod.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.AsyncKeyword)) == false)
                        _consoleRewriter._methodsToMakeAsync.Add(containingMethod);
                }

                LocalFunctionStatementSyntax containingLocalFunction = node.Ancestors().OfType<LocalFunctionStatementSyntax>().FirstOrDefault();
                if (containingLocalFunction != null)
                {
                    if (containingLocalFunction.Identifier.Text == "GumknixEntryPoint")
                        return;
                    if (containingLocalFunction.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.AsyncKeyword)) == false)
                        _consoleRewriter._localFunctionsToMakeAsync.Add(containingLocalFunction);
                }
            }
        }

        private class RewriterOuterPass : CSharpSyntaxRewriter
        {
            private readonly ConsoleRewriter _consoleRewriter;

            public RewriterOuterPass(ConsoleRewriter consoleRewriter)
            {
                _consoleRewriter = consoleRewriter;
            }

            public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (_consoleRewriter._methodsToMakeAsync.Any(method => method.Identifier.Text == node.Identifier.Text))
                {
                    MethodDeclarationSyntax newMethod = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node);
                    if (newMethod.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.AsyncKeyword)) == false)
                        newMethod = newMethod.WithModifiers(newMethod.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.AsyncKeyword)))
                            .NormalizeWhitespace();

                    if (newMethod.ReturnType is PredefinedTypeSyntax predefinedType &&
                        predefinedType.Keyword.IsKind(SyntaxKind.VoidKeyword))
                        newMethod = newMethod.WithReturnType(
                            SyntaxFactory.ParseTypeName("Task ")
                            .WithLeadingTrivia(predefinedType.GetLeadingTrivia())
                            .WithTrailingTrivia(predefinedType.GetTrailingTrivia()));

                    return newMethod;
                }

                return base.VisitMethodDeclaration(node);
            }

            public override SyntaxNode VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
            {
                if (_consoleRewriter._localFunctionsToMakeAsync.Any(function => function.Identifier.Text == node.Identifier.Text))
                {
                    LocalFunctionStatementSyntax newFunction = (LocalFunctionStatementSyntax)base.VisitLocalFunctionStatement(node);
                    if (newFunction.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.AsyncKeyword)) == false)
                        newFunction = newFunction.WithModifiers(newFunction.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.AsyncKeyword)))
                            .NormalizeWhitespace();

                    if (newFunction.ReturnType is PredefinedTypeSyntax predefinedType &&
                        predefinedType.Keyword.IsKind(SyntaxKind.VoidKeyword))
                        newFunction = newFunction.WithReturnType(
                            SyntaxFactory.ParseTypeName("Task ")
                            .WithLeadingTrivia(predefinedType.GetLeadingTrivia())
                            .WithTrailingTrivia(predefinedType.GetTrailingTrivia()));

                    return newFunction;
                }

                return base.VisitLocalFunctionStatement(node);
            }

            public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                if (node.Expression is IdentifierNameSyntax identifierName)
                {
                    string methodName = identifierName.Identifier.Text;
                    if ((_consoleRewriter._methodsToMakeAsync.Any(method => method.Identifier.Text == methodName)) ||
                        (_consoleRewriter._localFunctionsToMakeAsync.Any(method => method.Identifier.Text == methodName)))
                    {
                        if (node.Parent is AwaitExpressionSyntax)
                            return base.VisitInvocationExpression(node);

                        if (node.Parent is MemberAccessExpressionSyntax)
                        {
                            ParenthesizedExpressionSyntax awaitExpressionInParentheses = SyntaxFactory.ParenthesizedExpression(
                                SyntaxFactory.AwaitExpression(node)
                                    .WithLeadingTrivia(node.GetLeadingTrivia())
                                    .WithTrailingTrivia(node.GetTrailingTrivia())
                                    .NormalizeWhitespace());
                            return awaitExpressionInParentheses;
                        }

                        AwaitExpressionSyntax awaitExpression = SyntaxFactory.AwaitExpression(node)
                            .WithLeadingTrivia(node.GetLeadingTrivia())
                            .WithTrailingTrivia(node.GetTrailingTrivia())
                            .NormalizeWhitespace();
                        return awaitExpression;
                    }
                }
                return base.VisitInvocationExpression(node);
            }
        }
    }
}
