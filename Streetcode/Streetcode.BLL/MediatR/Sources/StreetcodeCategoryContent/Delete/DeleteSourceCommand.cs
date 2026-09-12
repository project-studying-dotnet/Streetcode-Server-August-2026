using FluentResults;
using MediatR;

namespace Streetcode.BLL.MediatR.Sources.StreetcodeCategoryContent.Delete;

public record DeleteSourceCommand(
    int StreetcodeId,
    int SourceLinkCategoryId)
    : IRequest<Result<Unit>>;
