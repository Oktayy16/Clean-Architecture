﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CA.Core.Domain.Persistence.Contracts;
using CA.Core.Domain.Persistence.Entities;
using CA.Infrastructure.Persistence.Context;
using CA.Infrastructure.Persistence.Repositories;
using NUnit.Framework;

namespace CA.Tests.DomainServices.Persistences
{
    [TestFixture]
    public class PostRepositoryUnitTest
    {
        private  IPostRepositoryAsync _postRepositoryAsync;

        [OneTimeSetUp]
        public async Task SetUp()
        {
            var posts = new List<Post>
            {
                new Post
                {
                    Id = 1,
                    Title = "First Post",
                    Slug = "first-post",
                    Summary = "This is a first post",
                    Content = "This is a big content"
                },
                new Post
                {
                    Id = 2,
                    Title = "second Post",
                    Slug = "second-post",
                    Summary = "This is a second post",
                    Content = "This is a big content"
                },
                new Post
                {
                    Id = 3,
                    Title = "third Post",
                    Slug = "third-post",
                    Summary = "This is a third post",
                    Content = "This is a big content"
                }
            };
            var dbContext = await GetInMemoryDbContext.GetMemoryContext();
         
            
            await dbContext.Posts.AddRangeAsync(posts);
            await dbContext.SaveChangesAsync();
            _postRepositoryAsync = new PostRepositoryAsync(dbContext);

        }

        [Test]
        public async Task CanGetPosts()
        {
            var items = await _postRepositoryAsync.GetAllAsync();

            Assert.AreEqual(3, items.Count);
            Assert.AreEqual("third Post", items.FirstOrDefault(x => x.Id == 3)?.Title);

        }
    }
}
