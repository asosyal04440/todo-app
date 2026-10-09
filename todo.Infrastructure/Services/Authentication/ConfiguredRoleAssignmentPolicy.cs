using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Domain.Enums;

namespace TodoApi.Infrastructure.Services.Authentication;

public sealed class ConfiguredRoleAssignmentPolicy : IRoleAssignmentPolicy
{
    public const string AdminEmailsSectionName = "Authorization:AdminEmails";

    private readonly HashSet<string> _adminEmails;

    public ConfiguredRoleAssignmentPolicy(IEnumerable<string> adminEmails)
    {
        if (adminEmails == null)
        {
            throw new ArgumentNullException(nameof(adminEmails));
        }

        _adminEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string adminEmail in adminEmails)
        {
            if (string.IsNullOrWhiteSpace(adminEmail) == false)
            {
                _adminEmails.Add(adminEmail.Trim());
            }
        }
    }

    public UserRole GetRoleForEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return UserRole.User;
        }

        bool isAdminEmail = _adminEmails.Contains(email.Trim());

        if (isAdminEmail)
        {
            return UserRole.Admin;
        }

        return UserRole.User;
    }

    public static ConfiguredRoleAssignmentPolicy FromConfiguration(IConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        List<string> adminEmails = new List<string>();

        foreach (IConfigurationSection child in configuration.GetSection(AdminEmailsSectionName).GetChildren())
        {
            if (child.Value != null)
            {
                adminEmails.Add(child.Value);
            }
        }

        ConfiguredRoleAssignmentPolicy policy = new ConfiguredRoleAssignmentPolicy(adminEmails);

        return policy;
    }
}
