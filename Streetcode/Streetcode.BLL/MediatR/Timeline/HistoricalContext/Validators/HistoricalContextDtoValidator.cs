using FluentValidation;
using Streetcode.BLL.DTO.Timeline;
using Streetcode.BLL.MediatR.Validators;
using HistoricalContextEntity =
    Streetcode.DAL.Entities.Timeline.HistoricalContext;

namespace Streetcode.BLL.MediatR.Timeline.HistoricalContext.Validators;

public sealed class HistoricalContextDtoValidator
    : AbstractValidator<HistoricalContextDTO>
{
    public HistoricalContextDtoValidator()
    {
        RuleFor(context => context.Id)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Historical context ID cannot be negative.");

        When(
            context => context.Id == 0,
            () =>
            {
                RuleFor(context => context.Title)
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty()
                    .WithMessage("Historical context title is required.")
                    .MustNotExceedLength(
                        HistoricalContextEntity.TitleMaxLength,
                        "Historical context title")
                    .Matches(@"^[\p{L}\s]+$")
                    .WithMessage(
                        "Historical context title can contain only letters and spaces.");
            });
    }
}