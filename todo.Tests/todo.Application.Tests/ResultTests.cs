using TodoApi.Application.Common.Results;

namespace todo.Application.Tests;

public class ResultTests
{
    [Fact]
    public void FailureCannotBeReadAsASuccessAndNullPayloadsAreRejected()
    {
        var failure = Result<string>.Failure(AppErrors.ListNotFound);
        Assert.True(failure.IsFailure);
        Assert.Equal(ErrorCode.ListNotFound, failure.Error!.Code);
        Assert.Throws<InvalidOperationException>(() => failure.Value);
        Assert.Throws<ArgumentNullException>(() => Result<string>.Success(null!));
        Assert.Throws<ArgumentNullException>(() => Result<string>.Failure(null!));
        var success = Result<string>.Success("saved");
        Assert.Null(success.Error);
        Assert.Equal("saved", success.Value);
    }

    [Fact]
    public void ValidationOwnsACopyOfTheOriginalFieldErrors()
    {
        string[] messages = ["Required"];
        var fields = new Dictionary<string, string[]> { ["Name"] = messages };
        var error = AppErrors.Validation(fields);
        messages[0] = "Changed";
        fields.Clear();
        Assert.Equal("Required", Assert.Single(error.Errors["Name"]));
    }
}
