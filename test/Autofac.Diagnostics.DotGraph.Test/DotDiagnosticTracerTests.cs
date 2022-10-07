// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Core;
using Autofac.Core.Resolving;
using Autofac.Core.Resolving.Pipeline;

namespace Autofac.Diagnostics.DotGraph.Test;

public class DotDiagnosticTracerTests
{
    private interface IService
    {
    }

    [Fact]
    public void DiagnosticTracerRaisesEventsOnSuccess()
    {
        var containerBuilder = new ContainerBuilder();
        containerBuilder.Register(ctxt => "Hello");
        var container = containerBuilder.Build();

        var tracer = new DotDiagnosticTracer();
        container.SubscribeToDiagnostics(tracer);
        string lastOpResult = null;
        tracer.OperationCompleted += (sender, args) =>
        {
            Assert.Same(tracer, sender);
            lastOpResult = args.TraceContent;
        };

        container.Resolve<string>();

        Assert.Contains("λ:string", lastOpResult, StringComparison.Ordinal);
        Assert.StartsWith("digraph G {", lastOpResult, StringComparison.Ordinal);
        Assert.EndsWith("}", lastOpResult.Trim(), StringComparison.Ordinal);
    }

    [Fact]
    public void DiagnosticTracerRaisesEventsOnError()
    {
        var containerBuilder = new ContainerBuilder();
        containerBuilder.Register<string>(ctxt => throw new InvalidOperationException());
        var container = containerBuilder.Build();

        var tracer = new DotDiagnosticTracer();
        container.SubscribeToDiagnostics(tracer);
        string lastOpResult = null;
        tracer.OperationCompleted += (sender, args) =>
        {
            Assert.Same(tracer, sender);
            lastOpResult = args.TraceContent;
        };

        Assert.Throws<DependencyResolutionException>(() => container.Resolve<string>());
        Assert.Contains(nameof(InvalidOperationException), lastOpResult, StringComparison.Ordinal);
        Assert.StartsWith("digraph G {", lastOpResult, StringComparison.Ordinal);
        Assert.EndsWith("}", lastOpResult.Trim(), StringComparison.Ordinal);
    }

    [Fact]
    public void DiagnosticTracerHandlesDecorators()
    {
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterType<Implementor>().As<IService>();
        containerBuilder.RegisterDecorator<Decorator, IService>();
        var container = containerBuilder.Build();

        var tracer = new DotDiagnosticTracer();
        container.SubscribeToDiagnostics(tracer);
        string lastOpResult = null;
        tracer.OperationCompleted += (sender, args) =>
        {
            Assert.Same(tracer, sender);
            lastOpResult = args.TraceContent;
        };

        container.Resolve<IService>();

        Assert.Contains("Decorator", lastOpResult, StringComparison.Ordinal);
    }

    [Fact]
    public void DiagnosticTracerDoesNotLeakMemory()
    {
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterType<Implementor>().As<IService>();
        containerBuilder.RegisterDecorator<Decorator, IService>();
        var container = containerBuilder.Build();

        var tracer = new DotDiagnosticTracer();
        container.SubscribeToDiagnostics(tracer);
        container.Resolve<IService>();

        // The dictionary of tracked operations and
        // graphs should be empty.
        Assert.Equal(0, tracer.OperationsInProgress);
    }

    [Fact]
    public void OnOperationStart_AddsOperation()
    {
        var tracer = new TestTracer();
        tracer.TestWrite(DiagnosticEventKeys.OperationStart, new OperationStartDiagnosticData(MockResolveOperation(), MockResolveRequest()));
        Assert.Equal(1, tracer.OperationsInProgress);
    }

    [Fact]
    public void OnOperationStart_NoDataSkipsOperation()
    {
        var tracer = new TestTracer();
        tracer.TestWrite(DiagnosticEventKeys.OperationStart, null);
        Assert.Equal(0, tracer.OperationsInProgress);
    }

    [Fact]
    public void OnOperationSuccess_CompletesOperation()
    {
        var tracer = new TestTracer();
        var called = false;
        tracer.OperationCompleted += (sender, args) =>
        {
            called = true;
        };
        var op = MockResolveOperation();
        tracer.TestWrite(DiagnosticEventKeys.OperationStart, new OperationStartDiagnosticData(op, MockResolveRequest()));
        tracer.TestWrite(DiagnosticEventKeys.OperationSuccess, new OperationSuccessDiagnosticData(op, "instance"));
        Assert.Equal(0, tracer.OperationsInProgress);
        Assert.True(called);
    }

