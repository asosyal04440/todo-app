using FluentValidation.Results;
using TodoApi.Application.Exceptions;

namespace TodoApi.Application.Contracts;

public sealed class PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;

    public PageRequest(int pageNumber = 1, int pageSize = DefaultPageSize)
    {
        List<ValidationFailure> failures = [];
        if (pageNumber < 1)
            failures.Add(new ValidationFailure(nameof(PageNumber), "Page number must be at least 1."));
        if (pageSize < 1 || pageSize > MaximumPageSize)
            failures.Add(new ValidationFailure(nameof(PageSize), $"Page size must be between 1 and {MaximumPageSize}."));
        long offset = ((long)pageNumber - 1) * pageSize;
        if (offset > int.MaxValue)
            failures.Add(new ValidationFailure(nameof(PageNumber), "The requested page is too large."));
        if (failures.Count > 0) throw new RequestValidationException(failures);

        PageNumber = pageNumber;
        PageSize = pageSize;
        Offset = (int)offset;
    }

    public int PageNumber { get; }
    public int PageSize { get; }
    public int Offset { get; }
}
