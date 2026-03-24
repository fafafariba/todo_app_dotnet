using Api.DTOs.Todos;

namespace Api.Services;

public interface ITodoService
{
    Task<List<TodoResponse>> ListAsync(string userId);
    Task<TodoResponse> CreateAsync(string userId, CreateTodoRequest request);
    Task<TodoResponse?> UpdateAsync(string userId, string id, UpdateTodoRequest request, HashSet<string> explicitFields);
    Task<string?> DeleteAsync(string userId, string id);
}
