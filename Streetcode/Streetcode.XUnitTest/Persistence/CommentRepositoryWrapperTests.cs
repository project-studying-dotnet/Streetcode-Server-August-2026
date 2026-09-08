// <copyright file="CommentRepositoryWrapperTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Streetcode.XUnitTest.Persistence
{
    using Microsoft.EntityFrameworkCore;
    using Streetcode.DAL.Persistence;
    using Streetcode.DAL.Repositories.Realizations.Base;
    using Streetcode.DAL.Repositories.Realizations.Streetcode;
    using Xunit;

    public class CommentRepositoryWrapperTests
    {
        [Fact]
        public void CommentRepository_ShouldBeCreatedOnceAndCached()
        {
            var options = new DbContextOptionsBuilder<StreetcodeDbContext>()
                .UseSqlServer("Server=.;Database=Test;")
                .Options;
            using var context = new StreetcodeDbContext(options);
            var wrapper = new RepositoryWrapper(context);

            var firstRepository = wrapper.CommentRepository;
            var secondRepository = wrapper.CommentRepository;

            Assert.IsType<CommentRepository>(firstRepository);
            Assert.Same(firstRepository, secondRepository);
        }
    }
}
