using Api.DTOs.Todos;
using FluentValidation;

namespace Api.Validators;

public class CreateTodoRequestValidator : AbstractValidator<CreateTodoRequest>
{
    public CreateTodoRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.Priority).IsInEnum();
    }
}
