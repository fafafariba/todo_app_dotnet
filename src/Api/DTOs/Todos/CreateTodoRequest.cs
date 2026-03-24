using Api.Entities;

namespace Api.DTOs.Todos;

public record CreateTodoRequest(
    string Title,
    string? Description = null,
    Priority Priority = Priority.MEDIUM,
    DateTime? DueDate = null
);
