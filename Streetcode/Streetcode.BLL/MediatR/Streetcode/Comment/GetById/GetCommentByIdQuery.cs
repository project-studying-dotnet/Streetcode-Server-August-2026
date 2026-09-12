using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Streetcode.Comments;

namespace Streetcode.BLL.MediatR.Streetcode.Comment.GetById;

public record GetCommentByIdQuery(int Id)
    : IRequest<Result<CommentWithRepliesDto>>;
