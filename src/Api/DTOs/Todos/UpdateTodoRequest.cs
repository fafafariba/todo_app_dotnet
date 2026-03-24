using Api.Entities;

namespace Api.DTOs.Todos;

public record UpdateTodoRequest(
    string? Title = null,
    string? Description = null,
    Priority? Priority = null,
    DateTime? DueDate = null,
    DateTime? CompletedAt = null
);
