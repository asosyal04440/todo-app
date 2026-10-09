using TodoApi.Domain.Exceptions;

namespace TodoApi.Domain.Entities;

public class RefreshToken
{
    private int _id;
    private int _userId;
    private string _tokenHash = string.Empty;
    private DateTime _createdAtUtc;
    private DateTime _expiresAtUtc;
    private DateTime? _revokedAtUtc;
    private string? _replacedByTokenHash;

    public RefreshToken(int userId, string tokenHash, DateTime createdAtUtc, DateTime expiresAtUtc)
    {
        if (userId <= 0)
        {
            throw new DomainRuleViolationException(
                "A refresh token must belong to a saved user.");
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new DomainRuleViolationException(
                "A refresh token hash cannot be empty.");
        }

        if (expiresAtUtc <= createdAtUtc)
        {
            throw new DomainRuleViolationException(
                "A refresh token must expire after it is created.");
        }

        UserId = userId;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
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

    public int UserId
    {
        get
        {
            return _userId;
        }
        private set
        {
            _userId = value;
        }
    }

    public string TokenHash
    {
        get
        {
            return _tokenHash;
        }
        private set
        {
            _tokenHash = value;
        }
    }

    public DateTime CreatedAtUtc
    {
        get
        {
            return _createdAtUtc;
        }
        private set
        {
            _createdAtUtc = value;
        }
    }

    public DateTime ExpiresAtUtc
    {
        get
        {
            return _expiresAtUtc;
        }
        private set
        {
            _expiresAtUtc = value;
        }
    }

    public DateTime? RevokedAtUtc
    {
        get
        {
            return _revokedAtUtc;
        }
        private set
        {
            _revokedAtUtc = value;
        }
    }

    public string? ReplacedByTokenHash
    {
        get
        {
            return _replacedByTokenHash;
        }
        private set
        {
            _replacedByTokenHash = value;
        }
    }

    public bool IsRevoked
    {
        get
        {
            return RevokedAtUtc != null;
        }
    }

    public bool IsActive(DateTime nowUtc)
    {
        return IsRevoked == false && nowUtc < ExpiresAtUtc;
    }

    public void Revoke(DateTime nowUtc, string? replacedByTokenHash = null)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAtUtc = nowUtc;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
