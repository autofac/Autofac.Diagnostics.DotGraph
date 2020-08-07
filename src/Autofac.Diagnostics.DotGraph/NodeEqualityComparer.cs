// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Autofac.Diagnostics.DotGraph
{
    /// <summary>
    /// Equality comparer that determines if two resolve requests are effectively the
    /// same based on the returned instance. Used to find "duplicates" in the graph
    /// during normalization.
    /// </summary>
    internal class NodeEqualityComparer : IEqualityComparer<ResolveRequestNode>
    {
        /// <summary>
        /// Gets the singleton default instance of the comparer.
        /// </summary>
        /// <value>
        /// A singleton <see cref="NodeEqualityComparer"/> that can be used in graph normalization.
        /// </value>
        public static NodeEqualityComparer Default { get; } = new NodeEqualityComparer();

        /// <summary>
        /// Determines whether the specified objects are equal.
        /// </summary>
        /// <param name="x">
        /// The first <see cref="ResolveRequestNode"/> to compare.
        /// </param>
        /// <param name="y">
        /// The second <see cref="ResolveRequestNode"/> to compare.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the specified objects are equal; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Equals(ResolveRequestNode x, ResolveRequestNode y) => ReferenceEquals(x.Instance, y.Instance);

        /// <summary>
        /// Returns a hash code for the specified object.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="ResolveRequestNode"/> for which a hash code is to be returned.
        /// </param>
        /// <returns>
        /// A hash code for the specified object.
        /// </returns>
        public int GetHashCode(ResolveRequestNode obj) => RuntimeHelpers.GetHashCode(obj.Instance);
    }
}
