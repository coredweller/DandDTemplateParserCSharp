using FluentValidation;
using DandDTemplateParserCSharp.Controllers;

namespace DandDTemplateParserCSharp.Validators;

public sealed class GeneralSheetRequestValidator : AbstractValidator<GeneralSheetRequest>
{
    public GeneralSheetRequestValidator()
    {
        RuleFor(x => x.CharacterName)
            .NotEmpty().WithMessage("CharacterName is required.")
            .MaximumLength(255).WithMessage("CharacterName must not exceed 255 characters.");

        RuleFor(x => x.Level)
            .InclusiveBetween(1, 20).WithMessage("Level must be between 1 and 20.");
    }
}

public sealed class LegendarySheetRequestValidator : AbstractValidator<LegendarySheetRequest>
{
    public LegendarySheetRequestValidator()
    {
        // Reuse all general rules
        Include(new GeneralSheetRequestValidator());
    }
}
