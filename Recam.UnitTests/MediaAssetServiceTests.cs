using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Recam.Models.Collections;
using Recam.Models.Entities;
using Recam.Models.Enums;
using Recam.Repositories.Interfaces;
using Recam.Services.DTOs;
using Recam.Services.Interfaces;
using Recam.Services.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static Recam.Services.DTOs.CreateMediaAssetResponse;
using static Recam.Services.DTOs.DeleteMediaAssetResponse;

namespace Recam.UnitTests
{
    public class MediaAssetServiceTests
    {
        private IMediaAssetService _mediaAssetService;
        private Mock<IMediaAssetRepository> _mockMediaAssetRepository;
        private Mock<IListingCaseRepository> _mockListingCaseRepository;
        private Mock<IMediaAssetHistoryRepository> _mockMediaAssetHistoryRepo;
        private Mock<IMapper> _mockMapper;
        private Mock<IAuthorizationService> _mockAuthorizationService;
        private readonly ClaimsPrincipal _testUser;

        public MediaAssetServiceTests()
        {
            _mockMediaAssetRepository = new Mock<IMediaAssetRepository>();
            
            _mockListingCaseRepository = new Mock<IListingCaseRepository>();

            _mockMediaAssetHistoryRepo = new Mock<IMediaAssetHistoryRepository>();
            _mockMediaAssetHistoryRepo.Setup(r => r.Insert(It.IsAny<MediaAssetHistory>())).Returns(Task.CompletedTask);

            _mockMapper = new Mock<IMapper>();

            _mockAuthorizationService = new Mock<IAuthorizationService>();
            _mockAuthorizationService.Setup(s => s.AuthorizeAsync(
                                                     It.IsAny<ClaimsPrincipal>(),
                                                     It.IsAny<MediaAsset>(),
                                                     "MediaAssetAccess"))
                                     .ReturnsAsync(AuthorizationResult.Success());
            _mockAuthorizationService.Setup(s => s.AuthorizeAsync(
                                                     It.IsAny<ClaimsPrincipal>(),
                                                     It.IsAny<ListingCase>(),
                                                     "ListingCaseAccess"))
                                     .ReturnsAsync(AuthorizationResult.Success());

            _testUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "test-user")
                }, "mock"));

            _mediaAssetService = new MediaAssetService(
                _mockMediaAssetRepository.Object,
                _mockListingCaseRepository.Object,
                _mockMediaAssetHistoryRepo.Object,
                _mockMapper.Object,
                _mockAuthorizationService.Object
                );
        }

        #region CreateMediaAsset Tests

        [Fact]
        public async Task CreateMediaAsset_ListingCaseNotFound_ReturnsBadRequest()
        {
            // Arrange
            var listingCaseId = 3;

            var request = new CreateMediaAssetRequest
            {
                MediaType = MediaType.Photo,
                MediaUrl = "https://www.examplephoto.com.au",
            };

            _mockListingCaseRepository.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync((ListingCase)null);

            // Act
            var result = await _mediaAssetService.CreateMediaAsset(listingCaseId, request, _testUser);

            // Asset
            Assert.Equal(CreateMediaAssetResult.BadRequest, result.Result);
            Assert.Equal("Unable to find the resource. Please provide a valid listing case id.", result.ErrorMessage);
            _mockListingCaseRepository.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.AddMediaAsset(It.IsAny<MediaAsset>()), Times.Never);
        }

        [Fact]
        public async Task CreateMediaAsset_NoAccess_ReturnsForbidden()
        {
            // Arrange
            var listingCaseId = 3;

            var request = new CreateMediaAssetRequest
            {
                MediaType = MediaType.Photo,
                MediaUrl = "https://www.examplephoto.com.au",
            };

            var listingCase = new ListingCase
            {
                Id = 5,
                Title = "Test Listing Case",
                UserId = "different-user"
            };

            _mockListingCaseRepository.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync(listingCase);

            // Override authorization setup to fail
            _mockAuthorizationService.Setup(s => s.AuthorizeAsync(
                                            It.IsAny<ClaimsPrincipal>(),
                                            It.IsAny<ListingCase>(),
                                            "ListingCaseAccess"))
                            .ReturnsAsync(AuthorizationResult.Failed());

            // Act
            var result = await _mediaAssetService.CreateMediaAsset(listingCaseId, request, _testUser);

            // Asset
            Assert.Equal(CreateMediaAssetResult.Forbidden, result.Result);
            Assert.Equal("You are not allowed to create media asset for this listing case.", result.ErrorMessage);
            _mockListingCaseRepository.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockAuthorizationService.Verify(s => s.AuthorizeAsync(_testUser, listingCase, "ListingCaseAccess"), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.AddMediaAsset(It.IsAny<MediaAsset>()), Times.Never);
        }

        [Fact]
        public async Task CreateMediaAsset_HasAccessAndHero_ReturnsMediaId()
        {
            // Arrange
            var listingCaseId = 3;

            var request = new CreateMediaAssetRequest
            {
                MediaType = MediaType.Photo,
                MediaUrl = "https://www.examplephoto.com.au",
            };

            var listingCase = new ListingCase
            {
                Id = 5,
                Title = "Test Listing Case",
                UserId = "test-user"
            };

            _mockListingCaseRepository.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync(listingCase);

            var mediaAsset = new MediaAsset
            {
                Id = 6,
                MediaType = MediaType.Photo,
                MediaUrl = "https://www.examplephoto.com.au",
                UploadedAt = DateTime.UtcNow,
                IsSelect = false,
                IsHero = false,
                ListingCaseId = 3,
                UserId = "test-user",
                IsDeleted = false,
            };

            _mockMapper.Setup(m => m.Map<MediaAsset>(request)).Returns(mediaAsset);

            _mockMediaAssetRepository.Setup(r => r.AddMediaAsset(mediaAsset)).Returns(Task.CompletedTask);

            _mockMediaAssetRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            _mockMediaAssetHistoryRepo.Setup(r => r.Insert(It.IsAny<MediaAssetHistory>())).Returns(Task.CompletedTask);

            // Act
            var result = await _mediaAssetService.CreateMediaAsset(listingCaseId, request, _testUser);

            // Assert
            Assert.Equal(CreateMediaAssetResult.Success, result.Result);
            Assert.Equal(mediaAsset.Id, result.MediaAssetId);
            _mockListingCaseRepository.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockAuthorizationService.Verify(s => s.AuthorizeAsync(_testUser, listingCase, "ListingCaseAccess"), Times.Once);
            _mockMapper.Verify(m => m.Map<MediaAsset>(It.IsAny<CreateMediaAssetRequest>()), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.GetHeroByListingCaseId(listingCaseId), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.AddMediaAsset(It.IsAny<MediaAsset>()), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
            _mockMediaAssetHistoryRepo.Verify(r => r.Insert(It.IsAny<MediaAssetHistory>()), Times.Once);
        }

        #endregion

        #region DeleteMediaAsset Tests

        [Fact]
        public async Task DeleteMediaAsset_MediaAssetNotFound_ReturnsBadRequest()
        {
            // Arrange
            var mediaAssetId = 8;

            _mockMediaAssetRepository.Setup(r => r.GetMediaAssetById(mediaAssetId)).ReturnsAsync((MediaAsset)null);

            // Act
            var result = await _mediaAssetService.DeleteMediaAsset(mediaAssetId, _testUser);

            // Assert
            Assert.Equal(DeleteMediaAssetResult.BadRequest, result.Result);
            Assert.Equal("Unable to find the resource. Please provide a valid media asset id.", result.ErrorMessage);
            _mockMediaAssetRepository.Verify(r => r.GetMediaAssetById(mediaAssetId), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.DeleteMediaAsset(mediaAssetId), Times.Never);
        }

        [Fact]
        public async Task DeleteMediaAsset_NoAccess_ReturnsForbidden()
        {
            // Arrange
            var mediaAssetId = 8;

            var mediaAsset = new MediaAsset
            {
                Id = 8,
                MediaType = MediaType.Photo,
                MediaUrl = "https://www.examplephoto.com.au",
                UploadedAt = DateTime.UtcNow,
                IsSelect = true,
                IsHero = true,
                ListingCaseId = 3,
                UserId = "different-user",
                IsDeleted = false,
            };

            _mockMediaAssetRepository.Setup(r => r.GetMediaAssetById(mediaAssetId)).ReturnsAsync(mediaAsset);

            // Override authorization setup to fail
            _mockAuthorizationService.Setup(s => s.AuthorizeAsync(
                                            It.IsAny<ClaimsPrincipal>(),
                                            It.IsAny<MediaAsset>(),
                                            "MediaAssetAccess"))
                            .ReturnsAsync(AuthorizationResult.Failed());

            // Act
            var result = await _mediaAssetService.DeleteMediaAsset(mediaAssetId, _testUser);

            // Assert
            Assert.Equal(DeleteMediaAssetResult.Forbidden, result.Result);
            Assert.Equal("You are not allowed to access this media asset.", result.ErrorMessage);
            _mockMediaAssetRepository.Verify(r => r.GetMediaAssetById(mediaAssetId), Times.Once);
            _mockAuthorizationService.Verify(s => s.AuthorizeAsync(_testUser, mediaAsset, "MediaAssetAccess"), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.DeleteMediaAsset(mediaAssetId), Times.Never);
        }

        [Fact]
        public async Task DeleteMediaAsset_RepoDeleteFailure_ThrowsException()
        {
            // Arrange
            var mediaAssetId = 8;

            var mediaAsset = new MediaAsset
            {
                Id = 8,
                MediaType = MediaType.Photo,
                MediaUrl = "https://www.examplephoto.com.au",
                UploadedAt = DateTime.UtcNow,
                IsSelect = true,
                IsHero = true,
                ListingCaseId = 3,
                UserId = "different-user",
                IsDeleted = false,
            };

            _mockMediaAssetRepository.Setup(r => r.GetMediaAssetById(mediaAssetId)).ReturnsAsync(mediaAsset);

            _mockMediaAssetRepository.Setup(r => r.DeleteMediaAsset(mediaAssetId)).ReturnsAsync(0);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(async () =>
            {
                await _mediaAssetService.DeleteMediaAsset(mediaAssetId, _testUser);
            });
            exception.Message.Should().Be("Failed to delete media asset.");
        }

        [Fact]
        public async Task DeleteMediaAsset_HasAccess_ReturnsSuccess()
        {
            // Arrange
            var mediaAssetId = 8;

            var mediaAsset = new MediaAsset
            {
                Id = 8,
                MediaType = MediaType.Photo,
                MediaUrl = "https://www.examplephoto.com.au",
                UploadedAt = DateTime.UtcNow,
                IsSelect = true,
                IsHero = true,
                ListingCaseId = 3,
                UserId = "different-user",
                IsDeleted = false,
                ListingCase = new ListingCase
                {
                    Id = 3,
                    Title = "Test_Listing_Case"
                }
            };

            _mockMediaAssetRepository.Setup(r => r.GetMediaAssetById(mediaAssetId)).ReturnsAsync(mediaAsset);

            _mockMediaAssetRepository.Setup(r => r.DeleteMediaAsset(mediaAssetId)).ReturnsAsync(1);

            _mockMediaAssetRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            _mockMediaAssetHistoryRepo.Setup(r => r.Insert(It.IsAny<MediaAssetHistory>())).Returns(Task.CompletedTask);

            // Act
            var result = await _mediaAssetService.DeleteMediaAsset(mediaAssetId, _testUser);

            // Assert
            Assert.Equal(DeleteMediaAssetResult.Success, result.Result);
            _mockMediaAssetRepository.Verify(r => r.GetMediaAssetById(mediaAssetId), Times.Once);
            _mockAuthorizationService.Verify(s => s.AuthorizeAsync(_testUser, mediaAsset, "MediaAssetAccess"), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.DeleteMediaAsset(mediaAssetId), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
            _mockMediaAssetHistoryRepo.Verify(r => r.Insert(It.IsAny<MediaAssetHistory>()), Times.Once);
        }

        #endregion

        #region GetMediaAssetsByListingCaseId Tests

        [Fact]
        public async Task GetMediaAssetsByListingCaseId_ListingCaseNotFound_ReturnsBadRequest()
        {
            // Arrange
            var listingCaseId = 4;

            _mockListingCaseRepository.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync((ListingCase)null);

            // Act
            var result = await _mediaAssetService.GetMediaAssetsByListingCaseId(listingCaseId, _testUser);

            // Asset
            Assert.Equal(GetMediaAssetsResult.BadRequest, result.Result);
            Assert.Equal("Unable to find the resource. Please provide a valid listing case id.", result.ErrorMessage);
            _mockListingCaseRepository.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.GetMediaAssetsByListingCaseId(listingCaseId), Times.Never);
        }

        [Fact]
        public async Task GetMediaAssetsByListingCaseId_NoAccess_ReturnsForbidden()
        {
            // Arrange
            var listingCaseId = 4;

            var listingCase = new ListingCase
            {
                Id = 4,
                Title = "Test Listing Case",
                UserId = "test-user"
            };

            _mockListingCaseRepository.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync(listingCase);

            // Override authorization setup to fail
            _mockAuthorizationService.Setup(s => s.AuthorizeAsync(
                                            It.IsAny<ClaimsPrincipal>(),
                                            It.IsAny<ListingCase>(),
                                            "ListingCaseAccess"))
                            .ReturnsAsync(AuthorizationResult.Failed());

            // Act
            var result = await _mediaAssetService.GetMediaAssetsByListingCaseId(listingCaseId, _testUser);

            // Asset
            Assert.Equal(GetMediaAssetsResult.Forbidden, result.Result);
            Assert.Equal("You are not allowed to access this media assets of this listing case.", result.ErrorMessage);
            _mockListingCaseRepository.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockAuthorizationService.Verify(s => s.AuthorizeAsync(_testUser, listingCase, "ListingCaseAccess"), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.GetMediaAssetsByListingCaseId(listingCaseId), Times.Never);
        }

        [Fact]
        public async Task GetMediaAssetsByListingCaseId_HasAccess_ReturnsMediaAssets()
        {
            // Arrange
            var listingCaseId = 4;

            var listingCase = new ListingCase
            {
                Id = 4,
                Title = "Test Listing Case",
                UserId = "test-user"
            };

            _mockListingCaseRepository.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync(listingCase);

            var assets = new List<MediaAsset>
            {
                new MediaAsset
                {
                    Id = 8,
                    MediaType = MediaType.Photo,
                    MediaUrl = "https://www.examplephoto.com.au",
                    UploadedAt = DateTime.UtcNow,
                    IsSelect = true,
                    IsHero = true,
                    ListingCaseId = 3,
                    UserId = "test-user",
                    IsDeleted = false,
                },
                new MediaAsset
                {
                    Id = 9,
                    MediaType = MediaType.Video,
                    MediaUrl = "https://www.examplevideo.com.au",
                    UploadedAt = DateTime.UtcNow,
                    IsSelect = true,
                    IsHero = false,
                    ListingCaseId = 3,
                    UserId = "test-user",
                    IsDeleted = false,
                },
                new MediaAsset
                {
                    Id = 10,
                    MediaType = MediaType.Photo,
                    MediaUrl = "https://www.examplephoto2.com.au",
                    UploadedAt = DateTime.UtcNow,
                    IsSelect = true,
                    IsHero = false,
                    ListingCaseId = 3,
                    UserId = "test-user",
                    IsDeleted = false,
                }
            };

            _mockMediaAssetRepository.Setup(r => r.GetMediaAssetsByListingCaseId(listingCaseId)).ReturnsAsync(assets);

            var assetsDto = new List<MediaAssetDto>
            {
                new MediaAssetDto
                {
                    Id = 8,
                    MediaType = MediaType.Photo,
                    MediaUrl = "https://www.examplephoto.com.au",
                    UploadedAt = DateTime.UtcNow,
                    IsSelect = true,
                    IsHero = true,
                    ListingCaseId = 3,
                    UserId = "test-user",
                },
                new MediaAssetDto
                {
                    Id = 9,
                    MediaType = MediaType.Video,
                    MediaUrl = "https://www.examplevideo.com.au",
                    UploadedAt = DateTime.UtcNow,
                    IsSelect = true,
                    IsHero = false,
                    ListingCaseId = 3,
                    UserId = "test-user",
                },
                new MediaAssetDto
                {
                    Id = 10,
                    MediaType = MediaType.Photo,
                    MediaUrl = "https://www.examplephoto2.com.au",
                    UploadedAt = DateTime.UtcNow,
                    IsSelect = true,
                    IsHero = false,
                    ListingCaseId = 3,
                    UserId = "test-user",
                }
            };

            _mockMapper.Setup(m => m.Map<List<MediaAssetDto>>(It.IsAny<List<MediaAsset>>())).Returns(assetsDto);

            // Act
            var result = await _mediaAssetService.GetMediaAssetsByListingCaseId(listingCaseId, _testUser);

            // Asset
            Assert.Equal(GetMediaAssetsResult.Success, result.Result);
            Assert.Equal(assetsDto, result.MediaAssets);
            _mockListingCaseRepository.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockAuthorizationService.Verify(s => s.AuthorizeAsync(_testUser, listingCase, "ListingCaseAccess"), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.GetMediaAssetsByListingCaseId(listingCaseId), Times.Once);
            _mockMapper.Verify(m => m.Map<List<MediaAssetDto>>(It.IsAny<List<MediaAsset>>()), Times.Once);
        }
        #endregion

    }
}
