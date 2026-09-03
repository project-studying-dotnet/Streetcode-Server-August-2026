using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Sources;

namespace Streetcode.BLL.MediatR.Sources.StreetcodeCategoryContent.Create;

public record CreateSourceCommand(SourceCreateDTO SourceCreateDto)
    : IRequest<Result<StreetcodeCategoryContentDTO>>;
