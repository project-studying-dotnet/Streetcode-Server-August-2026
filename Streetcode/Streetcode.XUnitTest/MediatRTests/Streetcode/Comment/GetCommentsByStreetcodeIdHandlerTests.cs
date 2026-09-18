using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Streetcode.BLL.DTO.Streetcode.Comments;
using Streetcode.BLL.MediatR.Streetcode.Comment.GetByStreetcodeId;
using Streetcode.DAL.Repositories.Interfaces.Base;
using Streetcode.DAL.Repositories.Interfaces.Streetcode;
using Xunit;
using CommentEntity = Streetcode.DAL.Entities.Streetcode.Comment;

namespace Streetcode.XUnitTest.MediatRTests.Streetcode.Comment;

public class GetCommentsByStreetcodeIdHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsOnlyRootCommentsForStreetcodeInChronologicalOrder()
    {
        var repository = new Mock<ICommentRepository>();
        var wrapper = new Mock<IRepositoryWrapper>();
        var mapper = new Mock<IMapper>();
        wrapper.Setup(value => value.CommentRepository).Returns(repository.Object);

        var date = new DateTimeOffset(2026, 9, 14, 10, 0, 0, TimeSpan.Zero);
        var comments = new[]
        {
            new CommentEntity { Id = 3, StreetcodeId = 5, CreatedAt = date.AddMinutes(1) },
            new CommentEntity { Id = 2, StreetcodeId = 5, CreatedAt = date },
            new CommentEntity { Id = 1, StreetcodeId = 5, CreatedAt = date },
            new CommentEntity { Id = 4, StreetcodeId = 5, ParentCommentId = 1 },
            new CommentEntity { Id = 5, StreetcodeId = 6 },
        };

        repository.Setup(value => value.GetAllAsync(
                It.IsAny<Expression<Func<CommentEntity, bool>>>(),
                It.IsAny<Func<IQueryable<CommentEntity>, IIncludableQueryable<CommentEntity, object>>?>()))
            .ReturnsAsync((Expression<Func<CommentEntity, bool>> predicate,
                Func<IQueryable<CommentEntity>, IIncludableQueryable<CommentEntity, object>>? _) =>
                comments.Where(predicate.Compile()).ToList());

        mapper.Setup(value => value.Map<IEnumerable<CommentWithRepliesDto>>(
                It.IsAny<object>()))
            .Returns((object source) => ((IEnumerable<CommentEntity>)source)
                .Select(comment => new CommentWithRepliesDto { Id = comment.Id })
                .ToList());

        var handler = new GetCommentsByStreetcodeIdHandler(wrapper.Object, mapper.Object);
        var result = await handler.Handle(new GetCommentsByStreetcodeIdQuery(5), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { 1, 2, 3 }, result.Value.Select(comment => comment.Id));
    }

    [Fact]
    public async Task Handle_WhenNoCommentsExist_ReturnsEmptyCollection()
    {
        var repository = new Mock<ICommentRepository>();
        var wrapper = new Mock<IRepositoryWrapper>();
        var mapper = new Mock<IMapper>();
        wrapper.Setup(value => value.CommentRepository).Returns(repository.Object);
        repository.Setup(value => value.GetAllAsync(
                It.IsAny<Expression<Func<CommentEntity, bool>>>(),
                It.IsAny<Func<IQueryable<CommentEntity>, IIncludableQueryable<CommentEntity, object>>?>()))
            .ReturnsAsync(Array.Empty<CommentEntity>());
        mapper.Setup(value => value.Map<IEnumerable<CommentWithRepliesDto>>(
                It.IsAny<object>()))
            .Returns(Array.Empty<CommentWithRepliesDto>());

        var result = await new GetCommentsByStreetcodeIdHandler(wrapper.Object, mapper.Object)
            .Handle(new GetCommentsByStreetcodeIdQuery(5), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}
