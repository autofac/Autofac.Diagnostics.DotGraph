// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac.Core;

namespace Autofac.Diagnostics.DotGraph
{
    /// <summary>
    /// One node in the graph. The resulting instance (for successful requests)
    /// is what uniquely identifies the node when normalizing the data. This
    /// converts the notion of a "resolve request" into a dependency graph
    /// based on completed resolutions.
    /// </summary>
    internal class ResolveRequestNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResolveRequestNode"/> class.
        /// </summary>
        /// <param name="component">
        /// The display name of the component in the graph. A single component can support multiple services.
        /// </param>
        public ResolveRequestNode(string component)
        {
            Services = new Dictionary<Service, Guid>();
            Component = component;
            Id = Guid.NewGuid();
            Edges = new HashSet<GraphEdge>();
        }

        /// <summary>
        /// Gets the unique ID for the component node in the graph.
        /// </summary>
        /// <value>
        /// A <see cref="Guid"/> that uniquely identifies the component node in the graph.
        /// This is important because a given component may be resolved multiple times in
        /// an overall operation.
        /// </value>
        public Guid Id { get; }

        /// <summary>
        /// Gets the set of services and unique IDs fulfilled by this component.
        /// </summary>
        /// <value>
        /// A <see cref="Dictionary{K,V}"/> of <see cref="Service"/> that are associated
        /// with this component. Each associated value is a child ID that can be used
        /// for generating graph edges pointing directly to the service itself.
        /// </value>
        public Dictionary<Service, Guid> Services { get; private set; }

        /// <summary>
        /// Gets the display name of the component in the graph.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that can be used in the graph as the title for
        /// the component node.
        /// </value>
        public string Component { get; private set; }

        /// <summary>
        /// Gets or sets the decorator target display name for the graph node.
        /// </summary>
        /// <value>
        /// An optional <see cref="string"/> that indicates this request is for a
        /// decorator and this is the display name of the thing being decorated.
        /// </value>
        public string? DecoratorTarget { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the request was successful.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the request resulted in a successful resolution;
        /// <see langword="false"/> if the request failed.
        /// </value>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error explaining why the request failed.
        /// </summary>
        /// <value>
        /// An <see cref="Exception"/> that contains information about why the request failed.
        /// This will only be available if <see cref="Success"/> is <see langword="false"/>.
        /// </value>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Gets or sets the instance that was resolved in this request.
        /// </summary>
        /// <value>
        /// An <see cref="object"/> that resulted from the resolve request. This will
        /// only be available if <see cref="Success"/> is <see langword="true"/>.
        /// </value>
        public object? Instance { get; set; }

        /// <summary>
        /// Gets a list of edges in the graph that originate at this node.
        /// </summary>
        /// <value>
        /// A <see cref="HashSet{T}"/> that contains a unique set of graph edges
        /// originating with this resolve request. These generally point to
        /// other services that were resolved during this request (i.e., child
        /// resolve requests).
        /// </value>
        public HashSet<GraphEdge> Edges { get; }

        /// <summary>
        /// Serializes this node to DOT graph format. This is generally only done after
        /// the overall resolve operation is complete.
        /// </summary>
        /// <param name="stringBuilder">
        /// The <see cref="StringBuilder"/> to which the node content should be written.
        /// </param>
        /// <param name="allRequests">
        /// A <see cref="RequestDictionary"/> that contains all the other resolve requests
        /// that occurred in the complete resolve operation. It is used to ensure
        /// graph edges are drawn to the correct destination service with the proper
        /// success/failure formatting.
        /// </param>
        public void ToString(StringBuilder stringBuilder, RequestDictionary allRequests)
        {
            var shape = DecoratorTarget == null ? "component" : "box3d";
            stringBuilder.StartNode(Id, shape, Success);
            foreach (var service in Services.Keys)
            {
                stringBuilder.AppendServiceRow(service.Description, Services[service]);
            }

            stringBuilder.AppendTableRow(TracerMessages.ComponentDisplay, Component);

            if (DecoratorTarget is object)
            {
                stringBuilder.AppendTableRow(TracerMessages.TargetDisplay, DecoratorTarget);
            }

            // Only write the instance info IF
            // - There IS an instance AND
            //   - There's more than one service exposed (which means there's at least one service)
            //     not matching the instance type OR
            //   - There's only one service exposed and that one doesn't match the instance type.
            if (Instance is object &&
                (Services.Count != 1 || !(Services.First().Key is IServiceWithType swt) || swt.ServiceType != Instance.GetType()))
            {
                stringBuilder.AppendTableRow(TracerMessages.InstanceDisplay, Instance.GetType().FullName);
            }

            if (Exception is object)
            {
                stringBuilder.AppendTableErrorRow(Exception.GetType().FullName, Exception.Message);
            }

            stringBuilder.EndNode();
            foreach (var edge in Edges)
            {
                // Connect into a table with the ID format "parent:tablerow"
                var destination = allRequests[edge.Request];
                var edgeId = destination.Id.NodeId() + ":" + destination.Services[edge.Service].NodeId();

                // Shorter type name for line descriptions where possible.
                var description = edge.Service is IServiceWithType edgeSwt ? edgeSwt.ServiceType.Name : edge.Service.Description;
                stringBuilder.ConnectNodes(Id.NodeId(), edgeId, description, !destination.Success);
            }
        }
    }
}
