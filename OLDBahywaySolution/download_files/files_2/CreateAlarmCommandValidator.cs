using FluentValidation;

namespace AlarmInsight.Application.Alarms.Commands.CreateAlarm;

/// <summary>
/// Validator for CreateAlarmCommand using FluentValidation.
/// Validates input BEFORE it reaches the handler.
/// </summary>
public sealed class CreateAlarmCommandValidator : AbstractValidator<CreateAlarmCommand>
{
    public CreateAlarmCommandValidator()
    {
        RuleFor(x => x.Source)
            .NotEmpty().WithMessage("Alarm source is required")
            .MaximumLength(200).WithMessage("Source cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Alarm description is required")
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

        RuleFor(x => x.SeverityValue)
            .InclusiveBetween(1, 4)
            .WithMessage("Severity must be between 1 (Low) and 4 (Critical)");

        RuleFor(x => x.LocationName)
            .NotEmpty().WithMessage("Location name is required")
            .MaximumLength(200).WithMessage("Location name cannot exceed 200 characters");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Latitude must be between -90 and 90");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Longitude must be between -180 and 180");
    }
}
