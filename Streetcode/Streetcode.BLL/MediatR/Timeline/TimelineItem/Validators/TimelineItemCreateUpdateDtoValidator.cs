using FluentValidation;
using Streetcode.BLL.DTO.Timeline;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.Timeline.TimelineItem.Validators;

public sealed class TimelineItemCreateUpdateDtoValidator : AbstractValidator<TimelineItemCreateUpdateDTO>
{
    public TimelineItemCreateUpdateDtoValidator(IValidator<HistoricalContextDTO> historicalContextValidator)
    {
        RuleFor(query => query.StreetcodeId)
            .MustBeValidId("Streetcode");

        RuleFor(query => query.Title)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Timeline item title is required.")
            .MustNotExceedLength(
                TimelineItemCreateUpdateDTO.TitleMaxLength,
                "Timeline item title");

        RuleFor(query => query.Description)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Timeline item description is required.")
            .MustNotExceedLength(
                TimelineItemCreateUpdateDTO.DescriptionMaxLength,
                "Timeline item description");

        RuleFor(item => item.Date)
            .NotEmpty()
            .WithMessage("Timeline item date is required.");

        RuleFor(item => item.DateViewPattern)
            .IsInEnum()
            .WithMessage("Timeline item date view pattern is invalid.");

        RuleFor(item => item.HistoricalContexts)
            .NotNull()
            .WithMessage("Historical contexts collection is required.");

        RuleForEach(item => item.HistoricalContexts)
            .SetValidator(historicalContextValidator);
    }
}
