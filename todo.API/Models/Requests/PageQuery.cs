using System.ComponentModel.DataAnnotations;
using TodoApi.Application.Contracts;

namespace TodoApi.API.Models.Requests;

public sealed class PageQuery
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;

    [Range(1, PageRequest.MaximumPageSize)]
    public int PageSize { get; init; } = PageRequest.DefaultPageSize;

    public PageRequest ToPageRequest() => new(PageNumber, PageSize);
}
