using System.CommandLine;
using System.IO.Abstractions;
using Microsoft.CodeAnalysis;
using Spectre.Console;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.AutoMapper;
using Unshackle.Core.Fixes.EfBulk;
using Unshackle.Core.Fixes.EPPlus;
using Unshackle.Core.Fixes.FluentAssertions;
using Unshackle.Core.Fixes.MassTransit;
using Unshackle.Core.Fixes.MediatR;
using Unshackle.Core.Fixes.Moq;
using Unshackle.Core.Fixes.Polly;
using Unshackle.Core.Fixes.SpecFlow;
using Unshackle.Core.Fixes.XunitV3;
using Unshackle.Core.Models;
using Unshackle.DataAccess.Projects;
using Unshackle.DataAccess.Reports;
using Unshackle.DataAccess.Solutions;
using Unshackle.DataAccess.SourceControl;
using Unshackle.Interface.Commands;
using Unshackle.Interface.Output;

namespace Unshackle.Interface;

// Composition root and command line: Program.cs only calls RunAsync, so tests can run the real CLI in-process.
public static class Cli
{
    private const int RedirectedWidth = 1000;

    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        var fileSystem = new FileSystem();
        var assetsReader = new JsonPackageAssetsReader(fileSystem);
        IReadOnlyList<IFix> fixes =
        [
            new FluentAssertionsFix([MetadataReference.CreateFromFile(Path.Combine(AppContext.BaseDirectory, "References", "Shouldly.dll"))]),
            new MediatRFix(new XmlProjectProperties(fileSystem)),
            new AutoMapperFix(new XmlProjectProperties(fileSystem)),
            new MoqFix([MetadataReference.CreateFromFile(Path.Combine(AppContext.BaseDirectory, "References", "NSubstitute.dll"))]),
            new PollyFix(),
            new EPPlusFix(new XmlProjectProperties(fileSystem), new FileConfigKeySearch(fileSystem), assetsReader),
            new MassTransitFix(),
            new EfBulkFix(),
            new SpecFlowFix(new XmlProjectProperties(fileSystem)),
            new XunitV3Fix(),
        ];
        IReportWriter[] reportWriters =
            [new CsvReportWriter(fileSystem), new JsonReportWriter(fileSystem), new MarkdownReportWriter(fileSystem)];
        IAnsiConsole output = AnsiConsole.Console;
        IAnsiConsole error = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(Console.Error) });
        // Redirected (a file, a pipe, PowerShell capturing): Spectre would wrap at 80 columns and break paths and diff lines.
        output.Profile.Width = Console.IsOutputRedirected ? RedirectedWidth : output.Profile.Width;
        error.Profile.Width = Console.IsErrorRedirected ? RedirectedWidth : error.Profile.Width;
        var view = new SpectreConsoleView(output, error);
        using var store = new MsBuildSolutionStore(fileSystem, fixes.SelectMany(f => f.RemovedNamespaces));
        var projectFiles = new XmlProjectFileStore(fileSystem);
        var scan = new ScanCommand(new Fixer(fixes, projectFiles, assetsReader), store, reportWriters, view);
        var fix = new FixCommand(scan, projectFiles, new GitSourceControl(), view);
        var list = new ListCommand(fixes, view);

        RootCommand root = CreateRootCommand(fixes, reportWriters, scan, fix, list, view);
        ParseResult parseResult = root.Parse(args);
        int exitCode = await parseResult.InvokeAsync(cancellationToken: cancellationToken);
        // System.CommandLine prints parse errors with exit code 1; scripts get 2 to tell them apart from load errors.
        return parseResult.Errors.Count > 0 ? ExitCodes.InvalidArguments : exitCode;
    }

    private static RootCommand CreateRootCommand(
        IReadOnlyList<IFix> fixes,
        IReadOnlyList<IReportWriter> reportWriters,
        ScanCommand scan,
        FixCommand fix,
        ListCommand list,
        IConsoleView view)
    {
        var solution = new Argument<string?>("solution")
        {
            Description = "Path to a .sln, .slnx or project file. Default: the only solution in the current directory.",
            Arity = ArgumentArity.ZeroOrOne,
        };
        var fixOption = new Option<string>("--fix") { Description = "The fix to run, see 'unshackle list'.", Required = true };
        fixOption.AcceptOnlyFromAmong([.. fixes.Select(f => f.Id)]);
        var report = new Option<string?>("--report")
        {
            Description = $"Write every finding to this file ({string.Join(", ", reportWriters.Select(w => w.Extension))}).",
        };
        var dryRun = new Option<bool>("--dry-run") { Description = "Show the diff, write nothing." };
        var force = new Option<bool>("--force") { Description = "Write even when the working tree is dirty, not a git repository, or git fails; overwrite an existing report." };

        var sequential = new Option<bool>("--sequential") { Description = "Fix one file at a time instead of several in parallel (same output)." };
        var maxDegree = new Option<int?>("--max-degree") { Description = $"Fix at most this many files at once (default: the processor count, at most {Fixer.DefaultMaxDegree}); lower it when memory runs out." };
        var checkEveryFile = new Option<bool>("--check-every-file") { Description = "Run the patterns on every file, also those without a trace of the old library (same output; for comparison runs).", Hidden = true };
        FixRequest Request(ParseResult r) => new()
        {
            FixId = r.GetRequiredValue(fixOption), Sequential = r.GetValue(sequential), MaxDegree = r.GetValue(maxDegree),
            CheckEveryFile = r.GetValue(checkEveryFile),
        };
        var scanCommand = new Command("scan", "Run a fix and report what it would change, without writing code.")
        {
            solution, fixOption, report, force, sequential, maxDegree, checkEveryFile,
        };
        scanCommand.SetAction((r, ct) => WithSolution(view, r.GetValue(solution), path => scan.ExecuteAsync(
            path, Request(r), r.GetValue(report), r.GetValue(force), ct)));

        var fixCommand = new Command("fix", "Run a fix and write the changes.") { solution, fixOption, report, dryRun, force, sequential, maxDegree, checkEveryFile };
        fixCommand.SetAction((r, ct) => WithSolution(view, r.GetValue(solution), path => fix.ExecuteAsync(
            path, Request(r), r.GetValue(report), r.GetValue(dryRun), r.GetValue(force), ct)));

        var markdown = new Option<bool>("--markdown") { Description = "Print the patterns as a Markdown table (docs/patterns.md is made with this)." };
        var listCommand = new Command("list", "Show every fix and its patterns.") { markdown };
        listCommand.SetAction(r => list.Execute(r.GetValue(markdown)));

        return new RootCommand("Migrate .NET code away from libraries that went commercial.") { scanCommand, fixCommand, listCommand };
    }

    private static Task<int> WithSolution(IConsoleView view, string? given, Func<string, Task<int>> run)
    {
        string path;
        try
        {
            path = SolutionLocator.Resolve(given, Directory.GetCurrentDirectory());
        }
        catch (CommandException ex)
        {
            view.ShowError(ex.Message);
            return Task.FromResult(ex.ExitCode);
        }

        return run(path);
    }
}
