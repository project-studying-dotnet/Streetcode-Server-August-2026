using AutoMapper;
using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Streetcode.TextContent.Text;
using Streetcode.BLL.Interfaces.Logging;
using Streetcode.DAL.Repositories.Interfaces.Base;

using Entity = Streetcode.DAL.Entities.Streetcode.TextContent.Text;

namespace Streetcode.BLL.MediatR.Streetcode.Text.Create
{
    public class CreateTextHandler : IRequestHandler<CreateTextCommand, Result<TextDTO>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IMapper _mapper;
        private readonly ILoggerService _logger;

        public CreateTextHandler(IRepositoryWrapper repository, IMapper mapper, ILoggerService logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<TextDTO>> Handle(CreateTextCommand request, CancellationToken cancellationToken)
        {
            var streetcodeExists = await _repository.StreetcodeRepository
                .GetFirstOrDefaultAsync(sc => sc.Id == request.TextCreateDto.StreetcodeId);

            if (streetcodeExists is null)
            {
                const string errorMsg = "Cannot create text: streetcode with the given id does not exist!";
                _logger.LogError(request, errorMsg);
                return Result.Fail(new Error(errorMsg));
            }

            var existingText = await _repository.TextRepository
                .GetFirstOrDefaultAsync(t => t.StreetcodeId == request.TextCreateDto.StreetcodeId);

            if (existingText is not null)
            {
                const string errorMsg = "Cannot create text: streetcode with the given id already has a text!";
                _logger.LogError(request, errorMsg);
                return Result.Fail(new Error(errorMsg));
            }

            var text = _mapper.Map<Entity>(request.TextCreateDto);

            if (text is null)
            {
                const string errorMsg = "Cannot create new text!";
                _logger.LogError(request, errorMsg);
                return Result.Fail(new Error(errorMsg));
            }

            var createdText = _repository.TextRepository.Create(text);

            var isSuccessResult = await _repository.SaveChangesAsync() > 0;

            if (!isSuccessResult)
            {
                const string errorMsg = "Cannot save changes in the database after text creation!";
                _logger.LogError(request, errorMsg);
                return Result.Fail(new Error(errorMsg));
            }

            var createdTextDTO = _mapper.Map<TextDTO>(createdText);

            if (createdTextDTO != null)
            {
                return Result.Ok(createdTextDTO);
            }
            else
            {
                const string errorMsg = "Cannot map entity!";
                _logger.LogError(request, errorMsg);
                return Result.Fail(new Error(errorMsg));
            }
        }
    }
}