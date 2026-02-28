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
            .InclusiveBetween(1, 30).WithMessage("Level must be between 1 and 30.");

        RuleFor(x => x.SavingThrows)
            .Must(d => d is null || d.Count <= 10)
            .WithMessage("SavingThrows must not exceed 10 entries.");

        RuleFor(x => x.Skills)
            .Must(d => d is null || d.Count <= 10)
            .WithMessage("Skills must not exceed 10 entries.");

        RuleFor(x => x.SpecialTraits)
            .Must(d => d is null || d.Count <= 10)
            .WithMessage("SpecialTraits must not exceed 10 entries.");

        RuleFor(x => x.Actions)
            .Must(d => d is null || d.Count <= 10)
            .WithMessage("Actions must not exceed 10 entries.");
    }
}

public sealed class LegendarySheetRequestValidator : AbstractValidator<LegendarySheetRequest>
{
    public LegendarySheetRequestValidator()
    {
        // Reuse all general rules
        Include(new GeneralSheetRequestValidator());

        RuleFor(x => x.BonusActions)
            .Must(d => d is null || d.Count <= 10)
            .WithMessage("BonusActions must not exceed 10 entries.");

        RuleFor(x => x.Reactions)
            .Must(d => d is null || d.Count <= 10)
            .WithMessage("Reactions must not exceed 10 entries.");

        RuleFor(x => x.LegendaryTraits)
            .Must(d => d is null || d.Count <= 10)
            .WithMessage("LegendaryTraits must not exceed 10 entries.");

        RuleFor(x => x.LairActions)
            .Must(d => d is null || d.Count <= 10)
            .WithMessage("LairActions must not exceed 10 entries.");

        RuleFor(x => x.LegendaryActions)
            .Must(la => la is null || la.Options is null || la.Options.Count <= 10)
            .WithMessage("LegendaryActions.Options must not exceed 10 entries.");
    }
}
