using Microsoft.EntityFrameworkCore;
using Recam.DataAccess.Data;
using Recam.Models.Entities;
using Recam.Models.Enums;
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
    public class MediaAssetRepositoryTests : IDisposable
    {
        private IMediaAssetRepository _mediaAssetRepository;
        private RecamDbContext _dbContext;

        public MediaAssetRepositoryTests()
        {
            var dbOptions = new DbContextOptionsBuilder<RecamDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;

            _dbContext = new RecamDbContext(dbOptions);
            _mediaAssetRepository = new MediaAssetRepository(_dbContext);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        [Fact]
        public async Task GetMediaAssetById_GetSuccess_ReturnsMediaAssetWithAgentInfo()
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

            var mediaAsset = new MediaAsset
            {
                Id = 6,
                MediaType = MediaType.Photo,
                MediaUrl = "https://www.examplephoto.com.au",
                UploadedAt = DateTime.UtcNow,
                IsSelect = true,
                IsHero = true,
                ListingCaseId = 5,
                UserId = "user1",
                IsDeleted = false,
                ListingCase = listingCase
            };

            await _dbContext.Agents.AddAsync(agent);
            await _dbContext.ListingCases.AddAsync(listingCase);
            await _dbContext.MediaAssets.AddAsync(mediaAsset);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _mediaAssetRepository.GetMediaAssetById(mediaAsset.Id);

            var agentListingCase = result.ListingCase.AgentListingCases.First();

            // Asset
            Assert.NotNull(result);
            Assert.Equal(mediaAsset.Id, result.Id);
            Assert.NotNull(result.ListingCase);
            Assert.NotNull(result.ListingCase.AgentListingCases);
            Assert.NotEmpty(result.ListingCase.AgentListingCases);
            Assert.NotNull(agentListingCase.Agent);
            Assert.Equal(agentListingCase.Agent.Id, agent.Id);
            Assert.Equal(agentListingCase.Agent.AgentFirstName, agent.AgentFirstName);
            Assert.Equal(agentListingCase.Agent.AgentLastName, agent.AgentLastName);
            Assert.Equal(agentListingCase.Agent.CompanyName, agent.CompanyName);
        }
    }
}
