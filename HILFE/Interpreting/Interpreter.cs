﻿using HILFE.Parsing.Statements;

namespace HILFE.Interpreting;

public class Interpreter
{
    public TextWriter? StdOut { get; }
    public TextWriter? StdErr { get; }
    public TextReader? StdIn { get; }
    public int ExitCode { get; set; }

    private readonly TextWriter? debugOut;
    private readonly Stack<Scope> scopes = new();

    public Interpreter(TextWriter? stdOut = null, TextWriter? stdErr = null, TextReader? stdIn = null,
        TextWriter? debugOut = null)
    {
        StdOut = stdOut;
        StdErr = stdErr;
        StdIn = stdIn;
        this.debugOut = debugOut;

        scopes.Push(Scope.GlobalScope);
    }

    public Scope CurrentScope => scopes.Peek();

    public void PushScope() => scopes.Push(new(CurrentScope));

    public void PopScope() => scopes.Pop();

    public async Task InterpretAsync(IAsyncEnumerable<Statement> statements,
        CancellationToken cancellationToken = default)
    {
        await foreach (var statement in statements.WithCancellation(cancellationToken))
        {
            if (debugOut is not null)
                await debugOut.WriteLineAsync(statement.ToString());

            await InterpretStatement(statement, cancellationToken);
        }
    }

    private async Task InterpretStatement(Statement statement, CancellationToken cancellationToken)
    {
        switch (statement)
        {
            case IExecutableStatement executableStatement:
                await executableStatement.ExecuteAsync(this, cancellationToken);
                break;
            case ILoopingStatement loopingStatement:
                await InterpretLoopingStatement(loopingStatement, cancellationToken);
                break;
            default:
                throw new NotImplementedException($"Given statement cannot be interpreted: {statement}");
        }
    }

    private async Task InterpretLoopingStatement(ILoopingStatement statement, CancellationToken cancellationToken)
    {
        await statement.InitializeLoopAsync(this, cancellationToken);

        while (await statement.ShouldLoopAsync(this, cancellationToken))
            await statement.ExecuteLoopAsync(this, cancellationToken);

        await statement.FinalizeLoopAsync(this, cancellationToken);
    }
}