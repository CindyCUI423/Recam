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
    }
}
