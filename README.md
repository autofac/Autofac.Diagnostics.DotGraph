# Autofac.Diagnostics.DotGraph

Autofac diagnostics support to enable DOT graph visualization of resolve requests.

[![Build status](https://ci.appveyor.com/api/projects/status/asqnmv0qa7m43oy0/branch/develop?svg=true)](https://ci.appveyor.com/project/Autofac/autofac-diagnostics-dotgraph/branch/develop)

Please file issues and pull requests for this package in this repository rather than in the Autofac core repo.

- [Documentation](https://autofac.readthedocs.io/)
- [NuGet](https://www.nuget.org/packages/Autofac.Diagnostics.DotGraph)
- [Contributing](https://autofac.readthedocs.io/en/latest/contributors.html)

## Getting Started

After building your container, attach the `Autofac.Diagnostics.DotGraph.DotDiagnosticTracer` to the container. When every resolve operation completes (success or failure) you'll get a trace. It's up to you to determine what to do with that trace - write it to a file, render it to an image, etc.

```c#
// Build a container with some registrations.
var containerBuilder = new ContainerBuilder();
containerBuilder.Register(ctx => "Hello");
var container = containerBuilder.Build();

// Attach a DotDiagnosticTracer to the container.
// Handle the OperationCompleted event to deal
// with the trace output.
var tracer = new DotDiagnosticTracer();
tracer.OperationCompleted += (sender, args) =>
{
  using var file = File.OpenWrite(Guid.NewGuid().ToString() + ".dot");
  using var writer = new StreamWriter(file);
  writer.WriteLine(args.TraceContent);
};
container.SubscribeToDiagnostics(tracer);

// Resolve some things and look at the graphs!
// You can use graphviz to render a PNG like:
// dot -T png -O filename.dot
using var scope = container.BeginLifetimeScope();
scope.Resolve<string>();
```

> **Tracing graphs is expensive!** Getting a graph trace is convenient but does have a performance and memory/resource impact. It's recommended you only enable this in a development/troubleshooting situation.

## Get Help

**Need help with Autofac?** We have [a documentation site](https://autofac.readthedocs.io/) as well as [API documentation](https://autofac.org/apidoc/). We're ready to answer your questions on [Stack Overflow](https://stackoverflow.com/questions/tagged/autofac) or check out the [discussion forum](https://groups.google.com/forum/#forum/autofac).

## Note on Building From Source

If you check this repo out or fork it, be sure to do a build _before_ opening it up in your IDE - a quick `dotnet build` should do it. [Due to issues with strongly-typed RESX class generation](https://github.com/dotnet/msbuild/issues/4751) the `.Designer.cs` file for the resources is not checked in and needs to be generated during a build.
