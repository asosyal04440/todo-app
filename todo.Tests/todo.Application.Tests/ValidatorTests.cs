using FluentValidation.Results;
using TodoApi.Application.Features.Authentication.LoginWithGoogle;
using TodoApi.Application.Features.Authentication.LoginWithPassword;
using TodoApi.Application.Features.Authentication.RegisterWithPassword;
using TodoApi.Application.Features.TodoLists.CreateTodoList;
using TodoApi.Application.Features.TodoLists.RenameTodoList;
using TodoApi.Application.Features.Todos.CreateTodo;
using TodoApi.Application.Features.Todos.UpdateTodo;
using TodoApi.Domain.Enums;
using todo.Application.Tests.TestDoubles;

namespace todo.Application.Tests;

public class ValidatorTests
{
    [Theory]
    [InlineData(120, true)]
    [InlineData(121, false)]
    public void CreateTodo_EnforcesTheDomainTitleBoundary(int length, bool expected)
    {
        CreateTodoCommand command = new CreateTodoCommand
        {
            TodoListId = 1,
            Title = new string('a', length)
        };

        ValidationResult result = new CreateTodoCommandValidator(FakeTimeProvider.AtTestStart())
            .Validate(command);

        Assert.Equal(expected, result.IsValid);
    }

    [Theory]
    [InlineData(7, false)]
    [InlineData(8, true)]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void Register_EnforcesThePasswordLengthBoundaries(int length, bool expected)
    {
        RegisterWithPasswordCommand command = new RegisterWithPasswordCommand
        {
            Email = "ali@example.com",
            DisplayName = "Ali",
            Password = "a1" + new string('a', length - 2)
        };

        Assert.Equal(expected, new RegisterWithPasswordCommandValidator().Validate(command).IsValid);
    }

