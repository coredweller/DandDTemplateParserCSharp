using FluentValidation;
using DandDTemplateParserCSharp.Controllers;

namespace DandDTemplateParserCSharp.Validators;

public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
                .WithMessage("Title is required.")
            .MaximumLength(200)
                .WithMessage("Title must not exceed 200 characters.");
    }
}
