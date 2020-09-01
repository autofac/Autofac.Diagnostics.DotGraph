// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Xunit;
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

        public interface IService1
        {
        }

        public interface IService2
        {
        }

        public interface IService3
        {
        }

        public interface IHandler<T>
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
            Assert.Contains("labelloc=t", result);
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
            Assert.Contains("label=<Autofac.Diagnostics.DotGraph.Test.ComplexGraphTests.IHandler&lt;string&gt;", result);

            // No raw type names.
            Assert.DoesNotContain("`1", result);
            Assert.DoesNotContain("Culture=neutral", result);
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

        public class Component1 : IService1
        {
            public Component1(IService2 service2, IService3 service3)
            {
                Service2 = service2 ?? throw new ArgumentNullException(nameof(service2));
                Service3 = service3 ?? throw new ArgumentNullException(nameof(service3));
            }

            public IService2 Service2 { get; }

            public IService3 Service3 { get; }
        }

        public class Component2 : IService2
        {
            public Component2(IService3 service3)
            {
                Service3 = service3 ?? throw new ArgumentNullException(nameof(service3));
            }

            public IService3 Service3 { get; }
        }

        public class Component3 : IService3
        {
        }

        public class Component3Decorator : IService3
        {
            public Component3Decorator(IService3 decorated)
            {
                Decorated = decorated ?? throw new ArgumentNullException(nameof(decorated));
            }

            public IService3 Decorated { get; }
        }

        public class Handler<T> : IHandler<T>
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
