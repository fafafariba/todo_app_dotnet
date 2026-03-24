using Api.Entities;

namespace Api.DTOs.Todos;

public record TodoResponse(
    string Id,
    string Title,
    string? Description,
    Priority Priority,
    DateTime? DueDate,
    DateTime? CompletedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
