﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class CA1036DiagnosticAnalyzer : AbstractNamedTypeAnalyzer
    {
        internal const string RuleId = "CA1036";
        private static LocalizableString localizableTitle = new LocalizableResourceString(nameof(FxCopRulesResources.OverloadOperatorEqualsOnIComparableInterface), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        private static LocalizableString localizableMessage = new LocalizableResourceString(nameof(FxCopRulesResources.OverloadOperatorEqualsOnIComparableInterface), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        private static LocalizableString localizableDescription = new LocalizableResourceString(nameof(FxCopRulesResources.OverloadOperatorEqualsOnIComparableInterfaceDescription), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                                  localizableTitle,
                                                                                  localizableMessage,
                                                                                  FxCopDiagnosticCategory.Design,
                                                                                  DiagnosticSeverity.Warning,
                                                                                  isEnabledByDefault: true,
                                                                                  description: localizableDescription,
                                                                                  helpLinkUri: "http://msdn.microsoft.com/library/ms182163.aspx",
                                                                                  customTags: DiagnosticCustomTags.Microsoft);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        protected override void AnalyzeSymbol(INamedTypeSymbol namedTypeSymbol, Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            var comparableType = WellKnownTypes.IComparable(compilation);

            if (comparableType == null)
            {
                return;
            }

            if (namedTypeSymbol.DeclaredAccessibility == Accessibility.Private)
            {
                return;
            }

            if (namedTypeSymbol.Interfaces.Contains(comparableType))
            {
                if (!(DoesOverrideEquals(namedTypeSymbol) && IsEqualityOperatorImplemented(namedTypeSymbol)))
                {
                    addDiagnostic(namedTypeSymbol.CreateDiagnostic(Rule));
                }
            }
        }

        private static bool DoesOverrideEquals(INamedTypeSymbol symbol)
        {
            // Does the symbol override Object.Equals?
            return symbol.GetMembers(WellKnownMemberNames.ObjectEquals).OfType<IMethodSymbol>().Where(m => IsEqualsOverride(m)).Any();
        }

        // Rule: A public or protected type implements the System.IComparable interface and 
        // does not override Object.Equals or does not overload the language-specific operator
        // for equality, inequality, less than, or greater than. The rule does not report a
        // violation if the type inherits only an implementation of the interface.
        private static bool IsEqualsOverride(IMethodSymbol method)
        {
            // TODO: reimplement using OverriddenMethods, possibly exposing that property if needed
            return method.IsOverride &&
                   method.ReturnType.SpecialType == SpecialType.System_Boolean &&
                   method.Parameters.Length == 1 &&
                   method.Parameters[0].Type.SpecialType == SpecialType.System_Object;
        }

        private static bool IsEqualityOperatorImplemented(INamedTypeSymbol symbol)
        {
            // Does the symbol overload all of the equality operators?  (All are required per http://msdn.microsoft.com/en-us/library/ms182163.aspx example.)
            return IsOperatorImplemented(symbol, WellKnownMemberNames.EqualityOperatorName) &&
                    IsOperatorImplemented(symbol, WellKnownMemberNames.InequalityOperatorName) &&
                    IsOperatorImplemented(symbol, WellKnownMemberNames.LessThanOperatorName) &&
                    IsOperatorImplemented(symbol, WellKnownMemberNames.GreaterThanOperatorName);
        }

        private static bool IsOperatorImplemented(INamedTypeSymbol symbol, string op)
        {
            // TODO: should this filter on the right-hand-side operator type?
            return symbol.GetMembers(op).OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.UserDefinedOperator).Any();
        }
    }
}
