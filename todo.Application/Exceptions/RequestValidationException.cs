using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;

namespace TodoApi.Application.Exceptions;

public sealed class RequestValidationException : Exception
{
    private readonly IReadOnlyDictionary<string, string[]> _errors;

    public RequestValidationException(IEnumerable<ValidationFailure> failures)
        : base("One or more validation errors occurred.")
    {
        if (failures == null)
        {
            throw new ArgumentNullException(nameof(failures));
        }

        _errors = failures
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());
    }

    public IReadOnlyDictionary<string, string[]> Errors
    {
        get
        {
            return _errors;
        }
    }
}
