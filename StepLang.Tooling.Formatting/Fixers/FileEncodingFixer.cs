using System.Text;
using StepLang.Tooling.Formatting.Fixers.Results;

namespace StepLang.Tooling.Formatting.Fixers;

public class FileEncodingFixer : IFileFixer
{
    private static readonly Encoding DefaultEncoding = Encoding.UTF8;

    public async Task<FileFixResult> FixAsync(FileInfo input, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(input.FullName, FileMode.Open, FileAccess.Read);

        var usedEncoding = GetEncoding(stream, DefaultEncoding);
        if (Equals(usedEncoding, DefaultEncoding))
            return new(false, input);

        var tempFile = Path.GetTempFileName();

        await using var tempWriter = new StreamWriter(tempFile, false, DefaultEncoding);
        using var fileReader = new StreamReader(stream, usedEncoding);
        await tempWriter.WriteAsync(await fileReader.ReadToEndAsync(cancellationToken));
        await tempWriter.FlushAsync();

        return new(true, new(tempFile));
    }

    private static Encoding GetEncoding(Stream stream, Encoding fallback)
    {
        using var reader = new StreamReader(stream, fallback, true);

        // Detect byte order mark if any - otherwise assume default
        reader.Peek();

        return reader.CurrentEncoding;
    }
}