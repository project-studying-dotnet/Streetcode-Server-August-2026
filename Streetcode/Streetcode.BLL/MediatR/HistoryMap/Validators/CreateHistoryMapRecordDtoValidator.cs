using FluentValidation;
using Streetcode.BLL.DTO.HistoryMap;
using Streetcode.BLL.MediatR.Validators;

namespace Streetcode.BLL.MediatR.HistoryMap.Validators
{
    public sealed class CreateHistoryMapRecordDtoValidator
        : AbstractValidator<CreateHistoryMapRecordDTO>
    {
        public CreateHistoryMapRecordDtoValidator(
            IValidator<CreateHistoryMapRecordDTO> dtoValidator)
        {
            RuleFor(x => x.StreetcodeId)
                .MustBeValidId("Streetcode");

            RuleFor(x => x.ToponymId)
                .MustBeValidId("Toponym");

            RuleFor(x => x.PhysicalStreetcodeNumber)
                .GreaterThan(0)
                .WithMessage("Physical streetcode number must be greater than 0.");

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90)
                .WithMessage("Latitude must be between -90 and 90.");

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180)
                .WithMessage("Longitude must be between -180 and 180.");
        }
    }
}