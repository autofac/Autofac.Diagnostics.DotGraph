// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Core;
using Autofac.Core.Activators.Delegate;

namespace Autofac.Diagnostics.DotGraph;

/// <summary>
/// Extension methods for activators.
/// </summary>
internal static class ActivatorExtensions
{
    /// <summary>
    /// This shorthand name for the activator is used in exception messages; for activator types
    /// where the limit type generally describes the activator exactly, we use that; for delegate
    /// activators, a variation on the type name is used to indicate this.
    /// </summary>
    /// <param name="activator">The activator instance.</param>
    /// <returns>A display name.</returns>
    public static string DisplayName(this IInstanceActivator activator)
    {
        // There is a similar class in the Autofac core library but
        // this gives us full control over display in the graph as
        // well as not requiring DisplayName be part of the public API.
        var fullName = activator?.LimitType.CSharpName() ?? "";
        return activator is DelegateActivator ?
            $"λ:{fullName}" :
            fullName;
    }
}
