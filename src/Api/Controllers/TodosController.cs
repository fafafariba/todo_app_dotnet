using System.Text.Json;
using Api.DTOs.Todos;
using Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/todos")]
[Authorize]
public class TodosController : ControllerBase
{
    private readonly ITodoService _todoService;
    private readonly IValidator<CreateTodoRequest> _createValidator;
    private readonly IValidator<UpdateTodoRequest> _updateValidator;

    public TodosController(
        ITodoService todoService,
        IValidator<CreateTodoRequest> createValidator,
        IValidator<UpdateTodoRequest> updateValidator)
    {
        _todoService = todoService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    private string UserId => User.FindFirst("userId")!.Value;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var todos = await _todoService.ListAsync(UserId);
        return Ok(new { data = todos });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTodoRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { error = validation.Errors });

        var todo = await _todoService.CreateAsync(UserId, request);
        return Created("", new { data = todo });
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] JsonElement body)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var request = body.Deserialize<UpdateTodoRequest>(options) ?? new UpdateTodoRequest();

        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(new { error = validation.Errors });

        // Track which fields were explicitly sent (even as null) so the service
        // can distinguish "clear this field" from "don't touch this field"
        var explicitFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in body.EnumerateObject())
            explicitFields.Add(prop.Name);

        var todo = await _todoService.UpdateAsync(UserId, id, request, explicitFields);
        if (todo is null)
            return NotFound(new { error = "Todo not found" });

        return Ok(new { data = todo });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deletedId = await _todoService.DeleteAsync(UserId, id);
        if (deletedId is null)
            return NotFound(new { error = "Todo not found" });

        return Ok(new { data = new { id = deletedId } });
    }
}
