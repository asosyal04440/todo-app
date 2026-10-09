using System;

namespace TodoApi.Application.Abstractions.Authentication;

public sealed class GoogleUserInfo
{
    private readonly string _subjectId;
    private readonly string _email;
    private readonly bool _isEmailVerified;
    private readonly string? _name;

    public GoogleUserInfo(string subjectId, string email, bool isEmailVerified, string? name)
    {
        if (string.IsNullOrWhiteSpace(subjectId))
        {
            throw new ArgumentException("A Google subject id cannot be empty.", nameof(subjectId));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("A Google email cannot be empty.", nameof(email));
        }

        _subjectId = subjectId;
        _email = email;
        _isEmailVerified = isEmailVerified;
        _name = name;
    }

    public string SubjectId
    {
        get
        {
            return _subjectId;
        }
    }

    public string Email
    {
        get
        {
            return _email;
        }
    }

    public bool IsEmailVerified
    {
        get
        {
            return _isEmailVerified;
        }
    }

    public string? Name
    {
        get
        {
            return _name;
        }
    }
}
