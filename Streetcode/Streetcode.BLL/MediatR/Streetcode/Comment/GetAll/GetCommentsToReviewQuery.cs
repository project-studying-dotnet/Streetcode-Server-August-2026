using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Streetcode.Comments;

namespace Streetcode.BLL.MediatR.Streetcode.Comment.GetAll;

public record GetCommentsToReviewQuery(GetCommentsToReviewRequestDto Request)
    : IRequest<Result<GetCommentsToReviewResponseDto>>;
