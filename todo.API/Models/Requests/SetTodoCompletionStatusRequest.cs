using System.ComponentModel.DataAnnotations;

namespace TodoApi.API.Models.Requests;

public sealed class SetTodoCompletionStatusRequest
{
    [Required]
    public bool? IsCompleted { get; set; }
}
