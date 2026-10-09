using TodoApi.Domain.Enums;

namespace TodoApi.API.Models.Requests;

public sealed class UpdateTodoRequest
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime? DueAtUtc { get; set; }

    public TodoPriority Priority { get; set; } = TodoPriority.Normal;
}
