using Microsoft.EntityFrameworkCore;
using Recam.DataAccess.Data;
using Recam.Models.Entities;
using Recam.Repositories.Interfaces;
using Recam.Repositories.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Recam.UnitTests
{
    public class AuthRepositoryTests: IDisposable
    {
        private IAuthRepository _authRepository;
        private RecamDbContext _dbContext;
        public AuthRepositoryTests()
        {
            var dbOptions = new DbContextOptionsBuilder<RecamDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
            
            _dbContext = new RecamDbContext(dbOptions);
            _authRepository = new AuthRepository(_dbContext);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        [Fact]
        public async Task GetUsersPaginated_GetUsersSuccess_ReturnsUsersWithRoles()
        {
            // Arrange
            var pageNumber = 2;
            var pageSize = 5;

            var role = new Role
            {
                Id =  "1",
                Name = "Agent",
            };

            var baseTime = DateTime.UtcNow;

            var users = Enumerable.Range(1, 15).Select(i => new User
            {
                Id = i.ToString(),
                UserName = $"User-{i}",
                Email = $"u{i}testemail@test.com",
                UserRoles = new List<UserRole>(),
                CreatedAt = baseTime.AddSeconds(i),
            }).ToList();

            foreach (var u in users)
            {
                u.UserRoles.Add(new UserRole
                {
                    UserId = u.Id,
                    RoleId = role.Id,
                    Role = role
                });
            }

            await _dbContext.Roles.AddAsync(role);
            await _dbContext.Users.AddRangeAsync(users);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _authRepository.GetUsersPaginated(pageNumber, pageSize);

            // Assert
            Assert.Equal(pageSize, result.Count);
            Assert.Equal("6", result.First().Id);
            Assert.All(result, u =>
            {
                Assert.NotNull(u.UserRoles);
                Assert.NotEmpty(u.UserRoles);
                Assert.NotNull(u.UserRoles.First().Role); 
            });

        }

        [Fact]
        public async Task GetAssignedListingCaseIds_FiltersByAgentUserId_ReturnsListingCaseIds()
        {
            // Arrange
            await _dbContext.AgentListingCases.AddRangeAsync(
                // agent-1
                new AgentListingCase { AgentId = "agent-1", ListingCaseId = 101 },
                new AgentListingCase { AgentId = "agent-1", ListingCaseId = 102 },
                // agent-2
                new AgentListingCase { AgentId = "agent-2", ListingCaseId = 999 }
            );

            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _authRepository.GetAssignedListingCaseIds("agent-1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result!.Count);
            Assert.Contains(101, result);
            Assert.Contains(102, result);
            Assert.DoesNotContain(999, result);
        }

        [Fact]
        public async Task GetAssociatedListingCaseIds_FiltersByPhotographyCompanyUserId_ReturnsListingCaseIds()
        {
            // Arrange
            await _dbContext.ListingCases.AddRangeAsync(
                // photographycompany-1
                new ListingCase { Id = 1, UserId = "pc-1", City = "Sydney", State = "NSW", Street="23 Main St", Title = "Test Listing Case" },
                new ListingCase { Id = 2, UserId = "pc-1", City = "Sydney", State = "NSW", Street = "23 Main St", Title = "Test Listing Case" },
                // photographycompany-1
                new ListingCase { Id = 3, UserId = "pc-2", City = "Sydney", State = "NSW", Street = "23 Main St", Title = "Test Listing Case" }
            );

            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _authRepository.GetAssociatedListingCaseIds("pc-1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result!.Count);
            Assert.Contains(1, result);
            Assert.Contains(2, result);
            Assert.DoesNotContain(3, result);
        }
    }
}
