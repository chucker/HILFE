﻿using System.Drawing;
using System.Globalization;
using Pastel;
using StepLang.Tooling.Formatting.Applicators;
using StepLang.Tooling.Formatting.Fixers;
using StepLang.Tooling.Formatting.FixerSets;

namespace StepLang.CLI;

internal static class FormatCommand
{
    private static VerbosityWriter stdout = new(Console.Out, Verbosity.Normal);
    private static VerbosityWriter stderr = new(Console.Error, Verbosity.Normal);

    public static async Task<int> Invoke(string [] filesOrDirs, bool setExitCode, bool dryRun, Verbosity verbosity,
        FileInfo? formatConfigFile)
    {
        stdout = new(Console.Out, verbosity);
        stderr = new(Console.Error, verbosity);

        if (dryRun)
            await stdout.Normal("Dry run mode enabled. No files will be modified.".Pastel(ConsoleColor.DarkYellow));

        if (filesOrDirs.Length == 0)
            filesOrDirs = new [] { "." };

        var fixerConfig = await GetFixerConfiguration(formatConfigFile);

        IFixApplicator fixApplicator = dryRun ?
            new DryRunFixApplicator { Configuration = fixerConfig } :
            new DefaultFixApplicator { Configuration = fixerConfig };

        var files = new HashSet<FileInfo>();

        fixApplicator.BeforeFixerRun += async (_, f) =>
        {
            await stdout.Verbose(
                $"Running fixer '{f.Fixer.Name.Pastel(ConsoleColor.DarkMagenta)}' on '{f.File.Name.Pastel(ConsoleColor.Cyan)}'");
        };

        fixApplicator.AfterApplyFix += async (_, f) =>
        {
            files.Add(f.File);

            await stdout.Verbose(
                $"Applied fixer '{f.Fixer.Name.Pastel(ConsoleColor.DarkMagenta)}' to '{f.File.Name.Pastel(ConsoleColor.Cyan)}'");
        };

        var fixerSet = new DefaultFixerSet();

        var results = await filesOrDirs
            .ToAsyncEnumerable()
            .AggregateAwaitAsync(
                FixApplicatorResult.Zero,
                async (current, fileOrDir) => current + await Format(fileOrDir, fixApplicator, fixerSet)
            );

        await stdout.Normal(
            $"{(dryRun ? "Would have fixed" : "Fixed")} {files.Count.ToString(CultureInfo.InvariantCulture).Pastel(ConsoleColor.Green)} file(s) in {results.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture).Pastel(ConsoleColor.DarkCyan)} seconds.");

        if (setExitCode)
            return results.AppliedFixers + results.FailedFixers > 0 ? 1 : 0;

        return 0;
    }

    private static async Task<FixerConfiguration> GetFixerConfiguration(FileInfo? formatConfigFile)
    {
        if (formatConfigFile != null)
        {
            var loadedFixerConfig = FixerConfiguration.FromFile(formatConfigFile);

            if (loadedFixerConfig == null)
            {
                await stderr.Normal($"Could not load format configuration from '{formatConfigFile.FullName.Pastel(Color.DarkRed)}'. Using default");

                return new();
            }

            await stdout.Verbose($"Loaded format configuration from '{formatConfigFile.FullName.Pastel(Color.DarkCyan)}'");

            return loadedFixerConfig;
        }

        await stdout.Verbose("Using default format configuration");

        return new();
    }

    private static async Task<FixApplicatorResult> Format(string fileOrDir, IFixApplicator fixApplicator,
        IFixerSet fixerSet)
    {
        await stdout.Normal($"Formatting '{fileOrDir.Pastel(Color.Aqua)}'...");

        FixApplicatorResult changes;
        if (File.Exists(fileOrDir))
            changes = await fixApplicator.ApplyFixesAsync(fixerSet, new FileInfo(fileOrDir));
        else if (Directory.Exists(fileOrDir))
            changes = await fixApplicator.ApplyFixesAsync(fixerSet, new DirectoryInfo(fileOrDir));
        else
        {
            await stderr.Normal($"The path '{fileOrDir.Pastel(Color.Aqua)}' is not a file or directory.");

            return FixApplicatorResult.Zero;
        }

        return changes;
    }
}