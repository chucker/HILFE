using HILFE.Parsing;
using HILFE.Tokenizing;

namespace HILFE.Tests.Parsing;

public class TokenQueueTest
{
    [Fact]
    public void TestDequeueThrowsIfEmpty()
    {
        var queue = new TokenQueue();
        
        Assert.Throws<UnexpectedEndOfInputException>(() => queue.Dequeue());
        Assert.Throws<UnexpectedEndOfInputException>(() => queue.Dequeue(1));
        Assert.Throws<UnexpectedEndOfInputException>(() => queue.Dequeue(TokenType.Whitespace));
    }
}