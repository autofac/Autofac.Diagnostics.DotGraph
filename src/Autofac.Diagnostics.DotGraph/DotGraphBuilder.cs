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
    /// Generator for DOT format graph traces. A single trace represents one resolve operation.
    /// </summary>
    internal class DotGraphBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DotGraphBuilder"/> class.
        /// </summary>
        public DotGraphBuilder()
        {
            Operation = new OperationNode();
            Requests = new RequestDictionary();
            CurrentRequest = new Stack<Guid>();
        }

        /// <summary>
        /// Gets the node that has operation-level data for the graph.
        /// </summary>
        public OperationNode Operation { get; private set; }

        /// <summary>
        /// Gets the set of all requests made during the operation.
        /// </summary>
        public RequestDictionary Requests { get; private set; }

        /// <summary>
        /// Gets the originating request ID. This will also be the first request in the
        /// stack of ongoing requests. Tracked to ensure we retain the originating
        /// request during the normalization of the graph.
        /// </summary>
        public Guid OriginatingRequest { get; private set; }

        /// <summary>
        /// Gets the stack of ongoing requests. The first request in the stack is the originating
        /// request where the graph should start.
        /// </summary>
        public Stack<Guid> CurrentRequest { get; private set; }

        /// <summary>
        /// Adds information about the operation available at operation start.
        /// </summary>
        /// <param name="service">The display name of the service being resolved in this operation.</param>
        /// <param name="sequenceNumber">
        /// A <see cref="long"/> indicating a basic ordering of resolve operations to
        /// enable some correlation between a parent operation and child operations that
        /// involve service location.
        /// </param>
        public void OnOperationStart(string? service, long sequenceNumber)
        {
            Operation.Service = service;
            Operation.SequenceNumber = sequenceNumber;
        }

        /// <summary>
        /// Signals the operation is complete and failed. Triggers normalization of the graph
        /// in preparation for rendering.
        /// </summary>
        public void OnOperationFailure()
        {
            Operation.Success = false;
            NormalizeGraph();
        }

        /// <summary>
        /// Signals the operation is complete and succeeded. Triggers normalization of the graph
        /// in preparation for rendering.
        /// </summary>
        public void OnOperationSuccess()
        {
            Operation.Success = true;
            NormalizeGraph();
        }

        /// <summary>
        /// Adds a new resolve request to the chain of requests being made in the graph. Resolve
        /// requests can be nested.
        /// </summary>
        /// <param name="service">
        /// The <see cref="Service"/> being resolved in the operation.
        /// </param>
        /// <param name="component">
        /// The display name of the component that should fulfill the service.
        /// </param>
        /// <param name="decoratorTarget">
        /// If this is a request where the result service is being decorated, this is the thing
        /// that's being decorated in this request. This value will be <see langword="null"/>
        /// if this isn't a decorator operation.
        /// </param>
        public void OnRequestStart(Service service, string component, string? decoratorTarget)
        {
            var request = new ResolveRequestNode(component);
            request.Services.Add(service, Guid.NewGuid());
            Requests.Add(request);
            if (decoratorTarget is not null)
            {
                request.DecoratorTarget = decoratorTarget;
            }

            if (CurrentRequest.Count != 0)
            {
                // We're already in a request, so add an edge from
                // the parent to this new request/service.
                var parent = Requests[CurrentRequest.Peek()];
                parent.Edges.Add(new GraphEdge(request.Id, service));
            }
            else
            {
                // The initiating request will be the first request we see.
                OriginatingRequest = request.Id;
            }

            // The inbound request is the new current.
            CurrentRequest.Push(request.Id);
        }

        /// <summary>
        /// Signals that the current resolve request is complete and ended in failure.
        /// </summary>
        /// <param name="requestException">
        /// The <see cref="Exception"/> with details about the request failure.
        /// </param>
        public void OnRequestFailure(Exception? requestException)
        {
            if (CurrentRequest.Count == 0)
            {
                // OnRequestFailure happened without a corresponding OnRequestStart.
                return;
            }

            var request = Requests[CurrentRequest.Pop()];
            request.Success = false;
            request.Exception = requestException;
        }

        /// <summary>
        /// Signals that the current resolve request is complete and ended in success.
        /// </summary>
        /// <param name="instance">
        /// The <see cref="object"/> that was resolved based on this request.
        /// </param>
        public void OnRequestSuccess(object? instance)
        {
            if (CurrentRequest.Count == 0)
            {
                // OnRequestSuccess happened without a corresponding OnRequestStart.
                return;
            }

            var request = Requests[CurrentRequest.Pop()];
            request.Success = true;
            request.Instance = instance;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.AppendLine("digraph G {");
            Operation.ToString(builder);
            foreach (var request in Requests)
            {
                request.ToString(builder, Requests);
            }

            builder.AppendLine("}");
            return builder.ToString();
        }

        private void NormalizeGraph()
        {
            // Remove any duplicates of the root node. We need to make sure that node in particular stays.
            RemoveDuplicates(OriginatingRequest);

            // Other than the originating request, find the rest of the distinct values
            // so we can de-dupe.
            var unique = Requests
                .Where(r => r.Success && r.Instance is not null && r.Id != OriginatingRequest)
                .Distinct(NodeEqualityComparer.Default)
                .Select(r => r.Id)
                .ToArray();

            foreach (var id in unique)
            {
                RemoveDuplicates(id);
            }
        }

        private void RemoveDuplicates(Guid sourceId)
        {
            if (!Requests.Contains(sourceId))
            {
                // Should always find this value, but in testing
                // sometimes the mock value uses an empty GUID or
                // something that might not be here. Also, it appears
                // TryGetValue on KeyedCollection<K,V> wasn't added
                // until netstandard2.1, so it causes compiler problems
                // to multitarget netstandard2.0.
                return;
            }

            var source = Requests[sourceId];
            if (!source.Success || source.Instance is null)
            {
                // We can only de-duplicate successful operations because
                // failed operations don't have instances to compare.
                return;
            }

            var duplicates = Requests.Where(dup =>

                // Successful requests where IDs are different
                dup.Id != sourceId && dup.Success &&

                // Instance is exactly the same
                dup.Instance is not null && ReferenceEquals(dup.Instance, source.Instance) &&

                // Decorator target must also be the same (otherwise we lose the instance/decorator relationship)
                dup.DecoratorTarget == source.DecoratorTarget).ToArray();

            if (duplicates.Length == 0)
            {
                // No duplicates.
                return;
            }

            foreach (var duplicate in duplicates)
            {
                Requests.Remove(duplicate.Id);
                foreach (var request in Requests)
                {
                    var duplicateEdges = request.Edges.Where(e => e.Request == duplicate.Id).ToArray();
                    foreach (var duplicateEdge in duplicateEdges)
                    {
                        // Replace edges pointing to the duplicate so they
                        // point at the new source. HashSet will only keep
                        // unique edges, so if there was already a link to
                        // the source, there won't be duplicate edges.
                        // Also, duplicateEdge will never be null but the
                        // analyzer thinks it could be in that GraphEdge.ctor
                        // call.
                        request.Edges.Remove(duplicateEdge);
                        request.Edges.Add(new GraphEdge(sourceId, duplicateEdge!.Service));
                        if (!source.Services.ContainsKey(duplicateEdge.Service))
                        {
                            source.Services.Add(duplicateEdge.Service, Guid.NewGuid());
                        }
                    }
                }
            }
        }
    }
}
