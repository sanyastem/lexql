using Antlr4.Runtime;
using Lexql.Core.Relational.Sql.Antlr;

namespace Lexql.Core.Relational.Sql;

public static class MySqlTokenizer
{
    public static IReadOnlyList<SqlToken> Tokenize(string sql)
    {
        ArgumentNullException.ThrowIfNull(sql);

        var lexer = new MySqlLexer(new AntlrInputStream(sql));
        lexer.RemoveErrorListeners();

        var tokens = new List<SqlToken>();
        for (var token = lexer.NextToken(); token.Type != TokenConstants.EOF; token = lexer.NextToken())
        {
            tokens.Add(new SqlToken(
                token.Type,
                MySqlLexer.DefaultVocabulary.GetSymbolicName(token.Type) ?? string.Empty,
                token.Text ?? string.Empty,
                token.StartIndex,
                token.StopIndex,
                token.Channel));
        }

        return tokens;
    }
}
