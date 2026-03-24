namespace Api.Entities;

public class Todo
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public Priority Priority { get; set; } = Priority.MEDIUM;
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
