using TodoApi.Domain.Common;
using TodoApi.Domain.Enums;
using TodoApi.Domain.Exceptions;

namespace TodoApi.Domain.Entities;

public class User : AuditableEntity
{
    public const int MaximumEmailLength = 256;
    public const int MaximumDisplayNameLength = 100;
    public const int MaximumGoogleSubjectIdLength = 255;

    private int _id;
    private string _email = string.Empty;
    private string _displayName = string.Empty;
    private string? _passwordHash;
    private string? _googleSubjectId;
    private UserRole _role = UserRole.User;

    private User(string email, string displayName, UserRole role)
    {
        Email = ValidateAndNormalizeEmail(email);
        DisplayName = ValidateAndNormalizeDisplayName(displayName);
        Role = ValidateRole(role);
    }

    public int Id
    {
        get
        {
            return _id;
        }
        private set
        {
            _id = value;
        }
    }

    public string Email
    {
        get
        {
            return _email;
        }
        private set
        {
            _email = value;
        }
    }

    public string DisplayName
    {
        get
        {
            return _displayName;
        }
        private set
        {
            _displayName = value;
        }
    }

    public string? PasswordHash
    {
        get
        {
            return _passwordHash;
        }
        private set
        {
            _passwordHash = value;
        }
    }

    public string? GoogleSubjectId
    {
        get
        {
            return _googleSubjectId;
        }
        private set
        {
            _googleSubjectId = value;
        }
    }

    public UserRole Role
    {
        get
        {
            return _role;
        }
        private set
        {
            _role = value;
        }
    }

    public bool HasPassword
    {
        get
        {
            return PasswordHash != null;
        }
    }

    public bool HasGoogleLogin
    {
        get
        {
            return GoogleSubjectId != null;
        }
    }

    public bool IsAdmin
    {
        get
        {
            return Role == UserRole.Admin;
        }
    }

    public static User RegisterWithPassword(
        string email,
        string displayName,
        string passwordHash)
    {
        User user = new User(email, displayName, UserRole.User);
        user.PasswordHash = ValidatePasswordHash(passwordHash);

        return user;
    }

    public static User RegisterWithGoogle(
        string email,
        string displayName,
        string googleSubjectId,
        UserRole role)
    {
        User user = new User(email, displayName, role);
        user.GoogleSubjectId = ValidateGoogleSubjectId(googleSubjectId);

        return user;
    }

    public void LinkGoogleAccount(string googleSubjectId)
    {
        string validatedGoogleSubjectId = ValidateGoogleSubjectId(googleSubjectId);

        if (GoogleSubjectId == validatedGoogleSubjectId)
        {
            return;
        }

        if (GoogleSubjectId != null)
        {
            throw new DomainRuleViolationException(
                "This user is already linked to a different Google account.");
        }

        GoogleSubjectId = validatedGoogleSubjectId;
        PasswordHash = null;
    }

    public void ReplacePasswordHash(string passwordHash)
    {
        if (HasPassword == false)
        {
            throw new DomainRuleViolationException(
                "A password hash can only be replaced for an account that has a password.");
        }

        PasswordHash = ValidatePasswordHash(passwordHash);
    }

    public void PromoteToAdmin()
    {
        if (HasGoogleLogin == false)
        {
            throw new DomainRuleViolationException(
                "Only an account whose email address was verified by Google can become an admin.");
        }

        Role = UserRole.Admin;
    }

    private static string ValidateAndNormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainRuleViolationException(
                "An email address cannot be empty.");
        }

        string normalizedEmail = EmailNormalizer.Normalize(email);

        if (normalizedEmail.Length > MaximumEmailLength)
        {
            throw new DomainRuleViolationException(
                $"An email address cannot exceed {MaximumEmailLength} characters.");
        }

        int atSignIndex = normalizedEmail.IndexOf('@');
        bool hasTextBeforeAtSign = atSignIndex > 0;
        bool hasTextAfterAtSign = atSignIndex < normalizedEmail.Length - 1;
        bool hasSingleAtSign = atSignIndex == normalizedEmail.LastIndexOf('@');

        if (hasTextBeforeAtSign == false || hasTextAfterAtSign == false || hasSingleAtSign == false)
        {
            throw new DomainRuleViolationException(
                "The email address is not valid.");
        }

        return normalizedEmail;
    }

    private static string ValidateAndNormalizeDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainRuleViolationException(
                "A display name cannot be empty.");
        }

        string normalizedDisplayName = displayName.Trim();

        if (normalizedDisplayName.Length > MaximumDisplayNameLength)
        {
            throw new DomainRuleViolationException(
                $"A display name cannot exceed {MaximumDisplayNameLength} characters.");
        }

        return normalizedDisplayName;
    }

    private static string ValidatePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainRuleViolationException(
                "A password hash cannot be empty.");
        }

        return passwordHash;
    }

    private static string ValidateGoogleSubjectId(string googleSubjectId)
    {
        if (string.IsNullOrWhiteSpace(googleSubjectId))
        {
            throw new DomainRuleViolationException(
                "A Google account id cannot be empty.");
        }

        string normalizedGoogleSubjectId = googleSubjectId.Trim();

        if (normalizedGoogleSubjectId.Length > MaximumGoogleSubjectIdLength)
        {
            throw new DomainRuleViolationException(
                $"A Google account id cannot exceed {MaximumGoogleSubjectIdLength} characters.");
        }

        return normalizedGoogleSubjectId;
    }

    private static UserRole ValidateRole(UserRole role)
    {
        bool isDefinedRole = Enum.IsDefined(typeof(UserRole), role);

        if (isDefinedRole == false)
        {
            throw new DomainRuleViolationException(
                "The selected user role is not supported.");
        }

        return role;
    }
}
