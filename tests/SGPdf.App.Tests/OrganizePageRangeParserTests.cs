using System.Reflection;
using Xunit;

namespace SGPdf.App.Tests;

public sealed class OrganizePageRangeParserTests
{
    [Theory]
    [InlineData("1", 5, new[] { 0 })]
    [InlineData("1,3,5-7", 7, new[] { 0, 2, 4, 5, 6 })]
    [InlineData(" 1 , 3 , 5 - 7 ", 7, new[] { 0, 2, 4, 5, 6 })]
    [InlineData("2-4,6", 6, new[] { 1, 2, 3, 5 })]
    public void Parse_ValidExpression_ReturnsOrderedUniqueZeroBasedIndices(
        string expression,
        int pageCount,
        int[] expected)
    {
        Assert.Equal(expected, Parse(expression, pageCount));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1-0")]
    [InlineData("3-2")]
    [InlineData("6")]
    [InlineData("1,1")]
    [InlineData("1-3,3")]
    [InlineData("1,,2")]
    [InlineData("1-")]
    [InlineData("-2")]
    [InlineData("a")]
    [InlineData("1-2-3")]
    public void Parse_InvalidExpression_ThrowsArgumentException(string expression)
    {
        Assert.ThrowsAny<ArgumentException>((Action)(() => Parse(expression, 5)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Parse_NonPositivePageCount_ThrowsArgumentOutOfRangeException(int pageCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => Parse("1", pageCount)));
    }

    private static IReadOnlyList<int> Parse(string expression, int pageCount)
    {
        var type = typeof(MainWindow).Assembly.GetType(
            "SGPdf.App.Features.Organize.OrganizePageRangeParser",
            throwOnError: false);
        Assert.NotNull(type);

        var method = type.GetMethod(
            "Parse",
            BindingFlags.Static | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(string), typeof(int) },
            modifiers: null);
        Assert.NotNull(method);

        try
        {
            return (IReadOnlyList<int>)method.Invoke(null, new object?[] { expression, pageCount })!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}
