// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Xunit.Abstractions;

namespace Autofac.Diagnostics.DotGraph.Test
{
    public class ComplexGraphTests
    {
        private readonly ITestOutputHelper _output;

        public ComplexGraphTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private interface IService1
        {
        }

        private interface IService2
        {
        }

        private interface IService3
        {
        }

        private interface IHandler<T>
        {
        }

        [Fact]
        public void GeneratesComplexGraph()
        {
            using var container = BuildGraphContainer();

            var tracer = new DotDiagnosticTracer();
            string result = null;
            tracer.OperationCompleted += (sender, args) =>
            {
                result = args.TraceContent;
                _output.WriteLine(result);
            };
            container.SubscribeToDiagnostics(tracer);

            using var scope = container.BeginLifetimeScope();
            scope.Resolve<IHandler<string>>();

            Assert.NotNull(result);
        }

        [Fact]
        public void LabelPosition()
        {
            using var container = BuildGraphContainer();

            var tracer = new DotDiagnosticTracer();
            string result = null;
            tracer.OperationCompleted += (sender, args) =>
            {
                result = args.TraceContent;
            };
            container.SubscribeToDiagnostics(tracer);

            using var scope = container.BeginLifetimeScope();
            scope.Resolve<IHandler<string>>();

            // Label should be at the top.
            Assert.Contains("labelloc=t", result, StringComparison.Ordinal);
        }

        [Fact]
        public void TypeNamesArePrettyPrinted()
        {
            using var container = BuildGraphContainer();

            var tracer = new DotDiagnosticTracer();
            string result = null;
            tracer.OperationCompleted += (sender, args) =>
            {
                result = args.TraceContent;
            };
            container.SubscribeToDiagnostics(tracer);

            using var scope = container.BeginLifetimeScope();
            scope.Resolve<IHandler<string>>();

            // Label should be pretty-printed.
            Assert.Contains("label=<Autofac.Diagnostics.DotGraph.Test.ComplexGraphTests.IHandler&lt;string&gt;", result, StringComparison.Ordinal);

            // No raw type names.
            Assert.DoesNotContain("`1", result, StringComparison.Ordinal);
            Assert.DoesNotContain("Culture=neutral", result, StringComparison.Ordinal);
        }

        private static IContainer BuildGraphContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterGeneric(typeof(Handler<>)).As(typeof(IHandler<>));
            builder.RegisterType<Component1>().As<IService1>();
            builder.RegisterType<Component2>().As<IService2>().SingleInstance();
            builder.Register(ctx => new Component3()).As<IService3>();
            builder.RegisterDecorator<Component3Decorator, IService3>();
            return builder.Build();
        }

        [SuppressMessage("CA1812", "CA1812", Justification = "Instantiated via reflection.")]
        private class Component1 : IService1
        {
            public Component1(IService2 service2, IService3 service3)
            {
                Service2 = service2 ?? throw new ArgumentNullException(nameof(service2));
                Service3 = service3 ?? throw new ArgumentNullException(nameof(service3));
            }

            public IService2 Service2 { get; }

            public IService3 Service3 { get; }
        }

        [SuppressMessage("CA1812", "CA1812", Justification = "Instantiated via reflection.")]
        private class Component2 : IService2
        {
            public Component2(IService3 service3)
            {
                Service3 = service3 ?? throw new ArgumentNullException(nameof(service3));
            }

            public IService3 Service3 { get; }
        }

        private class Component3 : IService3
        {
        }

        [SuppressMessage("CA1812", "CA1812", Justification = "Instantiated via reflection.")]
        private class Component3Decorator : IService3
        {
            public Component3Decorator(IService3 decorated, ILifetimeScope scope)
            {
                Decorated = decorated ?? throw new ArgumentNullException(nameof(decorated));
                Scope = scope ?? throw new ArgumentNullException(nameof(scope));
            }

            public IService3 Decorated { get; }

            public ILifetimeScope Scope { get; }
        }

        [SuppressMessage("CA1812", "CA1812", Justification = "Instantiated via reflection.")]
        private class Handler<T> : IHandler<T>
        {
            public Handler(IService1 service1, IService2 service2)
            {
                Service1 = service1 ?? throw new ArgumentNullException(nameof(service1));
                Service2 = service2 ?? throw new ArgumentNullException(nameof(service2));
            }

            public IService1 Service1 { get; }

            public IService2 Service2 { get; }
        }
    }
}
