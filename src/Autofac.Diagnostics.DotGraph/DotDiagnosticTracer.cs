// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using Autofac.Core;
using Autofac.Core.Resolving;

namespace Autofac.Diagnostics.DotGraph;

/// <summary>
/// Provides a resolve pipeline tracer that generates DOT graph output
/// traces for an end-to-end operation flow. Attach to the
/// <see cref="OperationDiagnosticTracerBase{TContent}.OperationCompleted"/>
/// event to receive notifications when a new graph is available.
/// </summary>
/// <remarks>
/// <para>
/// The tracer subscribes to all Autofac diagnostic events and can't be
/// unsubscribed. This is required to ensure beginning and end of each
/// logical activity can be captured.
/// </para>
/// </remarks>
public class DotDiagnosticTracer : OperationDiagnosticTracerBase<string>
{
    /// <summary>
    /// Metadata flag to help deduplicate the number of places where the exception is traced.
    /// </summary>
    private const string RequestExceptionTraced = "__RequestException";

    private static readonly string[] DotEvents = new string[]
    {
        DiagnosticEventKeys.OperationStart,
        DiagnosticEventKeys.OperationFailure,
        DiagnosticEventKeys.OperationSuccess,
        DiagnosticEventKeys.RequestStart,
        DiagnosticEventKeys.RequestFailure,
        DiagnosticEventKeys.RequestSuccess,
    };

    private readonly ConcurrentDictionary<IResolveOperation, DotGraphBuilder> _operationBuilders = new();

    // "Global" sequence number for graph ordering. Increment is done
    // with Interlocked.Increment, which is both thread-safe and handles
    // overflow by wrapping from Int64.MaxValue to Int64.MinValue as needed.
    private long _sequenceNumber;

    /// <summary>
    /// Initializes a new instance of the <see cref="DotDiagnosticTracer"/> class.
    /// </summary>
    public DotDiagnosticTracer()
        : base(DotEvents)
    {
    }

    /// <summary>
    /// Gets the number of operations in progress being traced.
    /// </summary>
    /// <value>
    /// An <see cref="int"/> with the number of trace IDs associated
    /// with in-progress operations being traced by this tracer.
    /// </value>
    public override int OperationsInProgress => _operationBuilders.Count;

    /// <inheritdoc/>
    protected override void OnOperationStart(OperationStartDiagnosticData data)
    {
        if (data is null)
        {
            return;
        }

        var builder = _operationBuilders.GetOrAdd(data.Operation, k => new DotGraphBuilder());
        builder.OnOperationStart(data.Operation.InitiatingRequest?.Service.GraphDisplayName(), Interlocked.Increment(ref _sequenceNumber));
    }

    /// <inheritdoc/>
    protected override void OnOperationSuccess(OperationSuccessDiagnosticData data)
    {
        if (data is null)
        {
            return;
        }

        if (_operationBuilders.TryGetValue(data.Operation, out var builder))
        {
            try
            {
                builder.OnOperationSuccess();
                OnOperationCompleted(new OperationTraceCompletedArgs<string>(data.Operation, true, builder.ToString()));
            }
            finally
            {
                _operationBuilders.TryRemove(data.Operation, out var _);
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnOperationFailure(OperationFailureDiagnosticData data)
    {
        if (data is null)
        {
            return;
        }

        if (_operationBuilders.TryGetValue(data.Operation, out var builder))
        {
            try
            {
                builder.OnOperationFailure();
                OnOperationCompleted(new OperationTraceCompletedArgs<string>(data.Operation, false, builder.ToString()));
            }
            finally
            {
                _operationBuilders.TryRemove(data.Operation, out var _);
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnRequestStart(RequestDiagnosticData data)
    {
        if (data is null)
        {
            return;
        }

        if (_operationBuilders.TryGetValue(data.Operation, out var builder))
        {
            builder.OnRequestStart(
                data.RequestContext.Service,
                data.RequestContext.Registration.Activator.DisplayName(),
                data.RequestContext.DecoratorTarget?.Activator.DisplayName());
        }
    }

    /// <inheritdoc/>
    protected override void OnRequestSuccess(RequestDiagnosticData data)
    {
        if (data is null)
        {
            return;
        }

        if (_operationBuilders.TryGetValue(data.Operation, out var builder))
        {
            builder.OnRequestSuccess(data.RequestContext.Instance);
        }
    }

    /// <inheritdoc/>
    protected override void OnRequestFailure(RequestFailureDiagnosticData data)
    {
        if (data is null)
        {
            return;
        }

        if (_operationBuilders.TryGetValue(data.Operation, out var builder))
        {
            var requestException = data.RequestException;
            if (requestException is DependencyResolutionException && requestException.InnerException is not null)
            {
                requestException = requestException.InnerException;
            }

            if (requestException.Data.Contains(RequestExceptionTraced))
            {
                builder.OnRequestFailure(null);
            }
            else
            {
                builder.OnRequestFailure(requestException);
            }

            requestException.Data[RequestExceptionTraced] = true;
        }
    }
}
