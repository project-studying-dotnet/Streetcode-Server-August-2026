using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Streetcode.BLL.DTO.Streetcode.Comments;
using Streetcode.DAL.Repositories.Interfaces.Base;

namespace Streetcode.BLL.MediatR.Streetcode.Comment.GetByStreetcodeId;

public class GetCommentsByStreetcodeIdHandler
    : IRequestHandler<GetCommentsByStreetcodeIdQuery, Result<IEnumerable<CommentWithRepliesDto>>>
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly IMapper _mapper;

    public GetCommentsByStreetcodeIdHandler(IRepositoryWrapper repositoryWrapper, IMapper mapper)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
    }

    public async Task<Result<IEnumerable<CommentWithRepliesDto>>> Handle(
        GetCommentsByStreetcodeIdQuery request,
        CancellationToken cancellationToken)
    {
        var comments = await _repositoryWrapper.CommentRepository.GetAllAsync(
            predicate: comment => comment.StreetcodeId == request.StreetcodeId &&
                comment.ParentCommentId == null,
            include: query => query.Include(comment => comment.Replies
                .OrderBy(reply => reply.CreatedAt)
                .ThenBy(reply => reply.Id)));

        var orderedComments = comments
            .OrderBy(comment => comment.CreatedAt)
            .ThenBy(comment => comment.Id);

        return Result.Ok(_mapper.Map<IEnumerable<CommentWithRepliesDto>>(orderedComments));
    }
}
