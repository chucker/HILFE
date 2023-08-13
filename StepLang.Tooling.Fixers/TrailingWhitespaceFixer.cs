﻿namespace StepLang.Formatters;

public class TrailingWhitespaceFixer : IStringFixer
{
    public string Name => "TrailingWhitespaceFixer";

    public Task<StringFixResult> FixAsync(string input, CancellationToken cancellationToken = default)
    {
        var fixedString = input.WithoutTrailingWhitespace();

        return Task.FromResult(StringFixResult.FromInputAndFix(input, fixedString));
    }
}