// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// #define ISRUNREVERSEORDER
// #define ISONECASE

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Autofac.Diagnostics.DotGraph;
using Microsoft.AspNetCore.Builder;

namespace Autofac.Extensions.DependencyInjection;

[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1316:Tuple element names should use correct casing", Justification = "naming preference")]
[SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1124:Do not use regions", Justification = "prefer region organization")]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1312:Variable names should begin with lower-case letter", Justification = "quasi-constants")]
[SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1503:Braces should not be omitted", Justification = "preference")]
[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "preference")]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "cannot resolve")]
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1629:Documentation text should end with a period", Justification = "linquistics")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1214:Readonly fields should appear before non-readonly fields", Justification = "member order preference")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1203:Constants should appear before fields", Justification = "member order preference")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "member order preference")]

#pragma warning disable CS1587 // XML comment is not placed on a valid language element
/// <summary>
/// Extension
///  </summary>
/// <remarks>Dontrell Bluford 20230810: Created
///     </remarks>
#pragma warning restore CS1587 // XML comment is not placed on a valid language element
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public static class AutofacDiagnosticExtensions
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
{
    #region Features

    /// <summary>
    ///   Use Autofac.Diagnostics.DotGraph to generate a DOT diagram of the container's traces. </summary>
    /// <param name="app">The application builder.</param>
    /// <exception cref="ArgumentNullException">Missing.</exception>
    /// <remarks>
    ///   Only compiles during Debug.</remarks>
    /// <remarks>Dontrell Bluford 20230810: Created
    ///     </remarks>
    public static void UseAutoFacDOTDiagnostics(this IApplicationBuilder app)
    {
        if (app is null)
        {
            throw new ArgumentNullException(nameof(app));
        }

#if DEBUG
        // bool permit = true;

        // Attach a DotDiagnosticTracer to the container.
        // Handle the OperationCompleted event to deal
        // with the trace output.
        var tracer = new DotDiagnosticTracer();
        tracer.OperationCompleted += async (sender, args) =>
        {
            // Define the operation title names.
            string[] operationNames =
            {
                "Operation: Task-based",
                "Operation: EnumTask-based",
            };

            long nanosecPerTick = (1000L * 1000L * 1000L) / Stopwatch.Frequency;

            int DISCREETCASE = 0;
            int COLLECTIONCASE = 1;
            int COUNT = 1;

#if ISRUNREVERSEORDER
            DISCREETCASE = 1;
            COLLECTIONCASE = 0;
            operationNames = operationNames.Reverse().ToArray();
#endif
#if ISONECASE
            COUNT = 0;
#endif

            // https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.stopwatch?view=net-7.0#examples
            for (int operation = 0; operation <= COUNT; operation++)
            {
                Stopwatch timePer = Stopwatch.StartNew();
                TimeSpan elapsed = TimeSpan.Zero;

                // Gather current object hierarchies
                if (operation == DISCREETCASE)
                {
                    var dotFI = new FileInfo(_dotName(Guid.NewGuid()));
                    _ = await dotFI
                            .CleanCodeAndImageFolders()
                            .WriteDOTCode(args.TraceContent)
                            .GetImageFileFullPath(args.TraceContent)
                            .WriteDOTDiagram()
                            .ConfigureAwait(false);

                    timePer.Stop();
                    Interlocked.Increment(ref _taskCount);
                    elapsed = timePer.Elapsed;
                    Interlocked.Add(ref _taskTotalElapsedTicks, elapsed.Ticks);
                }
                else if (operation == COLLECTIONCASE)
                {
                    int i = 0;
                    var dotFIenum = new FileInfo(_dotName(Guid.NewGuid()));
                    await foreach (var (dotFI, diagFI) in dotFIenum
                      .CleanCodeAndImageFoldersEnum()
                      .WriteDOTCodeEnum(args.TraceContent)
                      .GetImageFileFullPathEnum(args.TraceContent)
                      .WriteDOTDiagramEnum().ConfigureAwait(false))
                        i += 0; // dummy work-around to empty statement

                    timePer.Stop();
                    Interlocked.Increment(ref _enumCount);
                    elapsed = timePer.Elapsed;
                    Interlocked.Add(ref _enumTotalElapsedTicks, elapsed.Ticks);
                }

                // Display the statistics.
                if (operation == DISCREETCASE)
                {
                    var ticks = Volatile.Read(ref _taskTotalElapsedTicks);
                    var seconds = TimeSpan.FromSeconds(ticks / Stopwatch.Frequency);
                    var cnt = Volatile.Read(ref _taskCount);

                    Debug.WriteLine(string.Empty);
                    Debug.WriteLine("{0} Summary:", (object)operationNames[operation]);
                    Debug.WriteLine(
                        "  Discrete Time time:  {0} seconds = {1} milliseconds",
                        elapsed.TotalSeconds,
                        elapsed.TotalMilliseconds);

                    Debug.WriteLine(
                        "  Running Time time:  {0} total seconds vs {1} avg seconds",
                        seconds.TotalSeconds,
                        seconds.TotalSeconds / cnt);
                }
                else if (operation == COLLECTIONCASE)
                {
                    var ticksEnum = Volatile.Read(ref _enumTotalElapsedTicks);
                    var secondsEnum = TimeSpan.FromSeconds(ticksEnum / Stopwatch.Frequency);
                    var cntEnum = Volatile.Read(ref _enumCount);

                    Debug.WriteLine(string.Empty);
                    Debug.WriteLine("{0} Summary:", (object)operationNames[operation]);
                    Debug.WriteLine(
                        "  Discrete Time time:  {0} seconds = {1} milliseconds",
                        elapsed.TotalSeconds,
                        elapsed.TotalMilliseconds);

                    Debug.WriteLine(
                        "  Running Time time:  {0} total seconds vs {1} avg seconds",
                        secondsEnum.TotalSeconds,
                        secondsEnum.TotalSeconds / cntEnum);
                }
            }
        };

        // Track trace to gather future object hierarchies
        IContainer? container = app.ApplicationServices.GetAutofacRoot() as IContainer;
        container?.SubscribeToDiagnostics(tracer);

#endif
    }

    /// <summary>
    /// IsPathValid (may not exist)
    /// </summary>
    /// <param name="file">file information</param>
    /// <returns>Whether path is valid.</returns>
    /// <exception cref="ArgumentNullException">Missing file information.</exception>
    /// <remarks>Dontrell Bluford 20230810: Created
    ///     </remarks>
    public static bool IsPathValid(this FileInfo file)
    {
        if (file is null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        return Path.IsPathRooted(file.FullName) && Directory.Exists(Path.GetDirectoryName(file.FullName));
    }

    #endregion Features

    #region Feature Helpers

    private static async Task WriteDOTDiagramFor(string cmdGenDiagram)
    {
        if (cmdGenDiagram is null)
        {
            throw new ArgumentNullException(nameof(cmdGenDiagram));
        }

        // _startInfoDOT.Arguments = cmdGenDiagram;
        // _processDOT.Start();    //System.Diagnostics.Process.Start(startInfo);
        await _processDebug(_startInfoDOT(cmdGenDiagram)).ConfigureAwait(false);
    }

    #region Task-Based Helpers

    private static async Task<(FileInfo dotFI, FileInfo diagFI)> WriteDOTDiagram(this Task<(FileInfo dotFI, FileInfo diagFI)> filesTask)
    {
        if (filesTask is null)
        {
            throw new ArgumentNullException(nameof(filesTask));
        }

        // https://devblogs.microsoft.com/dotnet/configureawait-faq/
        var files = await filesTask.ConfigureAwait(false);

        if (!files.dotFI.Exists)
        {
            throw new ArgumentException($"File '{nameof(files.dotFI.FullName)}' does not exist to process.", nameof(filesTask));
        }

        if (!files.diagFI.IsPathValid())
        {
            throw new ArgumentException($"File '{nameof(files.diagFI.FullName)}' is not a valid path (e.g. c:\\seg1\\seg2\\...\\segN\\name.ext, please correct.", nameof(filesTask));
        }

        // string cmdGenDiagram = $@"/c dot -Tpng ""{dotName}"" -o""{diagFI.Name}"" & pause";
        // var cmdGenDiagram = "forfiles /m \"*.dot\" /c \"cmd /c dot -Tpng @file -o@fname.png\"";       https://stackoverflow.com/questions/48579136/graphviz-multiple-files-format-conversion
        // string cmdGenDiagram = $"-Tpng \"{files.dotFI.FullName}\" -o\"{files.diagFI.FullName}\"";
        // WriteDOTDiagramFor(cmdGenDiagram);
        await WriteDOTDiagramFor(_cmdGenDiagram(files)).ConfigureAwait(false);

        return files;
    }

    private static async Task<(FileInfo dotFI, FileInfo diagFI)> GetImageFileFullPath(this Task<FileInfo> dotFI_Task, string operationTraceContent)
    {
        if (dotFI_Task is null)
        {
            throw new ArgumentNullException(nameof(dotFI_Task));
        }

        var dotFI = await dotFI_Task.ConfigureAwait(false);
        if (!dotFI.Exists)
        {
            throw new ArgumentException($"File '{nameof(dotFI.FullName)}' does not exist to process.", nameof(dotFI_Task));
        }

        if (string.IsNullOrEmpty(operationTraceContent))
        {
            throw new ArgumentException($"'{nameof(operationTraceContent)}' cannot be null or empty.", nameof(operationTraceContent));
        }

        // var match = Regex.Match(operationTraceContent, _diagNamePattern); // https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expressions
        // string diagName = match.Groups[1].Value;
        // Debug.Print(diagName);
        // diagName = $"{Path.Combine(Path.GetFullPath(@"..\image", dotFI.DirectoryName!), diagName ?? dotFI.Name)}.png";
        string diagName = _match(operationTraceContent).Groups[1].Value;

        return (dotFI, new FileInfo(_diagName(dotFI, diagName)));
    }

    private static async Task<FileInfo> WriteDOTCode(this Task<FileInfo> dotFI_Task, string operationTraceContent)
    {
        if (dotFI_Task is null)
        {
            throw new ArgumentNullException(nameof(dotFI_Task));
        }

        if (string.IsNullOrEmpty(operationTraceContent))
        {
            throw new ArgumentException($"'{nameof(operationTraceContent)}' cannot be null or empty.", nameof(operationTraceContent));
        }

        var dotFI = await dotFI_Task.ConfigureAwait(false);
        using (var file = dotFI.OpenWrite())
        using (var writer = new StreamWriter(file))
        {
            // writer.WriteLine(operationTraceContent);
            await writer.WriteLineAsync(operationTraceContent).ConfigureAwait(false);
        }

        return dotFI;
    }

    // https://stackoverflow.com/a/9974107 / https://stackoverflow.com/questions/1965787/how-to-delete-files-subfolders-in-a-specific-directory-at-the-command-prompt-in
    private static async Task<FileInfo> CleanCodeAndImageFolders(this FileInfo dotFI) // , ref bool isPermited)
    {
        if (dotFI is null)
        {
            throw new ArgumentNullException(nameof(dotFI));
        }

        // if (!_isPermitted) /// <= daab 20230823 correlation key G: commented if
        // {

        //// daab 20230823 correlation key A
        ////   Supposedly, while #1 is acquiring a lock,
        ////   a few other processes (e.g., #s 3, 4, 5 and 6)
        ////   leak-through here instead of waiting?
        await _semaphoreSlim.WaitAsync().ConfigureAwait(false); //// <= daab 20230823 correlation key A: multiple threads enter here
                                                                //// daab 20230823 correlation key D
                                                                ////   Yup! Double-commenting-out the if block below because is not thread-safe!
                                                                ////
                                                                /////// daab 20230823 correlation key B
                                                                /////// While writing the above comment for correlation-key-A,
                                                                /////// I realized that I can squelch these wayward leaks here,
                                                                /////// closer-to-home, instead of in the else block;
                                                                /////// so adding here and commenting-out the else block below.
                                                                ////if (_semaphoreSlim.CurrentCount > 1)
                                                                ////{
                                                                ////  /// daab 20230823 correlation key B
                                                                ////  ///   Release the spurrious lock acquired by the wayward processes,
                                                                ////  ///   and wait for the lock to be released by the process that acquired it first.
                                                                ////  _semaphoreSlim.Release();
                                                                ////  await _semaphoreSlim.WaitAsync();

        ////  /// daab 20230823 correlation key C
        ////  ///   If any fall-through here, then restore the else block from correlation-key-B :-)
        ////}
        // } /// <= daab 20230823 correlation key G: commented if
        try
        {
            if (_isPermitted)
            {
                // lock(_lock) //// <= daab 20230823 correlation key H/correlation key I: protect shared memory with shared-object-lock; commented for deadock with (apparently?) semaphore
                _isPermitted = false;  //// <= daab 20230823 correlation key F: moved to here from correlation-key-E

                // _startInfoDEL.Arguments = _deletecommand(dotFI.DirectoryName!);
                // processDEL.Start();    //System.Diagnostics.Process.Start(startInfoCMD);
                // processDEL.WaitForExit();

                /////// daab 20230823 correlation key D
                ///////  Added if-block around the awaits to skip-to-finally if the delegated-code block has already ran.
                ///////  Likely overkill because can probably use this or the else block below, but I'm not sure which is better.
                ////if (_isPermitted)
                ////{
                if (await _processDebug(_startInfoDEL(_deletecommand(dotFI.DirectoryName!))).ConfigureAwait(false) is Process p)
                    await p.WaitForExitAsync().ConfigureAwait(false); // await Task.Delay(15000); Debug.Print("CLEANED DIRECTORIES");
                                                                      ////}
            }

            //// daab 20230823 correlation key D
            ////   Yup! Un-commenting-out correlation-key-B for the else block below because is proven(?) thread-safe!
            //// daab 20230823 correlation key B
            ////   Commented in-favor of the squelch above.
            // else
            //  /// daab 20230823 correlation key A
            //  ///   I missed this else-block the first twenty debugging-hours (save yourself!),
            //  ///   and it was causing the deadlock that stopped a few processes
            //  ///   that-snuck-through? (4 out-of 206, e.g. #s 3, 4, 5 and 6) from completing,
            //  ///   leaving some files not generated, and I memory leak, I suppose.
            //  _semaphoreSlim.Release();
            //  await _semaphoreSlim.WaitAsync();
        }
        finally
        {
            // _isPermitted = false;  /// daab 20230823 correlation key E: commented and moved to correlation-key-F
            // while (true) { if (_semaphoreSlim.Release() == 0) break; }
            _semaphoreSlim.Release();
        }

        return dotFI;
    }

    #endregion Task-Based Helpers

    #region IAysncEnumerable-Based Helpers

    private static async IAsyncEnumerable<(FileInfo dotFI, FileInfo diagFI)> WriteDOTDiagramEnum(this IAsyncEnumerable<(FileInfo dotFI, FileInfo diagFI)> filesTask)
    {
        if (filesTask is null)
        {
            throw new ArgumentNullException(nameof(filesTask));
        }

        // https://devblogs.microsoft.com/dotnet/configureawait-faq/
        // var files = await filesTask; //.ConfigureAwait(false);
        var fileInfos = new ConcurrentBag<(FileInfo dotFI, FileInfo diagFI)>();

        // await foreach (var files in filesTask)
        await Parallel.ForEachAsync(filesTask, async (files, token) =>
        {
            if (files.dotFI.Exists && files.diagFI.IsPathValid())
            {
                fileInfos.Add(files);

                // if (!files.dotFI.Exists)
                // {
                //  throw new ArgumentException($"File '{nameof(files.dotFI.FullName)}' does not exist to process.", nameof(files));
                // }
                // if (!files.diagFI.IsPathValid())
                // {
                //  throw new ArgumentException($"File '{nameof(files.diagFI.FullName)}' is not a valid path (e.g. c:\\seg1\\seg2\\...\\segN\\name.ext, please correct.", nameof(files));
                // }

                // string cmdGenDiagram = $@"/c dot -Tpng ""{dotName}"" -o""{diagFI.Name}"" & pause";
                // var cmdGenDiagram = "forfiles /m \"*.dot\" /c \"cmd /c dot -Tpng @file -o@fname.png\"";       https://stackoverflow.com/questions/48579136/graphviz-multiple-files-format-conversion
                // string cmdGenDiagram = $"-Tpng \"{files.dotFI.FullName}\" -o\"{files.diagFI.FullName}\"";
                // WriteDOTDiagramFor(cmdGenDiagram);
                await WriteDOTDiagramFor(_cmdGenDiagram(files)).ConfigureAwait(false);
            }
        }).ConfigureAwait(false);

        foreach (var x in fileInfos)
            yield return x;

        // await foreach (var files in filesTask)
        // {
        //
        //  if (!(files.dotFI.Exists && files.diagFI.IsPathValid()))
        //    continue;
        //
        //  //if (!files.dotFI.Exists)
        //  //{
        //  //  throw new ArgumentException($"File '{nameof(files.dotFI.FullName)}' does not exist to process.", nameof(files));
        //  //}
        //  //if (!files.diagFI.IsPathValid())
        //  //{
        //  //  throw new ArgumentException($"File '{nameof(files.diagFI.FullName)}' is not a valid path (e.g. c:\\seg1\\seg2\\...\\segN\\name.ext, please correct.", nameof(files));
        //  //}
        //
        //  //string cmdGenDiagram = $@"/c dot -Tpng ""{dotName}"" -o""{diagFI.Name}"" & pause";
        //  //var cmdGenDiagram = "forfiles /m \"*.dot\" /c \"cmd /c dot -Tpng @file -o@fname.png\"";       https://stackoverflow.com/questions/48579136/graphviz-multiple-files-format-conversion
        //  //string cmdGenDiagram = $"-Tpng \"{files.dotFI.FullName}\" -o\"{files.diagFI.FullName}\"";
        //  //WriteDOTDiagramFor(cmdGenDiagram);
        //
        //  await WriteDOTDiagramFor(_cmdGenDiagram(files));
        //
        //  yield return files;
        // }
    }

    private static async IAsyncEnumerable<(FileInfo dotFI, FileInfo diagFI)> GetImageFileFullPathEnum(this IAsyncEnumerable<FileInfo> dotFI_Task, string operationTraceContent)
    {
        if (dotFI_Task is null)
        {
            throw new ArgumentNullException(nameof(dotFI_Task));
        }

        var fileInfos = new ConcurrentBag<(FileInfo dotFI, FileInfo diagFI)>();

        await Parallel.ForEachAsync(dotFI_Task, (dotFI, token) =>
        {
            if (dotFI.Exists && !string.IsNullOrEmpty(operationTraceContent))
            {
                // var dotFI = await dotFI_Task;
                // if (!dotFI.Exists)
                // {
                //  throw new ArgumentException($"File '{nameof(dotFI.FullName)}' does not exist to process.", nameof(dotFI_Task));
                // }
                //
                // if (string.IsNullOrEmpty(operationTraceContent))
                // {
                //  throw new ArgumentException($"'{nameof(operationTraceContent)}' cannot be null or empty.", nameof(operationTraceContent));
                // }
                //
                // var match = Regex.Match(operationTraceContent, _diagNamePattern); // https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expressions
                // string diagName = match.Groups[1].Value;
                // Debug.Print(diagName);
                // diagName = $"{Path.Combine(Path.GetFullPath(@"..\image", dotFI.DirectoryName!), diagName ?? dotFI.Name)}.png";
                // return (dotFI, new FileInfo(diagName + "_enum"));
                string diagName = _match(operationTraceContent).Groups[1].Value;
                fileInfos.Add((dotFI, new FileInfo(diagName + "_enum")));
            }

            return ValueTask.CompletedTask;
        }).ConfigureAwait(false);

        foreach (var x in fileInfos)
            yield return x;

        // await foreach (var dotFI in dotFI_Task)
        // {
        //  if (!(dotFI.Exists && !string.IsNullOrEmpty(operationTraceContent)))
        //    continue;
        //
        //  //var dotFI = await dotFI_Task;
        //  //if (!dotFI.Exists)
        //  //{
        //  //  throw new ArgumentException($"File '{nameof(dotFI.FullName)}' does not exist to process.", nameof(dotFI_Task));
        //  //}
        //
        //  //if (string.IsNullOrEmpty(operationTraceContent))
        //  //{
        //  //  throw new ArgumentException($"'{nameof(operationTraceContent)}' cannot be null or empty.", nameof(operationTraceContent));
        //  //}
        //
        //  //var match = Regex.Match(operationTraceContent, _diagNamePattern); // https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expressions
        //  //string diagName = match.Groups[1].Value;
        //  //Debug.Print(diagName);
        //  //diagName = $"{Path.Combine(Path.GetFullPath(@"..\image", dotFI.DirectoryName!), diagName ?? dotFI.Name)}.png";
        //
        //  string diagName = _match(operationTraceContent).Groups[1].Value;
        //
        //  yield return (dotFI, new FileInfo(diagName + "_enum"));
        // }
    }

    private static async IAsyncEnumerable<FileInfo> WriteDOTCodeEnum(this IAsyncEnumerable<FileInfo> dotFI_Task, string operationTraceContent)
    {
        if (dotFI_Task is null)
        {
            throw new ArgumentNullException(nameof(dotFI_Task));
        }

        if (string.IsNullOrEmpty(operationTraceContent))
        {
            throw new ArgumentException($"'{nameof(operationTraceContent)}' cannot be null or empty.", nameof(operationTraceContent));
        }

        var fileInfos = new ConcurrentBag<FileInfo>();

        // await foreach (var dotFI in dotFI_Task)
        await Parallel.ForEachAsync(dotFI_Task, async (dotFI, token) =>
        {
            fileInfos.Add(dotFI);

            // var dotFI = await dotFI_Task;
            using (var file = dotFI.OpenWrite())
            using (var writer = new StreamWriter(file))
            {
                // writer.WriteLine(operationTraceContent);
                await writer.WriteLineAsync(operationTraceContent).ConfigureAwait(false);
            }
        }).ConfigureAwait(false);

        foreach (var x in fileInfos)
            yield return x;

        // await foreach (var dotFI in dotFI_Task)
        // {
        //  //var dotFI = await dotFI_Task;
        //  using (var file = dotFI.OpenWrite())
        //  using (var writer = new StreamWriter(file))
        //    //writer.WriteLine(operationTraceContent);
        //    await writer.WriteLineAsync(operationTraceContent);
        //
        //  yield return dotFI;
        // }
    }

    // https://stackoverflow.com/a/9974107 / https://stackoverflow.com/questions/1965787/how-to-delete-files-subfolders-in-a-specific-directory-at-the-command-prompt-in
    private static async IAsyncEnumerable<FileInfo> CleanCodeAndImageFoldersEnum(this FileInfo dotFI) // , ref bool isPermited)
    {
        if (dotFI is null)
        {
            throw new ArgumentNullException(nameof(dotFI));
        }

        // if (!_isPermitted) /// <= daab 20230823 correlation key G: commented if
        // {

        //// daab 20230823 correlation key A
        ////   Supposedly, while #1 is acquiring a lock,
        ////   a few other processes (e.g., #s 3, 4, 5 and 6)
        ////   leak-through here instead of waiting?
        await _semaphoreSlimEnum.WaitAsync().ConfigureAwait(false); //// <= daab 20230823 correlation key A: multiple threads enter here
                                                                    //// daab 20230823 correlation key D
                                                                    ////   Yup! Double-commenting-out the if block below because is not thread-safe!

        /////// daab 20230823 correlation key B
        /////// While writing the above comment for correlation-key-A,
        /////// I realized that I can squelch these wayward leaks here,
        /////// closer-to-home, instead of in the else block;
        /////// so adding here and commenting-out the else block below.
        ////if (_semaphoreSlim.CurrentCount > 1)
        ////{
        ////  /// daab 20230823 correlation key B
        ////  ///   Release the spurrious lock acquired by the wayward processes,
        ////  ///   and wait for the lock to be released by the process that acquired it first.
        ////  _semaphoreSlim.Release();
        ////  await _semaphoreSlim.WaitAsync();

        ////  /// daab 20230823 correlation key C
        ////  ///   If any fall-through here, then restore the else block from correlation-key-B :-)
        ////}
        // } /// <= daab 20230823 correlation key G: commented if
        try
        {
            if (_isPermitted)
            {
                // lock(_lock) //// <= daab 20230823 correlation key H/correlation key I: protect shared memory with shared-object-lock; commented for deadock with (apparently?) semaphore
                _isPermitted = false;  //// <= daab 20230823 correlation key F: moved to here from correlation-key-E

                // _startInfoDEL.Arguments = _deletecommand(dotFI.DirectoryName!);
                // processDEL.Start();    //System.Diagnostics.Process.Start(startInfoCMD);
                // processDEL.WaitForExit();

                /////// daab 20230823 correlation key D
                ///////  Added if-block around the awaits to skip-to-finally if the delegated-code block has already ran.
                ///////  Likely overkill because can probably use this or the else block below, but I'm not sure which is better.
                ////if (_isPermitted)
                ////{
                if (await _processDebug(_startInfoDEL(_deletecommand(dotFI.DirectoryName!))).ConfigureAwait(false) is Process p)
                    await p.WaitForExitAsync().ConfigureAwait(false); // await Task.Delay(15000); Debug.Print("CLEANED DIRECTORIES");
                                                                      ////}
            }

            //// daab 20230823 correlation key D
            ////   Yup! Un-commenting-out correlation-key-B for the else block below because is proven(?) thread-safe!
            //// daab 20230823 correlation key B
            ////   Commented in-favor of the squelch above.
            // else
            //  /// daab 20230823 correlation key A
            //  ///   I missed this else-block the first twenty debugging-hours (save yourself!),
            //  ///   and it was causing the deadlock that stopped a few processes
            //  ///   that-snuck-through? (4 out-of 206, e.g. #s 3, 4, 5 and 6) from completing,
            //  ///   leaving some files not generated, and I memory leak, I suppose.
            //  _semaphoreSlim.Release();
            //  await _semaphoreSlim.WaitAsync();
        }
        finally
        {
            // _isPermitted = false;  /// daab 20230823 correlation key E: commented and moved to correlation-key-F
            // while (true) { if (_semaphoreSlim.Release() == 0) break; }
            _semaphoreSlimEnum.Release();
        }

        yield return dotFI;
    }

    #endregion IAysncEnumerable-Based Helpers

    #endregion Feature Helpers

    #region Multi-threading Related

    private static bool _isPermitted = true;

    // https://stackoverflow.com/a/45769160 / https://stackoverflow.com/questions/20084695/lock-and-async-method-in-c-sharp
    private static readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private static readonly SemaphoreSlim _semaphoreSlimEnum = new(1, 1);

    #endregion Multi-threading Related

    #region File-System Related

    private const string DIAGNAMEPATTERN = @"<br\/><font point-size=""\d+"">(Operation #\d+)<\/font>";
    private static readonly FileInfo _cmdexe = new("C:\\Windows\\System32\\cmd.exe");
    private static readonly FileInfo _dotexe = new("C:\\Program Files\\Graphviz\\bin\\dot.exe")!;
    private static readonly Func<string, Match> _match = dotCode => Regex.Match(dotCode, DIAGNAMEPATTERN); // https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expressions
    private static readonly Func<FileInfo, string, string> _diagName = (dotFI, diagName) => $"{Path.Combine(Path.GetFullPath("..\\image", dotFI.DirectoryName!), diagName ?? dotFI.Name)}.png"; // https://stackoverflow.com/questions/4796254/relative-path-to-absolute-path-in-c
    private static readonly Func<Guid, string> _dotName = guid => Path.GetFullPath($@"..\..\tests\di\dot\{guid}.dot");
    private static readonly Func<(FileInfo dotFI, FileInfo diagFI), string> _cmdGenDiagram = files => $@"-Tpng ""{files.dotFI.FullName}"" -o""{_diagName(files.dotFI, files.diagFI.Name)}""";
    private static long _taskCount;
    private static long _taskTotalElapsedTicks;
    private static long _enumCount;
    private static long _enumTotalElapsedTicks;

    #endregion File-System Related

    #region Process Related

    /// <summary>
    ///   NOTE OF ACCEPTABLE ERROR!
    ///     </summary>
    /// <remarks>
    ///   When 'rd . /s /q' (A) attempts to delete the root directory (B),
    ///   the former (A) [innocuously](javascript:void(0) "except for case of '&amp;&amp;' which is error-sensitivie, so use '&amp;' instead")
    ///   errors because the latter (B) is the then 'PWD'-after-a-'cd /d'.
    ///
    ///   The error is fine because the intent is to recersively delete the contents
    ///   *except* the root directory (i.e. the failing to delete PWD).
    ///
    ///   The error reads, "The process cannot access the file because it is being used by another process.".
    ///   </remarks>
    ///
    /// <seealso ref="https://stackoverflow.com/questions/26710389/cant-delete-folder-from-batch-folder-or-file-open-in-another-program"/>
    private static readonly Func<string, string> _deletecommand = folderFullPath => @"""/c cd /d """"D:\backup\Documents\source\BluArchie\tests\di\dot"""" && RD . /S /Q & cd /d """"D:\backup\Documents\source\BluArchie\tests\di\image"""" && RD . /S /Q & exit /b 0""";

    private static readonly Func<FileInfo, string, bool, ProcessStartInfo> _startInfo = (cmd, args, useShellExecute) => new()
    {
        // https://stackoverflow.com/a/1469790 / https://stackoverflow.com/questions/1469764/run-command-prompt-commands
        WindowStyle = ProcessWindowStyle.Hidden,
        FileName = cmd.Exists ? cmd.FullName : throw new FileNotFoundException($"Missing {nameof(cmd.FullName)}", cmd.FullName),
        CreateNoWindow = true,
        UseShellExecute = useShellExecute,
        RedirectStandardOutput = !useShellExecute,
        Arguments = args,
    };

    private static readonly Func<string, ProcessStartInfo> _startInfoDOT = args => _startInfo(_dotexe!, args, false);
    private static readonly Func<string, ProcessStartInfo> _startInfoDEL = args => _startInfo(_cmdexe!, args, true);

    private static readonly Func<ProcessStartInfo, Task<Process?>> _process = si => Task.Run(() => Process.Start(si));
    private static readonly Func<ProcessStartInfo, Task<Process?>> _processDebug = async si =>
    {
        var p = await _process(si).ConfigureAwait(false);

#if DEBUG
        if (p is Process process && !process.StartInfo.UseShellExecute)
        {
            process.OutputDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) Debug.WriteLine(e.Data); };
            process.BeginOutputReadLine();
        }
#endif

        return p;
    };

    #region old 1

    // private static readonly ProcessStartInfo startInfoDOT = new()
    // {
    //  // https://stackoverflow.com/a/1469790 / https://stackoverflow.com/questions/1469764/run-command-prompt-commands
    //  WindowStyle = ProcessWindowStyle.Hidden,
    //  FileName = "C:\\Program Files\\Graphviz\\bin\\dot.exe",
    //  CreateNoWindow = true,
    //  UseShellExecute = false,
    //  RedirectStandardOutput = true,
    // };

    // private static readonly ProcessStartInfo startInfoDEL = new()
    // {
    //  // https://stackoverflow.com/a/1469790 / https://stackoverflow.com/questions/1469764/run-command-prompt-commands
    //  WindowStyle = ProcessWindowStyle.Hidden,
    //  FileName = "C:\\Windows\\System32\\cmd.exe",
    //  CreateNoWindow = true,
    //  UseShellExecute = true,
    // };

    // private static readonly Process processDOT = new Process()
    // {
    //  StartInfo = startInfoDOT
    // };

    // private static readonly Process processDEL = new Process()
    // {
    //  StartInfo = startInfoDEL
    // };
    //
    #endregion old 1

    #region old 2

    // private static readonly Func<string, ProcessStartInfo> _startInfoDOT = args => new()
    // {
    //  // https://stackoverflow.com/a/1469790 / https://stackoverflow.com/questions/1469764/run-command-prompt-commands
    //  WindowStyle = ProcessWindowStyle.Hidden,
    //  FileName = "C:\\Program Files\\Graphviz\\bin\\dot.exe",
    //  CreateNoWindow = true,
    //  UseShellExecute = false,
    //  RedirectStandardOutput = true,
    //  Arguments = args,
    // };

    // private static readonly Func<string, ProcessStartInfo> _startInfoDEL = args => new()
    // {
    //  // https://stackoverflow.com/a/1469790 / https://stackoverflow.com/questions/1469764/run-command-prompt-commands
    //  WindowStyle = ProcessWindowStyle.Hidden,
    //  FileName = "C:\\Windows\\System32\\cmd.exe",
    //  CreateNoWindow = true,
    //  UseShellExecute = false,
    //  RedirectStandardOutput = true,
    //  Arguments = args,
    // };
    #endregion old 2

    #endregion Process Related
}