    [Fact]
    public void OnOperationSuccess_NoDataSkipsOperation()
    {
        var tracer = new TestTracer();
        var op = MockResolveOperation();
        var called = false;
        tracer.OperationCompleted += (sender, args) =>
        {
            called = true;
        };

        tracer.TestWrite(DiagnosticEventKeys.OperationStart, new OperationStartDiagnosticData(op, MockResolveRequest()));
        tracer.TestWrite(DiagnosticEventKeys.OperationSuccess, null);
        Assert.Equal(1, tracer.OperationsInProgress);
        Assert.False(called);
    }

    [Fact]
    public void OnOperationFailure_CompletesOperation()
    {
        var tracer = new TestTracer();
        var called = false;
        tracer.OperationCompleted += (sender, args) =>
        {
            called = true;
        };
        var op = MockResolveOperation();
        tracer.TestWrite(DiagnosticEventKeys.OperationStart, new OperationStartDiagnosticData(op, MockResolveRequest()));
        tracer.TestWrite(DiagnosticEventKeys.OperationFailure, new OperationFailureDiagnosticData(op, new DivideByZeroException()));
        Assert.Equal(0, tracer.OperationsInProgress);
        Assert.True(called);
    }

    [Fact]
    public void OnOperationFailure_NoDataSkipsOperation()
    {
        var tracer = new TestTracer();
        var op = MockResolveOperation();
        var called = false;
        tracer.OperationCompleted += (sender, args) =>
        {
            called = true;
        };

        tracer.TestWrite(DiagnosticEventKeys.OperationStart, new OperationStartDiagnosticData(op, MockResolveRequest()));
        tracer.TestWrite(DiagnosticEventKeys.OperationFailure, null);
        Assert.Equal(1, tracer.OperationsInProgress);
        Assert.False(called);
    }

    [Fact]
    public void OnRequestStart_NoOperation()
    {
        var tracer = new TestTracer();
        tracer.TestWrite(DiagnosticEventKeys.RequestStart, new RequestDiagnosticData(MockResolveOperation(), MockResolveRequestContext()));
        Assert.Equal(0, tracer.OperationsInProgress);
    }

    [Fact]
    public void OnRequestSuccess_NoStartOperation()
    {
        var tracer = new TestTracer();
        var op = MockResolveOperation();
        tracer.TestWrite(DiagnosticEventKeys.OperationStart, new OperationStartDiagnosticData(op, MockResolveRequest()));

        // Should have a request start before ending, but make sure we don't
        // explode if something weird happens.
        tracer.TestWrite(DiagnosticEventKeys.RequestSuccess, new RequestDiagnosticData(op, MockResolveRequestContext()));
    }

    [Fact]
    public void OnRequestFailure_NoStartOperation()
    {
        var tracer = new TestTracer();
        var op = MockResolveOperation();
        tracer.TestWrite(DiagnosticEventKeys.OperationStart, new OperationStartDiagnosticData(op, MockResolveRequest()));

        // Should have a request start before ending, but make sure we don't
        // explode if something weird happens.
        tracer.TestWrite(DiagnosticEventKeys.RequestFailure, new RequestFailureDiagnosticData(op, MockResolveRequestContext(), new DivideByZeroException()));
    }

    private static IResolveOperation MockResolveOperation()
    {
        return Mock.Of<IResolveOperation>();
    }

    private static ResolveRequest MockResolveRequest()
    {
        return new ResolveRequest(
            new TypedService(typeof(string)),
            new ServiceRegistration(Mock.Of<IResolvePipeline>(), Mock.Of<IComponentRegistration>()),
            Enumerable.Empty<Parameter>());
    }

    private static ResolveRequestContext MockResolveRequestContext()
    {
        var service = new TypedService(typeof(string));
        var activator = Mock.Of<IInstanceActivator>(act => act.LimitType == typeof(string));
        var registration = Mock.Of<IComponentRegistration>(reg => reg.Activator == activator);
        return Mock.Of<ResolveRequestContext>(
            ctx =>
                ctx.Service == service &&
                ctx.Registration == registration);
    }

    private class TestTracer : DotDiagnosticTracer
    {
        public void TestWrite(string diagnosticName, object data)
        {
            Write(diagnosticName, data);
        }
    }

    [SuppressMessage("CA1812", "CA1812", Justification = "Instantiated via reflection.")]
    private class Decorator : IService
    {
        public Decorator(IService decorated)
        {
            Decorated = decorated;
        }

        public IService Decorated { get; }
    }

    [SuppressMessage("CA1812", "CA1812", Justification = "Instantiated via reflection.")]
    private class Implementor : IService
    {
    }
}
