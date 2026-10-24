using TodoApi.Application.Contracts;
using TodoApi.Application.Exceptions;

namespace todo.Application.Tests;

public class PageRequestTests
{
    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(1, 101)]
    [InlineData(int.MaxValue, 100)]
    public void InvalidBoundsAreRejected(int pageNumber, int pageSize) =>
        Assert.Throws<RequestValidationException>(() => new PageRequest(pageNumber, pageSize));

    [Fact]
    public void DefaultAndMaximumPagesHaveSafeOffsets()
    {
        Assert.Equal(20, new PageRequest().PageSize);
        Assert.Equal(0, new PageRequest().Offset);
        Assert.Equal(100, new PageRequest(2, 100).Offset);
        Assert.Equal(int.MaxValue - 1, new PageRequest(int.MaxValue, 1).Offset);
    }
}
