namespace Corel.Cli.Tests;

/// <summary>The shipped samples double as a guard that the documented grammar and the
/// validator agree - a sample that stopped preflighting cleanly would mean the docs
/// promise something the code rejects.</summary>
public class SampleScriptTests
{
    private static readonly Dictionary<string, string> SampleVars = new()
    {
        ["WIDTH"] = "12",
        ["HEIGHT"] = "19",
        ["UNIT"] = "in",
        ["OUTPUT"] = @"C:\temp\sample-output.cdr",
    };

    public static TheoryData<string> Samples()
    {
        var data = new TheoryData<string>();
        foreach (string file in Directory.GetFiles(SamplesDirectory(), "*.cdx"))
        {
            data.Add(file);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Samples))]
    public void EverySampleScriptPassesStructuralPreflightAndSafetyScreening(string path)
    {
        ScriptPreparation preparation = ScriptRunner.Prepare(path, SampleVars, corelVersion: null);

        Assert.Empty(preparation.BlockingErrors);
        Assert.Empty(ScriptSafety.Inspect(preparation.Lines));
        Assert.NotEmpty(preparation.Lines);
    }

    private static string SamplesDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "scripts", "samples")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(directory.FullName, "scripts", "samples");
    }
}