    [Fact]
    public void Register_AcceptsAUnicodeLetterAndDecimalDigit()
    {
        RegisterWithPasswordCommand command = new RegisterWithPasswordCommand
        {
            Email = "ali@example.com",
            DisplayName = "Ali",
            Password = "Şifre١٢٣"
        };

        Assert.True(new RegisterWithPasswordCommandValidator().Validate(command).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateTodoList_RejectsAnEmptyName(string name)
    {
        ValidationResult result = new CreateTodoListCommandValidator()
            .Validate(new CreateTodoListCommand { Name = name });

        ValidationFailure failure = Assert.Single(result.Errors);
        Assert.Equal(nameof(CreateTodoListCommand.Name), failure.PropertyName);
        Assert.Contains("Liste adı", failure.ErrorMessage);
    }

    [Fact]
    public void CreateTodoList_RejectsANameLongerThanEightyCharacters()
    {
        ValidationResult result = new CreateTodoListCommandValidator()
            .Validate(new CreateTodoListCommand { Name = new string('a', 81) });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateTodoList_AcceptsAValidName()
    {
        ValidationResult result = new CreateTodoListCommandValidator()
            .Validate(new CreateTodoListCommand { Name = "Ev işleri" });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void RenameTodoList_RejectsAnInvalidListId()
    {
        ValidationResult result = new RenameTodoListCommandValidator()
            .Validate(new RenameTodoListCommand { TodoListId = 0, Name = "Ev" });

        Assert.Equal(nameof(RenameTodoListCommand.TodoListId), Assert.Single(result.Errors).PropertyName);
    }

    [Fact]
    public void CreateTodo_ReportsEveryInvalidFieldAtOnce()
    {
        CreateTodoCommand command = new CreateTodoCommand
        {
            TodoListId = 1,
            Title = "",
            Description = new string('a', 1001),
            Priority = (TodoPriority)99
        };

        ValidationResult result = new CreateTodoCommandValidator(FakeTimeProvider.AtTestStart()).Validate(command);

        Assert.Equal(
            new[] { nameof(CreateTodoCommand.Title), nameof(CreateTodoCommand.Description), nameof(CreateTodoCommand.Priority) },
            result.Errors.Select(failure => failure.PropertyName).ToArray());
    }

    [Fact]
    public void CreateTodo_AcceptsAMissingDescription()
    {
        ValidationResult result = new CreateTodoCommandValidator(FakeTimeProvider.AtTestStart())
            .Validate(new CreateTodoCommand { TodoListId = 1, Title = "Raporu yaz", Description = null });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateTodo_RejectsADueDateInThePast()
    {
        CreateTodoCommand command = new CreateTodoCommand
        {
            TodoListId = 1,
            Title = "Raporu yaz",
            DueAtUtc = new DateTime(2026, 9, 20, 11, 0, 0, DateTimeKind.Utc)
        };

        ValidationResult result = new CreateTodoCommandValidator(FakeTimeProvider.AtTestStart()).Validate(command);

        ValidationFailure failure = Assert.Single(result.Errors);
        Assert.Equal(nameof(CreateTodoCommand.DueAtUtc), failure.PropertyName);
        Assert.Equal("'Bitiş tarihi' geçmişte olamaz.", failure.ErrorMessage);
    }

    [Fact]
    public void CreateTodo_ToleratesAFewMinutesOfClockDifference()
    {
        CreateTodoCommand command = new CreateTodoCommand
        {
            TodoListId = 1,
            Title = "Raporu yaz",
            DueAtUtc = new DateTime(2026, 9, 20, 11, 57, 0, DateTimeKind.Utc)
        };

        ValidationResult result = new CreateTodoCommandValidator(FakeTimeProvider.AtTestStart()).Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateTodo_RejectsADueDateMoreThanTenYearsAhead()
    {
        CreateTodoCommand command = new CreateTodoCommand
        {
            TodoListId = 1,
            Title = "Raporu yaz",
            DueAtUtc = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc)
        };

        ValidationResult result = new CreateTodoCommandValidator(FakeTimeProvider.AtTestStart()).Validate(command);

        Assert.Equal("'Bitiş tarihi' en fazla 10 yıl sonrası olabilir.", Assert.Single(result.Errors).ErrorMessage);
    }

    [Fact]
    public void UpdateTodo_RejectsADueDateMoreThanTenYearsAhead()
    {
        UpdateTodoCommand command = new UpdateTodoCommand
        {
            TodoListId = 1,
            TodoId = 1,
            Title = "Raporu yaz",
            DueAtUtc = new DateTime(2037, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        ValidationResult result = new UpdateTodoCommandValidator(FakeTimeProvider.AtTestStart()).Validate(command);

        Assert.Equal(nameof(UpdateTodoCommand.DueAtUtc), Assert.Single(result.Errors).PropertyName);
    }

    [Fact]
    public void UpdateTodo_RejectsATitleLongerThan120Characters()
    {
        UpdateTodoCommand command = new UpdateTodoCommand
        {
            TodoListId = 1,
            TodoId = 1,
            Title = new string('a', 121)
        };

        ValidationResult result = new UpdateTodoCommandValidator(FakeTimeProvider.AtTestStart()).Validate(command);

        Assert.Equal(nameof(UpdateTodoCommand.Title), Assert.Single(result.Errors).PropertyName);
    }

    [Theory]
    [InlineData("kisa1")]
    [InlineData("sadeceharf")]
    [InlineData("1234567890")]
    public void Register_RejectsAWeakPassword(string password)
    {
        RegisterWithPasswordCommand command = new RegisterWithPasswordCommand
        {
            Email = "ali@example.com",
            DisplayName = "Ali",
            Password = password
        };

        ValidationResult result = new RegisterWithPasswordCommandValidator().Validate(command);

        Assert.All(result.Errors, failure => Assert.Equal(nameof(command.Password), failure.PropertyName));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Register_RejectsAnInvalidEmail()
    {
        RegisterWithPasswordCommand command = new RegisterWithPasswordCommand
        {
            Email = "bu-bir-eposta-degil",
            DisplayName = "Ali",
            Password = "Sifre1234"
        };

        ValidationResult result = new RegisterWithPasswordCommandValidator().Validate(command);

        Assert.Equal(nameof(command.Email), Assert.Single(result.Errors).PropertyName);
    }

    [Fact]
    public void Login_OnlyRequiresBothFields()
    {
        LoginWithPasswordCommandValidator validator = new LoginWithPasswordCommandValidator();

        Assert.False(validator.Validate(new LoginWithPasswordCommand { Email = "", Password = "" }).IsValid);
        Assert.True(validator.Validate(new LoginWithPasswordCommand { Email = "x", Password = "y" }).IsValid);
    }

    [Fact]
    public void GoogleLogin_RejectsAnOversizedToken()
    {
        ValidationResult result = new LoginWithGoogleCommandValidator()
            .Validate(new LoginWithGoogleCommand { IdToken = new string('a', 5000) });

        Assert.False(result.IsValid);
    }
}
