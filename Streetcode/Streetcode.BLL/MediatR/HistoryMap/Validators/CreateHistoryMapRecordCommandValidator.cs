using FluentValidation;
using Streetcode.BLL.DTO.HistoryMap;
using Streetcode.BLL.MediatR.HistoryMap.Create;

namespace Streetcode.BLL.MediatR.HistoryMap.Validators
{
    public sealed class CreateHistoryMapRecordCommandValidator
        : AbstractValidator<CreateHistoryMapRecordCommand>
    {
        public CreateHistoryMapRecordCommandValidator(
            IValidator<CreateHistoryMapRecordDTO> dtoValidator)
        {
            RuleFor(command => command.Dto)
                .NotNull()
                .WithMessage("Create history map record DTO is required.")
                .SetValidator(dtoValidator);
        }
    }
}