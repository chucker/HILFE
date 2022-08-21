﻿using System.Runtime.CompilerServices;

namespace HILFE;

public static class Parser
{
    private enum State
    {
        LineStart,
        DeclarationType,
        DeclarationName,
        InitializerStart,
        InitializerIdentifier,
        InitializerLiteralString,
        InitializerLiteralNumber,
        IfStatement,
        IfExpression,
        IfExpressionEnd,
        IfCodeBlock,
        IfCodeBlockLineStart,
        IfCodeBlockCloser,
        IfElseBlockStart,
        IdentifierStart,
        IdentifierFunctionArgument,
        IdentifierAssignment,
        IdentifierFunctionArgumentEnd,
        IdentifierFunctionCallEnd
    }

    private static readonly Dictionary<State, Dictionary<(State, StatementType?), TokenType[]>> Transitions = new()
    {
        {
            State.LineStart,
            new()
            {
                { (State.LineStart, null), new[] { TokenType.Whitespace } },
                { (State.LineStart, StatementType.EmptyLine), new[] { TokenType.NewLine } },
                { (State.DeclarationType, null), new[] { TokenType.TypeName } },
                { (State.IfStatement, null), new [] { TokenType.IfKeyword } },
                { (State.IdentifierStart, null), new [] { TokenType.Identifier } },
            }
        },
        {
            State.DeclarationType,
            new()
            {
                { (State.DeclarationType, null), new[] { TokenType.Whitespace } },
                { (State.DeclarationName, null), new[] { TokenType.Identifier } },
            }
        },
        {
            State.DeclarationName,
            new()
            {
                { (State.DeclarationName, null), new[] { TokenType.Whitespace } },
                { (State.InitializerStart, null), new[] { TokenType.EqualsSymbol } },
                { (State.LineStart, StatementType.VariableDeclaration), new[] { TokenType.NewLine } },
            }
        },
        {
            State.InitializerStart,
            new()
            {
                { (State.InitializerStart, null), new[] { TokenType.Whitespace } },
                { (State.InitializerIdentifier, null), new[] { TokenType.Identifier } },
                { (State.InitializerLiteralString, null), new[] { TokenType.LiteralString } },
                { (State.InitializerLiteralNumber, null), new[] { TokenType.LiteralNumber } },
            }
        },
        {
            State.InitializerIdentifier,
            new()
            {
                { (State.LineStart, StatementType.VariableDeclaration), new[] { TokenType.NewLine } },
            }
        },
        {
            State.InitializerLiteralString,
            new()
            {
                { (State.InitializerLiteralString, null), new[] { TokenType.Whitespace } },
                { (State.LineStart, StatementType.VariableDeclaration), new[] { TokenType.NewLine } },
            }
        },
        {
            State.InitializerLiteralNumber,
            new()
            {
                { (State.LineStart, StatementType.VariableDeclaration), new[] { TokenType.NewLine } },
            }
        },
        {
            State.IfStatement,
            new()
            {
                { (State.IfStatement, null), new [] { TokenType.Whitespace } },
                { (State.IfExpression, null), new [] { TokenType.ExpressionOpener } },
            }
        },
        {
            State.IfExpression,
            new()
            {
                { (State.IfExpression, null), new [] { TokenType.Whitespace, TokenType.Identifier, TokenType.EqualsSymbol, TokenType.LiteralString, TokenType.LiteralNumber } },
                { (State.IfExpressionEnd, null), new [] { TokenType.ExpressionCloser } },
            }
        },
        {
            State.IfExpressionEnd,
            new()
            {
                { (State.IfExpressionEnd, null), new [] { TokenType.Whitespace } },
                { (State.IfCodeBlock, null), new [] { TokenType.CodeBlockOpener } },
            }
        },
        {
            State.IfCodeBlock,
            new()
            {
                { (State.IfCodeBlockLineStart, StatementType.IfStatement), new [] { TokenType.NewLine } },
            }
        },
        {
            State.IfCodeBlockLineStart,
            new()
            {
                { (State.IfCodeBlockLineStart, null), new[] { TokenType.Whitespace } },
                { (State.IfCodeBlockLineStart, StatementType.EmptyLine), new[] { TokenType.NewLine } },
                { (State.DeclarationType, null), new[] { TokenType.TypeName } },
                { (State.IfStatement, null), new [] { TokenType.IfKeyword } },
                { (State.IdentifierStart, null), new [] { TokenType.Identifier } },
                { (State.IfCodeBlockCloser, null), new [] { TokenType.CodeBlockCloser } },
            }
        },
        {
            State.IfCodeBlockCloser,
            new()
            {
                { (State.IfCodeBlockCloser, null), new [] { TokenType.Whitespace } },
                { (State.LineStart, StatementType.IfBlockEnd), new [] { TokenType.NewLine } },
                { (State.IfElseBlockStart, null), new [] { TokenType.ElseKeyword } },
            }
        },
        {
            State.IfElseBlockStart,
            new()
            {
                { (State.IfElseBlockStart, null), new [] { TokenType.Whitespace } },
                { (State.IfCodeBlockLineStart, StatementType.ElseStatement), new [] { TokenType.CodeBlockOpener } },
            }
        },
        {
            State.IdentifierStart,
            new()
            {
                { (State.IdentifierStart, null), new [] { TokenType.Whitespace } },
                { (State.IdentifierFunctionArgument, null), new [] { TokenType.ExpressionOpener } },
                { (State.IdentifierAssignment, null), new [] { TokenType.EqualsSymbol } },
            }
        },
        {
            State.IdentifierFunctionArgument,
            new()
            {
                { (State.IdentifierFunctionArgument, null), new [] { TokenType.Whitespace } },
                { (State.IdentifierFunctionArgumentEnd, null), new [] { TokenType.LiteralNumber, TokenType.LiteralString, TokenType.Identifier, } },
            }
        },
        {
            State.IdentifierFunctionArgumentEnd,
            new()
            {
                { (State.IdentifierFunctionArgumentEnd, null), new [] { TokenType.Whitespace } },
                { (State.IdentifierFunctionArgument, null), new [] { TokenType.ExpressionSeparator } },
                { (State.IdentifierFunctionCallEnd, null), new [] { TokenType.ExpressionCloser } },
            }
        },
        {
            State.IdentifierFunctionCallEnd,
            new()
            {
                { (State.IfCodeBlockLineStart, StatementType.FunctionCall), new [] { TokenType.NewLine } },
            }
        },
        {
            State.IdentifierAssignment,
            new()
            {
                { (State.IdentifierAssignment, null), new [] { TokenType.Whitespace } },
            }
        }
    };

