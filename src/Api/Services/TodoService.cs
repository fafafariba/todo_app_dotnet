using Api.Data;
using Api.DTOs.Todos;
using Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class TodoService : ITodoService
{
    private readonly AppDbContext _db;

    public TodoService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<TodoResponse>> ListAsync(string userId)
    {
        var todos = await _db.Todos
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return todos.Select(ToResponse).ToList();
    }

    public async Task<TodoResponse> CreateAsync(string userId, CreateTodoRequest request)
    {
        var todo = new Todo
        {
            UserId = userId,
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            DueDate = request.DueDate,
        };

        _db.Todos.Add(todo);
        await _db.SaveChangesAsync();

        Console.WriteLine("[todos] Created: todoId={0} userId={1}", todo.Id, userId);
        return ToResponse(todo);
    }

    public async Task<TodoResponse?> UpdateAsync(string userId, string id, UpdateTodoRequest request, HashSet<string> explicitFields)
    {
        var todo = await _db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (todo is null) return null;

        if (request.Title is not null) todo.Title = request.Title;
        if (request.Description is not null) todo.Description = request.Description;
        if (request.Priority is not null) todo.Priority = request.Priority.Value;

        // Nullable fields: only update if explicitly sent (even as null, to clear the value)
        if (explicitFields.Contains("dueDate")) todo.DueDate = request.DueDate;
        if (explicitFields.Contains("completedAt")) todo.CompletedAt = request.CompletedAt;

        await _db.SaveChangesAsync();

        Console.WriteLine("[todos] Updated: todoId={0} userId={1}", id, userId);
        return ToResponse(todo);
    }

    public async Task<string?> DeleteAsync(string userId, string id)
    {
        var todo = await _db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (todo is null) return null;

        todo.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        Console.WriteLine("[todos] Deleted: todoId={0} userId={1}", id, userId);
        return todo.Id;
    }

    private static TodoResponse ToResponse(Todo t) => new(
        t.Id, t.Title, t.Description, t.Priority,
        t.DueDate, t.CompletedAt, t.CreatedAt, t.UpdatedAt
    );
}
