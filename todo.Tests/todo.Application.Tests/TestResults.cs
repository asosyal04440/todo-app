using TodoApi.Application.Common.Results;

namespace todo.Application.Tests;

internal static class TestResults
{
    public static T Success<T>(Result<T> result)
    {
        Assert.True(result.IsSuccess, result.Error?.Code.ToString());
        return result.Value;
    }

    public static async Task<AppError> FailureAsync<T>(Func<Task<Result<T>>> execute, ErrorKind expected)
    {
        Result<T> result = await execute();
        Assert.True(result.IsFailure);
        Assert.Equal(expected, result.Error!.Kind);
        return result.Error;
    }
}