    public static async IAsyncEnumerable<Statement> ParseAsync(IAsyncEnumerable<Token> tokens, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var state = State.LineStart;
        var currentStatement = new List<Token>();

        await foreach (var token in tokens.WithCancellation(cancellationToken))
        {
            var stateTransitions = Transitions[state];
            (State, StatementType?)? acceptingTransition = null;
            List<TokenType> expectedTokenTypes = new();
            foreach (var transition in stateTransitions)
            {
                expectedTokenTypes.AddRange(transition.Value);
                if (!transition.Value.Contains(token.Type))
                    continue;

                acceptingTransition = transition.Key;
                break;
            }

            if (!acceptingTransition.HasValue)
            {
                #if DEBUG
                Console.WriteLine($"Processing token {token} in state {state}");
                #endif

                throw new UnexpectedTokenException(expectedTokenTypes, token);
            }

            var (nextState, statementType) = acceptingTransition.Value;

            state = nextState;

            currentStatement.Add(token);
            if (!statementType.HasValue)
                continue;

            yield return new(statementType.Value, currentStatement.ToArray());

            currentStatement.Clear();
        }

        if (currentStatement.Any(t => t.Type is not TokenType.Whitespace and not TokenType.NewLine))
            throw new InvalidOperationException("Unexpected end of input");
    }
}