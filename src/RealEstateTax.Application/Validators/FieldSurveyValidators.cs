using FluentValidation;
using RealEstateTax.Application.DTOs.FieldSurveys;

namespace RealEstateTax.Application.Validators;

public class CreateFieldSurveyRequestValidator : AbstractValidator<CreateFieldSurveyRequest>
{
    public CreateFieldSurveyRequestValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.AssignedToUserId).NotEmpty();

        RuleFor(x => x.ScheduledDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("Scheduled date cannot be in the past.")
            .When(x => x.ScheduledDate.HasValue);
    }
}

public class UpdateFieldSurveyRequestValidator : AbstractValidator<UpdateFieldSurveyRequest>
{
    public UpdateFieldSurveyRequestValidator()
    {
        RuleFor(x => x.MeasuredArea)
            .GreaterThan(0)
            .LessThanOrEqualTo(1_000_000)
            .When(x => x.MeasuredArea.HasValue);

        RuleFor(x => x.NumberOfFloorsObserved)
            .InclusiveBetween(1, 200)
            .When(x => x.NumberOfFloorsObserved.HasValue);

        RuleFor(x => x.FieldNotes)
            .MaximumLength(5000)
            .When(x => !string.IsNullOrEmpty(x.FieldNotes));

        RuleFor(x => x.OccupantInfo)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.OccupantInfo));

        // GPS coordinates within Egypt bounding box
        RuleFor(x => x.GpsLatitude)
            .InclusiveBetween(22.0, 31.9)
            .WithMessage("GPS latitude must be within Egypt's geographic bounds (22°N – 31.9°N).")
            .When(x => x.GpsLatitude.HasValue);

        RuleFor(x => x.GpsLongitude)
            .InclusiveBetween(24.7, 37.1)
            .WithMessage("GPS longitude must be within Egypt's geographic bounds (24.7°E – 37.1°E).")
            .When(x => x.GpsLongitude.HasValue);

        // Require both lat and lon together
        RuleFor(x => x.GpsLongitude)
            .NotNull()
            .WithMessage("GpsLongitude is required when GpsLatitude is provided.")
            .When(x => x.GpsLatitude.HasValue && !x.GpsLongitude.HasValue);

        RuleFor(x => x.GpsLatitude)
            .NotNull()
            .WithMessage("GpsLatitude is required when GpsLongitude is provided.")
            .When(x => x.GpsLongitude.HasValue && !x.GpsLatitude.HasValue);

        RuleFor(x => x.GpsAccuracy)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(500)  // meters; accuracy > 500m is unreliable
            .WithMessage("GPS accuracy must be between 0 and 500 metres.")
            .When(x => x.GpsAccuracy.HasValue);
    }
}
