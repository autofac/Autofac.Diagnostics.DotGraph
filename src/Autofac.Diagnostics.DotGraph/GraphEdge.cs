// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Core;

namespace Autofac.Diagnostics.DotGraph
{
    /// <summary>
    /// An edge that connects two nodes (two resolve requests) in a graph.
    /// The source of an edge is the request that's resolving child items;
    /// the target is a specific service on a child request.
    /// </summary>
    internal class GraphEdge : IEquatable<GraphEdge>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GraphEdge"/> class.
        /// </summary>
        /// <param name="request">
        /// A unique ID for the resolve request to which the edge should be drawn. If the edge is associated
        /// with request 'A' and 'A' calls into request 'B', this is the ID of 'B'. This corresponds to a
        /// <see cref="ResolveRequestNode.Id"/> value.
        /// </param>
        /// <param name="service">
        /// A <see cref="Service"/> being resolved in the request. This service should appear in the
        /// <see cref="ResolveRequestNode.Services"/> dictionary of the target resolve request.
        /// </param>
        public GraphEdge(Guid request, Service service)
        {
            Request = request;
            Service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Gets the unique ID for the destination resolve request.
        /// </summary>
        /// <value>
        /// A unique ID for the resolve request to which the edge should be drawn. If the edge is associated
        /// with request 'A' and 'A' calls into request 'B', this is the ID of 'B'. This corresponds to a
        /// <see cref="ResolveRequestNode.Id"/> value.
        /// </value>
        public Guid Request { get; private set; }

        /// <summary>
        /// Gets the service being resolved.
        /// </summary>
        /// <value>
        /// The <see cref="Service"/> being resolved in the request. This service should appear in the
        /// <see cref="ResolveRequestNode.Services"/> dictionary of the target resolve request.
        /// </value>
        public Service Service { get; private set; }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">
        /// An object to compare with this object.
        /// </param>
        /// <returns><see langword="true"/> if the two objects are equal; otherwise <see langword="false"/>.</returns>
        public bool Equals(GraphEdge? other)
        {
            return
                other is not null &&
                other.Request == Request &&
                ((other.Service is not null && Service is not null && other.Service.Equals(Service)) ||
                (other.Service is null && Service is null));
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as GraphEdge);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            // This doesn't have to be great; we don't really use it
            // but analyzers complain since we do need equality.
            return Request.GetHashCode() ^ Service.GetHashCode();
        }
    }
}
