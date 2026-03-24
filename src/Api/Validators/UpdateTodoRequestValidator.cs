using Api.DTOs.Todos;
using FluentValidation;

namespace Api.Validators;

public class UpdateTodoRequestValidator : AbstractValidator<UpdateTodoRequest>
{
    public UpdateTodoRequestValidator()
    {
        RuleFor(x => x.Title).MinimumLength(1).When(x => x.Title is not null);
        RuleFor(x => x.Priority).IsInEnum().When(x => x.Priority is not null);
        // Note: "at least one field" validation is not enforced here because
        // nullable fields (e.g. completedAt: null) are indistinguishable from
        // absent fields after JSON deserialization. The service handles no-op updates gracefully.
    }
}
