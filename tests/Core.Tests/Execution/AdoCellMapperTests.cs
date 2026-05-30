using Lexql.Core.Abstractions;
using Lexql.Core.Relational.Execution;

namespace Lexql.Core.Tests.Execution;

public class AdoCellMapperTests
{
    [Theory]
    [InlineData(typeof(bool), CellKind.Boolean)]
    [InlineData(typeof(byte), CellKind.Integer)]
    [InlineData(typeof(int), CellKind.Integer)]
    [InlineData(typeof(long), CellKind.Integer)]
    [InlineData(typeof(ulong), CellKind.Integer)]
    [InlineData(typeof(float), CellKind.Float)]
    [InlineData(typeof(double), CellKind.Float)]
    [InlineData(typeof(decimal), CellKind.Decimal)]
    [InlineData(typeof(string), CellKind.String)]
    [InlineData(typeof(byte[]), CellKind.Bytes)]
    [InlineData(typeof(DateTime), CellKind.DateTime)]
    [InlineData(typeof(Guid), CellKind.String)]
    public void MapKind_MapsClrTypes(Type type, CellKind expected)
    {
        Assert.Equal(expected, AdoCellMapper.MapKind(type));
    }

    [Fact]
    public void MapKind_UnwrapsNullable()
    {
        Assert.Equal(CellKind.Integer, AdoCellMapper.MapKind(typeof(int?)));
        Assert.Equal(CellKind.DateTime, AdoCellMapper.MapKind(typeof(DateTime?)));
    }

    [Fact]
    public void MapValue_NullAndDbNull_BecomeNullCell()
    {
        var fromNull = Assert.IsType<CellValue.Scalar>(AdoCellMapper.MapValue(null, CellKind.Integer));
        var fromDbNull = Assert.IsType<CellValue.Scalar>(AdoCellMapper.MapValue(DBNull.Value, CellKind.String));

        Assert.Equal(CellKind.Null, fromNull.Kind);
        Assert.Null(fromNull.Value);
        Assert.Equal(CellKind.Null, fromDbNull.Kind);
    }

    [Fact]
    public void MapValue_KeepsKindAndValue()
    {
        var scalar = Assert.IsType<CellValue.Scalar>(AdoCellMapper.MapValue(42, CellKind.Integer));

        Assert.Equal(CellKind.Integer, scalar.Kind);
        Assert.Equal(42, scalar.Value);
    }
}
