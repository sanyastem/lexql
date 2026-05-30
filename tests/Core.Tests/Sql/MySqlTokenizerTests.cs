using Lexql.Core.Relational.Sql;

namespace Lexql.Core.Tests.Sql;

public class MySqlTokenizerTests
{
    [Fact]
    public void Tokenize_ProducesKeywordAndIdentifierTokens()
    {
        var symbols = MySqlTokenizer.Tokenize("SELECT id FROM users")
            .Where(t => t.Channel == 0)
            .Select(t => t.Symbol)
            .ToList();

        Assert.Contains("SELECT", symbols);
        Assert.Contains("FROM", symbols);
        Assert.Contains("ID", symbols);
    }

    [Fact]
    public void Tokenize_IsCaseInsensitive()
    {
        var upper = MySqlTokenizer.Tokenize("select 1").First(t => t.Channel == 0);

        Assert.Equal("SELECT", upper.Symbol);
        Assert.Equal("select", upper.Text);
    }

    [Fact]
    public void Tokenize_StringLiteralIsSingleToken()
    {
        var token = MySqlTokenizer.Tokenize("'a;b'").Single(t => t.Symbol == "STRING_LITERAL");

        Assert.Equal("'a;b'", token.Text);
    }

    [Fact]
    public void Tokenize_CommentsAreHiddenChannel()
    {
        var tokens = MySqlTokenizer.Tokenize("SELECT 1 -- note\n");

        Assert.Contains(tokens, t => t.Symbol == "LINE_COMMENT" && t.Channel != 0);
    }
}
