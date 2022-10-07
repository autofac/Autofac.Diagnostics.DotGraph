// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.CodeDom;
using Microsoft.CSharp;

namespace Autofac.Diagnostics.DotGraph
{
    /// <summary>
    /// Extension methods for building DOT graph components from types.
    /// </summary>
    internal static class TypeExtensions
    {
        /// <summary>
        /// Gets the C# code text for a given type. This is effectively a "pretty printed" version
        /// of the type name.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> for which the C# name should be generated.</param>
        /// <returns>
        /// The name of the <paramref name="type"/> as it would be seen if one was
        /// writing C# code referencing the type.
        /// </returns>
        public static string CSharpName(this Type type)
        {
            using var provider = new CSharpCodeProvider();
            var typeRef = new CodeTypeReference(type);
            return provider.GetTypeOutput(typeRef);
        }
    }
}
