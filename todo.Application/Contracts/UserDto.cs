using System;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;

namespace TodoApi.Application.Contracts;

public class UserDto
{
    private readonly int _id;
    private readonly string _email;
    private readonly string _displayName;
    private readonly UserRole _role;
    private readonly bool _hasPassword;
    private readonly bool _hasGoogleLogin;
    private readonly DateTime _createdAtUtc;

    public UserDto(User user)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        _id = user.Id;
        _email = user.Email;
        _displayName = user.DisplayName;
        _role = user.Role;
        _hasPassword = user.HasPassword;
        _hasGoogleLogin = user.HasGoogleLogin;
        _createdAtUtc = user.CreatedAtUtc;
    }

    public UserDto(int id, string email, string displayName, UserRole role,
        bool hasPassword, bool hasGoogleLogin, DateTime createdAtUtc)
    {
        _id = id;
        _email = email;
        _displayName = displayName;
        _role = role;
        _hasPassword = hasPassword;
        _hasGoogleLogin = hasGoogleLogin;
        _createdAtUtc = createdAtUtc;
    }

    public int Id
    {
        get
        {
            return _id;
        }
    }

    public string Email
    {
        get
        {
            return _email;
        }
    }

    public string DisplayName
    {
        get
        {
            return _displayName;
        }
    }

    public UserRole Role
    {
        get
        {
            return _role;
        }
    }

    public bool HasPassword
    {
        get
        {
            return _hasPassword;
        }
    }

    public bool HasGoogleLogin
    {
        get
        {
            return _hasGoogleLogin;
        }
    }

    public DateTime CreatedAtUtc
    {
        get
        {
            return _createdAtUtc;
        }
    }
}
