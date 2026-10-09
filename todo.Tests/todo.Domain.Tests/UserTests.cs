using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;
using TodoApi.Domain.Exceptions;

namespace todo.Domain.Tests;

public class UserTests
{
    [Fact]
    public void RegisterWithPassword_NormalizesTheEmailAndStoresTheHash()
    {
        User user = User.RegisterWithPassword(
            "  Ali.Veli@Example.COM ",
            "  Ali Veli  ",
            "hash-value");

        Assert.Equal("ali.veli@example.com", user.Email);
        Assert.Equal("Ali Veli", user.DisplayName);
        Assert.Equal("hash-value", user.PasswordHash);
        Assert.True(user.HasPassword);
        Assert.False(user.HasGoogleLogin);
        Assert.False(user.IsAdmin);
    }

    [Fact]
    public void RegisterWithGoogle_StoresTheGoogleAccountWithoutAPassword()
    {
        User user = User.RegisterWithGoogle(
            "ayse@gmail.com",
            "Ayşe",
            "google-subject-1",
            UserRole.User);

        Assert.Equal("google-subject-1", user.GoogleSubjectId);
        Assert.True(user.HasGoogleLogin);
        Assert.False(user.HasPassword);
        Assert.Null(user.PasswordHash);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-at-sign")]
    [InlineData("@example.com")]
    [InlineData("ali@")]
    [InlineData("ali@@example.com")]
    public void RegisterWithPassword_RejectsAnInvalidEmail(string email)
    {
        Assert.Throws<DomainRuleViolationException>(
            () => User.RegisterWithPassword(email, "Ali", "hash-value"));
    }

    [Fact]
    public void RegisterWithPassword_RejectsAnEmptyPasswordHash()
    {
        Assert.Throws<DomainRuleViolationException>(
            () => User.RegisterWithPassword("ali@example.com", "Ali", " "));
    }

    [Fact]
    public void RegisterWithGoogle_RejectsAnUnsupportedRole()
    {
        Assert.Throws<DomainRuleViolationException>(
            () => User.RegisterWithGoogle("ali@gmail.com", "Ali", "google-subject-1", (UserRole)42));
    }

    [Fact]
    public void LinkGoogleAccount_DiscardsThePasswordThatWasSetBeforeGoogleProvedTheEmail()
    {
        User user = User.RegisterWithPassword("ali@gmail.com", "Ali", "hash-value");

        user.LinkGoogleAccount("google-subject-1");

        Assert.False(user.HasPassword);
        Assert.Null(user.PasswordHash);
        Assert.True(user.HasGoogleLogin);
        Assert.Equal("google-subject-1", user.GoogleSubjectId);
    }

    [Fact]
    public void LinkGoogleAccount_DoesNothingWhenTheSameGoogleAccountIsAlreadyLinked()
    {
        User user = User.RegisterWithGoogle("ali@gmail.com", "Ali", "google-subject-1", UserRole.User);

        user.LinkGoogleAccount("google-subject-1");

        Assert.Equal("google-subject-1", user.GoogleSubjectId);
    }

    [Fact]
    public void LinkGoogleAccount_RejectsADifferentGoogleAccount()
    {
        User user = User.RegisterWithGoogle("ali@gmail.com", "Ali", "google-subject-1", UserRole.User);

        Assert.Throws<DomainRuleViolationException>(
            () => user.LinkGoogleAccount("google-subject-2"));
    }

    [Fact]
    public void RegisterWithPassword_AlwaysCreatesANormalUser()
    {
        User user = User.RegisterWithPassword("patron@example.com", "Patron", "hash-value");

        Assert.Equal(UserRole.User, user.Role);
    }

    [Fact]
    public void PromoteToAdmin_GivesAGoogleVerifiedUserTheAdminRole()
    {
        User user = User.RegisterWithGoogle("ali@gmail.com", "Ali", "google-subject-1", UserRole.User);

        user.PromoteToAdmin();

        Assert.Equal(UserRole.Admin, user.Role);
        Assert.True(user.IsAdmin);
    }

    [Fact]
    public void PromoteToAdmin_RejectsAnAccountWhoseEmailWasNeverVerified()
    {
        User user = User.RegisterWithPassword("patron@example.com", "Patron", "hash-value");

        Assert.Throws<DomainRuleViolationException>(() => user.PromoteToAdmin());

        Assert.False(user.IsAdmin);
    }
}
