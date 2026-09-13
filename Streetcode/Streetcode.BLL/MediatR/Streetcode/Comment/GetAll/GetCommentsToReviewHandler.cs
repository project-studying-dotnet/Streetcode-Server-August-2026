using AutoMapper;
using FluentResults;
using MediatR;
using Streetcode.BLL.DTO.Streetcode.Comments;
using Streetcode.DAL.Repositories.Interfaces.Base;
using CommentEntity = Streetcode.DAL.Entities.Streetcode.Comment;

namespace Streetcode.BLL.MediatR.Streetcode.Comment.GetAll;

public class GetCommentsToReviewHandler
    : IRequestHandler<GetCommentsToReviewQuery, Result<GetCommentsToReviewResponseDto>>
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly IMapper _mapper;

    public GetCommentsToReviewHandler(
        IRepositoryWrapper repositoryWrapper,
        IMapper mapper)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
    }

    public Task<Result<GetCommentsToReviewResponseDto>> Handle(
        GetCommentsToReviewQuery request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var commentsQuery = _repositoryWrapper.CommentRepository
            .FindAll(comment => comment.ParentCommentId == null)
            .OrderByDescending(comment => comment.CreatedAt)
            .ThenByDescending(comment => comment.Id);

        int totalComments = commentsQuery.Count();
        int totalPages = (int)Math.Ceiling(
            totalComments / (double)request.Request.Amount);
        long commentsToSkip = ((long)request.Request.Page - 1) * request.Request.Amount;

        var comments = commentsToSkip > int.MaxValue
            ? new List<CommentEntity>()
            : commentsQuery
                .Skip((int)commentsToSkip)
                .Take(request.Request.Amount)
                .ToList();

        var response = new GetCommentsToReviewResponseDto
        {
            Pages = totalPages,
            Comments = _mapper.Map<IEnumerable<CommentDto>>(comments),
        };

        return Task.FromResult(Result.Ok(response));
    }
}
