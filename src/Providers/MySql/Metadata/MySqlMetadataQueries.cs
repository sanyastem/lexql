namespace Lexql.Providers.MySql.Metadata;

public static class MySqlMetadataQueries
{
    public static readonly Version CheckConstraintsMinVersion = new(8, 0, 16);

    public static bool SupportsCheckConstraints(Version? serverVersion) =>
        serverVersion is not null && serverVersion >= CheckConstraintsMinVersion;

    public const string Tables = """
        SELECT TABLE_NAME
        FROM information_schema.TABLES
        WHERE TABLE_SCHEMA = @schema AND TABLE_TYPE = 'BASE TABLE'
        ORDER BY TABLE_NAME;
        """;

    public const string Columns = """
        SELECT TABLE_NAME, COLUMN_NAME, ORDINAL_POSITION, COLUMN_TYPE, IS_NULLABLE,
               COLUMN_DEFAULT, EXTRA, GENERATION_EXPRESSION
        FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = @schema
        ORDER BY TABLE_NAME, ORDINAL_POSITION;
        """;

    public const string PrimaryKeys = """
        SELECT TABLE_NAME, COLUMN_NAME, ORDINAL_POSITION
        FROM information_schema.KEY_COLUMN_USAGE
        WHERE TABLE_SCHEMA = @schema AND CONSTRAINT_NAME = 'PRIMARY'
        ORDER BY TABLE_NAME, ORDINAL_POSITION;
        """;

    public const string Indexes = """
        SELECT TABLE_NAME, INDEX_NAME, COLUMN_NAME, SEQ_IN_INDEX, NON_UNIQUE
        FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = @schema
        ORDER BY TABLE_NAME, INDEX_NAME, SEQ_IN_INDEX;
        """;

    public const string ForeignKeys = """
        SELECT TABLE_NAME, CONSTRAINT_NAME, COLUMN_NAME, ORDINAL_POSITION,
               REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME
        FROM information_schema.KEY_COLUMN_USAGE
        WHERE TABLE_SCHEMA = @schema AND REFERENCED_TABLE_NAME IS NOT NULL
        ORDER BY TABLE_NAME, CONSTRAINT_NAME, ORDINAL_POSITION;
        """;

    public const string Checks = """
        SELECT tc.TABLE_NAME, cc.CONSTRAINT_NAME, cc.CHECK_CLAUSE
        FROM information_schema.CHECK_CONSTRAINTS cc
        JOIN information_schema.TABLE_CONSTRAINTS tc
          ON tc.CONSTRAINT_SCHEMA = cc.CONSTRAINT_SCHEMA
         AND tc.CONSTRAINT_NAME = cc.CONSTRAINT_NAME
        WHERE cc.CONSTRAINT_SCHEMA = @schema
        ORDER BY tc.TABLE_NAME, cc.CONSTRAINT_NAME;
        """;

    public const string Views = """
        SELECT TABLE_NAME, VIEW_DEFINITION
        FROM information_schema.VIEWS
        WHERE TABLE_SCHEMA = @schema
        ORDER BY TABLE_NAME;
        """;

    public const string Routines = """
        SELECT ROUTINE_NAME, ROUTINE_TYPE, ROUTINE_DEFINITION
        FROM information_schema.ROUTINES
        WHERE ROUTINE_SCHEMA = @schema
        ORDER BY ROUTINE_NAME;
        """;

    public const string Triggers = """
        SELECT TRIGGER_NAME, ACTION_STATEMENT
        FROM information_schema.TRIGGERS
        WHERE TRIGGER_SCHEMA = @schema
        ORDER BY TRIGGER_NAME;
        """;
}
