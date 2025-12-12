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
    public class ListingCaseRepositoryTests : IDisposable
    {
        private IListingCaseRepository _listingCaseRepository;
        private RecamDbContext _dbContext;
        public ListingCaseRepositoryTests()
        {
            var dbOptions = new DbContextOptionsBuilder<RecamDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;

            _dbContext = new RecamDbContext(dbOptions);
            _listingCaseRepository = new ListingCaseRepository(_dbContext);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        [Fact]
        public async Task GetListingCaseById_GetSuccess_ReturnsLisingCaseWithAgentInfo()
        {
            // Arrange
            var agent = new Agent
            {
                Id = "agent1",
                AgentFirstName = "FN",
                AgentLastName = "LN",
                CompanyName = "Company"
            };


            var listingCase = new ListingCase
            {
                Id = 5,
                Title = "Test Listing Case",
                Street = "123 Main St",
                City = "Sydney",
                State = "NSW",
                Postcode = 2000,
                UserId = "user1",
                IsDeleted = false,
                AgentListingCases = new List<AgentListingCase>()

            };
            listingCase.AgentListingCases.Add(new AgentListingCase
            {
                AgentId = agent.Id,
                ListingCaseId = listingCase.Id,
                Agent = agent,
            });

            await _dbContext.Agents.AddAsync(agent);
            await _dbContext.ListingCases.AddAsync(listingCase);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _listingCaseRepository.GetListingCaseById(listingCase.Id);

            var agentListingCase = result.AgentListingCases.First();

            //Assert
            Assert.NotNull(result);
            Assert.Equal(listingCase.Id, result.Id);
            Assert.NotNull(result.AgentListingCases);
            Assert.NotEmpty(result.AgentListingCases);
            Assert.NotNull(agentListingCase.Agent);
            Assert.Equal(agentListingCase.Agent.Id, agent.Id);
            Assert.Equal(agentListingCase.Agent.AgentFirstName, agent.AgentFirstName);
            Assert.Equal(agentListingCase.Agent.AgentLastName, agent.AgentLastName);
            Assert.Equal(agentListingCase.Agent.CompanyName, agent.CompanyName);
        }

    }
}
