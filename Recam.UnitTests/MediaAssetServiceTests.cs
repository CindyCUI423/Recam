using AutoMapper;
using Azure;
using Castle.Core.Logging;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
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
using System.Net.Mime;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static Recam.Services.DTOs.CreateMediaAssetResponse;
using static Recam.Services.DTOs.CreateMediaAssetsBatchResponse;
using static Recam.Services.DTOs.DeleteMediaAssetResponse;
using static Recam.Services.DTOs.DownloadListingCaseMediaZipResponse;
using static Recam.Services.DTOs.GetFinalSelectedMediaResponse;
using static Recam.Services.DTOs.SelectMediaResponse;
using static Recam.Services.DTOs.SetHeroMediaResponse;

namespace Recam.UnitTests
{
    public class MediaAssetServiceTests
    {
        private IMediaAssetService _mediaAssetService;
        private Mock<IMediaAssetRepository> _mockMediaAssetRepository;
        private Mock<IListingCaseRepository> _mockListingCaseRepository;
        private Mock<IMediaAssetHistoryRepository> _mockMediaAssetHistoryRepo;
        private Mock<IMapper> _mockMapper;
        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<IBlobStorageService> _mockBlobStorageService;
        private Mock<IAuthorizationService> _mockAuthorizationService;
        private Mock<ILogger<MediaAssetService>> _mockLogger;
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

            _mockUnitOfWork = new Mock<IUnitOfWork>();

            _mockBlobStorageService = new Mock<IBlobStorageService>();

            _mockLogger = new Mock<ILogger<MediaAssetService>>();

            _testUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "test-user")
                }, "mock"));

            _mediaAssetService = new MediaAssetService(
                _mockMediaAssetRepository.Object,
                _mockListingCaseRepository.Object,
                _mockMediaAssetHistoryRepo.Object,
                _mockMapper.Object,
                _mockAuthorizationService.Object,
                _mockUnitOfWork.Object,
                _mockBlobStorageService.Object,
                _mockLogger.Object
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
        public async Task CreateMediaAsset_HasAccess_ReturnsMediaId()
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
            _mockMediaAssetRepository.Verify(r => r.AddMediaAsset(It.IsAny<MediaAsset>()), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
            _mockMediaAssetHistoryRepo.Verify(r => r.Insert(It.IsAny<MediaAssetHistory>()), Times.Once);
        }

        #endregion

        #region CreateMediaAssetsBatch Tests

        [Fact]
        public async Task CreateMediaAssetsBatch_ListingCaseNotFound_ReturnsBadRequest()
        {
            // Arrange
            var listingCaseId = 3;

            var request = new CreateMediaAssetsBatchRequest
            {
                MediaType = MediaType.Photo,
                MediaUrls = ["https://www.examplephoto.com.au/recam/001", "https://www.examplephoto.com.au/recam/002", "https://www.examplephoto.com.au/recam/003"],
            };

            _mockListingCaseRepository.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync((ListingCase)null);

            // Act
            var result = await _mediaAssetService.CreateMediaAssetsBatch(listingCaseId, request, _testUser);

            // Asset
            Assert.Equal(CreateMediaAssetsBatchResult.BadRequest, result.Result);
            Assert.Equal("Unable to find the resource. Please provide a valid listing case id.", result.ErrorMessage);
            _mockListingCaseRepository.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.AddMediaAsset(It.IsAny<MediaAsset>()), Times.Never);
        }

        [Fact]
        public async Task CreateMediaAssetsBatch_NoAccess_ReturnsForbidden()
        {
            // Arrange
            var listingCaseId = 3;

            var request = new CreateMediaAssetsBatchRequest
            {
                MediaType = MediaType.Photo,
                MediaUrls = ["https://www.examplephoto.com.au/recam/001", "https://www.examplephoto.com.au/recam/002", "https://www.examplephoto.com.au/recam/003"],
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
            var result = await _mediaAssetService.CreateMediaAssetsBatch(listingCaseId, request, _testUser);

            // Asset
            Assert.Equal(CreateMediaAssetsBatchResult.Forbidden, result.Result);
            Assert.Equal("You are not allowed to create media asset for this listing case.", result.ErrorMessage);
            _mockListingCaseRepository.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockAuthorizationService.Verify(s => s.AuthorizeAsync(_testUser, listingCase, "ListingCaseAccess"), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.AddMediaAsset(It.IsAny<MediaAsset>()), Times.Never);
        }

        [Fact]
        public async Task CreateMediaAssetsBatch_WhenExceptionOccurs_ShouldRollback()
        {
            // Arrange
            var listingCaseId = 3;

            var request = new CreateMediaAssetsBatchRequest
            {
                MediaType = MediaType.Photo,
                MediaUrls = ["https://www.examplephoto.com.au/recam/001", "https://www.examplephoto.com.au/recam/002", "https://www.examplephoto.com.au/recam/003"],
            };

            var listingCase = new ListingCase
            {
                Id = 5,
                Title = "Test Listing Case",
                UserId = "test-user"
            };

            _mockListingCaseRepository.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync(listingCase);

            _mockUnitOfWork.Setup(u => u.BeginTransaction()).Returns(Task.CompletedTask);

            // Create an exception
            _mockMediaAssetRepository.Setup(r => r.AddMediaAssets(It.IsAny<List<MediaAsset>>())).ThrowsAsync(new Exception("DB error"));

            // Act & Asset
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await _mediaAssetService.CreateMediaAssetsBatch(listingCaseId, request, _testUser);
            });

            _mockUnitOfWork.Verify(u => u.BeginTransaction(), Times.Once);
            _mockUnitOfWork.Verify(u => u.Rollback(), Times.Once);
            _mockUnitOfWork.Verify(u => u.Commit(), Times.Never);
        }

        [Fact]
        public async Task CreateMediaAssetsBatch_HasAccess_ReturnsMediaAssetIds()
        {
            // Arrange
            var listingCaseId = 3;

            var request = new CreateMediaAssetsBatchRequest
            {
                MediaType = MediaType.Photo,
                MediaUrls = ["https://www.examplephoto.com.au/recam/001.jpg", "https://www.examplephoto.com.au/recam/002.jpg", "https://www.examplephoto.com.au/recam/003.jpg"],
            };

            var listingCase = new ListingCase
            {
                Id = 5,
                Title = "Test Listing Case",
                UserId = "test-user"
            };

            _mockListingCaseRepository.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync(listingCase);

            _mockUnitOfWork.Setup(u => u.BeginTransaction()).Returns(Task.CompletedTask);

            List<MediaAsset>? capturedAssets = null;

            _mockMediaAssetRepository
                .Setup(r => r.AddMediaAssets(It.IsAny<List<MediaAsset>>()))
                .Callback<List<MediaAsset>>(assets =>
                {
                    capturedAssets = assets;

                    // Moc the genration of media asset id
                    for (int i = 0; i < assets.Count; i++)
                    {
                        assets[i].Id = i + 1;
                    }
                })
                .Returns(Task.CompletedTask);

            _mockMediaAssetRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _mediaAssetService.CreateMediaAssetsBatch(listingCaseId, request, _testUser);

            // Assert
            Assert.Equal(CreateMediaAssetsBatchResult.Success, result.Result);
            Assert.NotNull(result.MediaAssetIds);
            Assert.Equal(new List<int> { 1, 2, 3 }, result.MediaAssetIds);
            _mockUnitOfWork.Verify(u => u.BeginTransaction(), Times.Once);
            _mockUnitOfWork.Verify(u => u.Commit(), Times.Once);
            _mockUnitOfWork.Verify(u => u.Rollback(), Times.Never);
            _mockMediaAssetRepository.Verify(r => r.AddMediaAssets(It.IsAny<List<MediaAsset>>()), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
            Assert.NotNull(capturedAssets);
            Assert.Equal(3, capturedAssets!.Count);
            Assert.All(capturedAssets, a =>
            {
                Assert.Equal(MediaType.Photo, a.MediaType);
                Assert.Equal(listingCaseId, a.ListingCaseId);
                Assert.Equal("test-user", a.UserId);
                Assert.False(a.IsSelect);
                Assert.False(a.IsHero);
                Assert.False(a.IsDeleted);
            });
            Assert.Equal("https://www.examplephoto.com.au/recam/003.jpg", capturedAssets[2].MediaUrl);
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
        public async Task DeleteMediaAsset_ExtractBlobNameFailure_ReturnsError()
        {
            // Arrange
            var mediaAssetId = 8;

            var mediaAsset = new MediaAsset
            {
                Id = 8,
                MediaType = MediaType.Photo,
                MediaUrl = "https://www.examplephoto.com.au", // this url will make ExtractBlobNameFromUrl return null
                UploadedAt = DateTime.UtcNow,
                IsSelect = true,
                IsHero = true,
                ListingCaseId = 3,
                UserId = "different-user",
                IsDeleted = false,
            };

            _mockMediaAssetRepository.Setup(r => r.GetMediaAssetById(mediaAssetId)).ReturnsAsync(mediaAsset);

            // Act
            var result = await _mediaAssetService.DeleteMediaAsset(mediaAssetId, _testUser);

            // Assert
            Assert.Equal(DeleteMediaAssetResult.Error, result.Result);
            Assert.Equal("Unable to extract the valid blob name from media url.", result.ErrorMessage);
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
                MediaUrl = "https://recamblobstorage.blob.core.windows.net/recam/12345.jpg",
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
                MediaUrl = "https://recamblobstorage.blob.core.windows.net/recam/12345.jpg",
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
                    MediaUrl = "https://www.examplevideo1.com.au",
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
                    MediaUrl = "https://www.examplevideo1.com.au",
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

        #region SetHeroMedia Tests

        [Fact]
        public async Task SetHeroMedia_ListingCaseNotFound_ReturnsBadRequest()
        {
            // Arrange
            var listingCaseId = 4;
            var mediaAssetId = 10;

            _mockListingCaseRepository.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync((ListingCase)null);

            // Act
            var result = await _mediaAssetService.SetHeroMedia(listingCaseId, mediaAssetId, _testUser);

            // Asset
            Assert.Equal(SetHeroMediaResult.BadRequest, result.Result);
            Assert.Equal("Unable to find the resource. Please provide a valid listing case id.", result.ErrorMessage);
            _mockListingCaseRepository.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.UpdateMediaAsset(It.IsAny<MediaAsset>()), Times.Never);
        }

        [Fact]
        public async Task SetHeroMedia_NoAccess_ReturnsForbidden()
        {
            // Arrange
            var listingCaseId = 4;
            var mediaAssetId = 10;

            var listingCase = new ListingCase
            {
                Id = 4,
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
            var result = await _mediaAssetService.SetHeroMedia(listingCaseId, mediaAssetId, _testUser);

            // Asset
            Assert.Equal(SetHeroMediaResult.Forbidden, result.Result);
            Assert.Equal("You are not allowed to access this media assets of this listing case.", result.ErrorMessage);
            _mockListingCaseRepository.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockAuthorizationService.Verify(s => s.AuthorizeAsync(_testUser, listingCase, "ListingCaseAccess"), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.UpdateMediaAsset(It.IsAny<MediaAsset>()), Times.Never);
        }

        [Fact]
        public async Task SetHeroMedia_MediaAssetNotFound_ReturnsBadRequest()
        {
            // Arrange
            var listingCaseId = 4;
            var mediaAssetId = 10;

            var listingCase = new ListingCase
            {
                Id = 4,
                Title = "Test Listing Case",
                UserId = "test-user"
            };

            _mockListingCaseRepository.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync(listingCase);

            _mockMediaAssetRepository.Setup(r => r.GetMediaAssetById(mediaAssetId)).ReturnsAsync((MediaAsset)null);

            // Act
            var result = await _mediaAssetService.SetHeroMedia(listingCaseId, mediaAssetId, _testUser);

            // Asset
            Assert.Equal(SetHeroMediaResult.BadRequest, result.Result);
            Assert.Equal("Unable to find the resource. Please provide a valid media asset id.", result.ErrorMessage);
            _mockListingCaseRepository.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockAuthorizationService.Verify(s => s.AuthorizeAsync(_testUser, listingCase, "ListingCaseAccess"), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.UpdateMediaAsset(It.IsAny<MediaAsset>()), Times.Never);
        }

        [Fact]
        public async Task SetHeroMedia_MediaAssetDoesNotBelongsToListingCase_ReturnsBadRequest()
        {
            // Arrange
            var listingCaseId = 4;
            var mediaAssetId = 10;

            var listingCase = new ListingCase
            {
                Id = 4,
                Title = "Test Listing Case",
                UserId = "test-user"
            };

            var mediaAsset = new MediaAsset
            {
                Id = 10,
                ListingCaseId = 1,
                UserId = "test-user"
            };

            _mockListingCaseRepository.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync(listingCase);

            _mockMediaAssetRepository.Setup(r => r.GetMediaAssetById(mediaAssetId)).ReturnsAsync(mediaAsset);

            // Act
            var result = await _mediaAssetService.SetHeroMedia(listingCaseId, mediaAssetId, _testUser);

            // Asset
            Assert.Equal(SetHeroMediaResult.BadRequest, result.Result);
            Assert.Equal($"Media Asset {mediaAssetId} does not belong to the Listing Case {listingCaseId}.", result.ErrorMessage);
            _mockListingCaseRepository.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockAuthorizationService.Verify(s => s.AuthorizeAsync(_testUser, listingCase, "ListingCaseAccess"), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.UpdateMediaAsset(It.IsAny<MediaAsset>()), Times.Never);
        }

        [Fact]
        public async Task SetHeroMedia_HasAccessAndExistingHero_ResetHeroAndReturnsSuccess()
        {
            // Arrange
            var listingCaseId = 4;
            var mediaAssetId = 10;

            var listingCase = new ListingCase
            {
                Id = 4,
                Title = "Test Listing Case",
                UserId = "test-user"
            };

            var mediaAsset = new MediaAsset
            {
                Id = 10,
                ListingCaseId = 4,
                IsHero = false,
                UserId = "test-user"
            };

            _mockListingCaseRepository.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync(listingCase);

            _mockMediaAssetRepository.Setup(r => r.GetMediaAssetById(mediaAssetId)).ReturnsAsync(mediaAsset);

            var existingHero = new MediaAsset
            {
                Id = 9,
                ListingCaseId = 4,
                IsHero = true,
                UserId = "test-user"
            };

            _mockMediaAssetRepository.Setup(r => r.GetHeroByListingCaseId(listingCaseId)).ReturnsAsync(existingHero);

            // Act
            var result = await _mediaAssetService.SetHeroMedia(listingCaseId, mediaAssetId, _testUser);

            // Assert
            Assert.Equal(SetHeroMediaResult.Success, result.Result);
            Assert.True(mediaAsset.IsHero);
            Assert.False(existingHero.IsHero);
            _mockListingCaseRepository.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockAuthorizationService.Verify(s => s.AuthorizeAsync(_testUser, listingCase, "ListingCaseAccess"), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.GetMediaAssetById(mediaAssetId), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.GetHeroByListingCaseId(listingCaseId), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.UpdateMediaAsset(It.IsAny<MediaAsset>()), Times.Exactly(2));
            _mockMediaAssetRepository.Verify(r => r.SaveChangesAsync(), Times.Once);

        }

        #endregion

        #region SelectMediaBatch Tests

        [Fact]
        public async Task SelectMediaBatch_ListingCaseNotFound_ReturnsBadRequest()
        {
            // Arrange
            var listingCaseId = 4;

            var request = new SelectMediaRequest
            {
                SelectedId = [9, 10],
                UnselectedId = [8]
            };

            _mockListingCaseRepository.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync((ListingCase)null);

            // Act
            var result = await _mediaAssetService.SelectMediaBatch(listingCaseId, request, _testUser);

            // Asset
            Assert.Equal(SelectMediaResult.BadRequest, result.Result);
            Assert.Equal("Unable to find the resource. Please provide a valid listing case id.", result.ErrorMessage);
            _mockListingCaseRepository.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.GetMediaAssetsByIds(listingCaseId, It.IsAny<List<int>>()), Times.Never);
        }

        [Fact]
        public async Task SelectMediaBatch_NoAccess_ReturnsForbidden()
        {
            // Arrange
            var listingCaseId = 4;

            var request = new SelectMediaRequest
            {
                SelectedId = [9, 10],
                UnselectedId = [8]
            };

            var listingCase = new ListingCase
            {
                Id = 4,
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
            var result = await _mediaAssetService.SelectMediaBatch(listingCaseId, request, _testUser);

            // Asset
            Assert.Equal(SelectMediaResult.Forbidden, result.Result);
            Assert.Equal("You are not allowed to access this media assets of this listing case.", result.ErrorMessage);
            _mockListingCaseRepository.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockAuthorizationService.Verify(s => s.AuthorizeAsync(_testUser, listingCase, "ListingCaseAccess"), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.GetMediaAssetsByIds(listingCaseId, It.IsAny<List<int>>()), Times.Never);
        }

        [Fact]
        public async Task SelectMediaBatch_MediaAssetsCountMismatch_ReturnsBadRequest()
        {
            // Arrange
            var listingCaseId = 4;

            var request = new SelectMediaRequest
            {
                SelectedId = [9, 10],
                UnselectedId = [8]
            };

            var listingCase = new ListingCase
            {
                Id = 4,
                Title = "Test Listing Case",
                UserId = "test-user"
            };

            _mockListingCaseRepository.Setup( r => r.GetListingCaseById(listingCaseId)).ReturnsAsync(listingCase);

            _mockUnitOfWork.Setup(u => u.BeginTransaction()).Returns(Task.CompletedTask);

            var assets = new List<MediaAsset>();
            assets.Add(new MediaAsset 
            {
                Id = 10,
                ListingCaseId = 1,
                IsSelect = false,
                UserId = "test-user"
            });

            _mockMediaAssetRepository.Setup(r => r.GetMediaAssetsByIds(listingCaseId, It.IsAny<List<int>>())).ReturnsAsync(assets);

            // Act
            var result = await _mediaAssetService.SelectMediaBatch(listingCaseId, request, _testUser);

            // Assert
            Assert.Equal(SelectMediaResult.BadRequest, result.Result);
            Assert.Equal("Unable to find the resource. Please provide valid listing case id or media asset id.", result.ErrorMessage);
            _mockUnitOfWork.Verify(u => u.BeginTransaction(), Times.Once);
            _mockUnitOfWork.Verify(u => u.Rollback(), Times.Once);
            _mockUnitOfWork.Verify(u => u.Commit(), Times.Never);
            _mockListingCaseRepository.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockAuthorizationService.Verify(s => s.AuthorizeAsync(_testUser, listingCase, "ListingCaseAccess"), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.GetMediaAssetsByIds(listingCaseId, It.IsAny<List<int>>()), Times.Once);
        }

        [Fact]
        public async Task SelectMediaBatch_WhenMoreThan10MediaSelected_ReturnsBadRequest()
        {
            // Arrange
            var listingCaseId = 1;

            var request = new SelectMediaRequest
            {
                SelectedId = [11, 12],
                UnselectedId = [8]
            };

            var listingCase = new ListingCase
            {
                Id = 1,
                Title = "Test Listing Case",
                UserId = "test-user"
            };

            _mockListingCaseRepository.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync(listingCase);

            _mockUnitOfWork.Setup(u => u.BeginTransaction()).Returns(Task.CompletedTask);

            var assets = new List<MediaAsset>();
            assets.Add(
                new MediaAsset
                {
                    Id = 8,
                    ListingCaseId = listingCaseId,
                    IsSelect = true,
                    UserId = "test-user"
                });
            assets.Add(
                new MediaAsset
                {
                    Id = 11,
                    ListingCaseId = listingCaseId,
                    IsSelect = false,
                    UserId = "test-user"
                });
            assets.Add(
                new MediaAsset
                {
                    Id = 12,
                    ListingCaseId = listingCaseId,
                    IsSelect = false,
                    UserId = "test-user"
                });

            _mockMediaAssetRepository.Setup(r => r.GetMediaAssetsByIds(listingCaseId, It.IsAny<List<int>>())).ReturnsAsync(assets);

            _mockMediaAssetRepository.Setup(r => r.CountSelectedMediaForListingCase(listingCaseId)).ReturnsAsync(10);

            // Act
            var result = await _mediaAssetService.SelectMediaBatch(listingCaseId, request, _testUser);

            // Assert
            Assert.Equal(SelectMediaResult.BadRequest, result.Result);
            Assert.Equal($"You can select up to 10 media assets for a listing case to display. Current selection would become 11.", result.ErrorMessage);
            _mockUnitOfWork.Verify(u => u.BeginTransaction(), Times.Once);
            _mockUnitOfWork.Verify(u => u.Rollback(), Times.Once);
            _mockUnitOfWork.Verify(u => u.Commit(), Times.Never);
            _mockListingCaseRepository.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockAuthorizationService.Verify(s => s.AuthorizeAsync(_testUser, listingCase, "ListingCaseAccess"), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.GetMediaAssetsByIds(listingCaseId, It.IsAny<List<int>>()), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.CountSelectedMediaForListingCase(listingCaseId), Times.Once);
        }

        [Fact]
        public async Task SelectMediaBatch_HasAccess_ReturnsSuccess()
        {
            // Arrange
            var listingCaseId = 1;

            var request = new SelectMediaRequest
            {
                SelectedId = [11, 12],
                UnselectedId = [8]
            };

            var listingCase = new ListingCase
            {
                Id = 1,
                Title = "Test Listing Case",
                UserId = "test-user"
            };

            _mockListingCaseRepository.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync(listingCase);

            _mockUnitOfWork.Setup(u => u.BeginTransaction()).Returns(Task.CompletedTask);

            var assets = new List<MediaAsset>();
            assets.Add(
                new MediaAsset
                {
                    Id = 8,
                    ListingCaseId = listingCaseId,
                    IsSelect = true,
                    UserId = "test-user"
                });
            assets.Add(
                new MediaAsset
                {
                    Id = 11,
                    ListingCaseId = listingCaseId,
                    IsSelect = false,
                    UserId = "test-user"
                });
            assets.Add(
                new MediaAsset
                {
                    Id = 12,
                    ListingCaseId = listingCaseId,
                    IsSelect = false,
                    UserId = "test-user"
                });

            _mockMediaAssetRepository.Setup(r => r.GetMediaAssetsByIds(listingCaseId, It.IsAny<List<int>>())).ReturnsAsync(assets);

            _mockMediaAssetRepository.Setup(r => r.CountSelectedMediaForListingCase(listingCaseId)).ReturnsAsync(8);

            // Act
            var result = await _mediaAssetService.SelectMediaBatch(listingCaseId, request, _testUser);

            // Assert
            Assert.Equal(SelectMediaResult.Success, result.Result);
            _mockUnitOfWork.Verify(u => u.BeginTransaction(), Times.Once);
            _mockUnitOfWork.Verify(u => u.Rollback(), Times.Never);
            _mockUnitOfWork.Verify(u => u.Commit(), Times.Once);
            _mockListingCaseRepository.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockAuthorizationService.Verify(s => s.AuthorizeAsync(_testUser, listingCase, "ListingCaseAccess"), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.GetMediaAssetsByIds(listingCaseId, It.IsAny<List<int>>()), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.CountSelectedMediaForListingCase(listingCaseId), Times.Once);
            _mockMediaAssetHistoryRepo.Verify(r => r.Insert(It.IsAny<MediaAssetHistory>()), Times.Exactly(3));
            Assert.False(assets[0].IsSelect);
            Assert.True(assets[1].IsSelect);
            Assert.True(assets[2].IsSelect);
        }

        #endregion

        #region GetFinalSelectedMediaForListingCase Tests

        [Fact]
        public async Task GetFinalSelectedMediaForListingCase_ListingCaseNotFound_ReturnsBadRequest()
        {
            // Arrange
            var listingCaseId = 4;

            _mockListingCaseRepository.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync((ListingCase)null);

            // Act
            var result = await _mediaAssetService.GetFinalSelectedMediaForListingCase(listingCaseId, _testUser);

            // Asset
            Assert.Equal(GetFinalSelectedMediaResult.BadRequest, result.Result);
            Assert.Equal("Unable to find the resource. Please provide a valid listing case id.", result.ErrorMessage);
            _mockListingCaseRepository.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.GetFinalSelectedMediaForListingCase(listingCaseId), Times.Never);
        }

        [Fact]
        public async Task GetFinalSelectedMediaForListingCase_NoAccess_ReturnsForbidden()
        {
            // Arrange
            var listingCaseId = 4;

            var listingCase = new ListingCase
            {
                Id = 4,
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
            var result = await _mediaAssetService.GetFinalSelectedMediaForListingCase(listingCaseId, _testUser);

            // Asset
            Assert.Equal(GetFinalSelectedMediaResult.Forbidden, result.Result);
            Assert.Equal("You are not allowed to access this media assets of this listing case.", result.ErrorMessage);
            _mockListingCaseRepository.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockAuthorizationService.Verify(s => s.AuthorizeAsync(_testUser, listingCase, "ListingCaseAccess"), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.GetFinalSelectedMediaForListingCase(listingCaseId), Times.Never);
        }

        [Fact]
        public async Task GetFinalSelectedMediaForListingCase_HasAccess_ReturnsSuccess()
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
                    IsSelect = false,
                    IsHero = false,
                    ListingCaseId = 4,
                    UserId = "test-user",
                    IsDeleted = false,
                },
                new MediaAsset
                {
                    Id = 9,
                    MediaType = MediaType.Video,
                    MediaUrl = "https://www.examplevideo1.com.au",
                    UploadedAt = DateTime.UtcNow,
                    IsSelect = false,
                    IsHero = false,
                    ListingCaseId = 4,
                    UserId = "test-user",
                    IsDeleted = false,
                },
                new MediaAsset
                {
                    Id = 10,
                    MediaType = MediaType.Photo,
                    MediaUrl = "https://www.examplephoto2.com.au",
                    UploadedAt = DateTime.UtcNow,
                    IsSelect = false,
                    IsHero = false,
                    ListingCaseId = 4,
                    UserId = "test-user",
                    IsDeleted = false,
                }
            };

            _mockMediaAssetRepository.Setup(r => r.GetFinalSelectedMediaForListingCase(listingCaseId)).ReturnsAsync(assets);

            var mappedAssets = new List<MediaAssetDto>
            {
                new MediaAssetDto
                {
                    Id = 8,
                    MediaType = MediaType.Photo,
                    MediaUrl = "https://www.examplephoto.com.au",
                    UploadedAt = DateTime.UtcNow,
                    IsSelect = false,
                    IsHero = false,
                    ListingCaseId = 4,
                    UserId = "test-user",
                },
                new MediaAssetDto
                {
                    Id = 9,
                    MediaType = MediaType.Video,
                    MediaUrl = "https://www.examplevideo1.com.au",
                    UploadedAt = DateTime.UtcNow,
                    IsSelect = false,
                    IsHero = false,
                    ListingCaseId = 4,
                    UserId = "test-user",
                },
                new MediaAssetDto
                {
                    Id = 10,
                    MediaType = MediaType.Photo,
                    MediaUrl = "https://www.examplephoto2.com.au",
                    UploadedAt = DateTime.UtcNow,
                    IsSelect = false,
                    IsHero = false,
                    ListingCaseId = 4,
                    UserId = "test-user",
                }

            };

            _mockMapper.Setup(m => m.Map<List<MediaAssetDto>>(It.IsAny<List<MediaAsset>>())).Returns(mappedAssets);

            // Act
            var result = await _mediaAssetService.GetFinalSelectedMediaForListingCase(listingCaseId, _testUser);

            // Asset
            Assert.Equal(GetFinalSelectedMediaResult.Success, result.Result);
            Assert.Equal(mappedAssets, result.SelectedMediaAssets);
            _mockListingCaseRepository.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockAuthorizationService.Verify(s => s.AuthorizeAsync(_testUser, listingCase, "ListingCaseAccess"), Times.Once);
            _mockMapper.Verify(m => m.Map<List<MediaAssetDto>>(It.IsAny<List<MediaAsset>>()), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.GetFinalSelectedMediaForListingCase(listingCaseId), Times.Once);
        }

        #endregion

        #region DownloadListingCaseMediaZip Tests

        [Fact]
        public async Task DownloadListingCaseMediaZip_ListingCaseNotFound_ReturnsBadRequest()
        {
            // Arrange
            var listingCaseId = 4;

            _mockListingCaseRepository.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync((ListingCase)null);

            // Act
            var result = await _mediaAssetService.DownloadListingCaseMediaZip(listingCaseId, _testUser, CancellationToken.None);

            // Asset
            Assert.Equal(DownloadZipResult.BadRequest, result.Result);
            Assert.Equal("Unable to find the resource. Please provide a valid listing case id.", result.ErrorMessage);
            _mockListingCaseRepository.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.GetMediaAssetsByListingCaseId(listingCaseId), Times.Never);
            _mockBlobStorageService.Verify(s => s.Download(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DownloadListingCaseMediaZip_NoAccess_ReturnsForbidden()
        {
            // Arrange
            var listingCaseId = 4;

            var listingCase = new ListingCase
            {
                Id = 4,
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
            var result = await _mediaAssetService.DownloadListingCaseMediaZip(listingCaseId, _testUser, CancellationToken.None);

            // Asset
            Assert.Equal(DownloadZipResult.Forbidden, result.Result);
            Assert.Equal("You are not allowed to access this media assets of this listing case.", result.ErrorMessage);
            _mockListingCaseRepository.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockAuthorizationService.Verify(s => s.AuthorizeAsync(_testUser, listingCase, "ListingCaseAccess"), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.GetMediaAssetsByListingCaseId(listingCaseId), Times.Never);
        }

        [Fact]
        public async Task DownloadListingCaseMediaZip_HasAccess_ReturnsSuccess()
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
                new MediaAsset { Id = 11, MediaUrl = "https://example.com/container/a.jpg" },
                new MediaAsset { Id = 12, MediaUrl = "https://example.com/container/b.png" }
            };

            _mockMediaAssetRepository.Setup(r => r.GetMediaAssetsByListingCaseId(listingCaseId)).ReturnsAsync(assets);

            _mockBlobStorageService
                .Setup(s => s.Download(It.IsAny<string>()))
                .ReturnsAsync(() =>
                {
                    var bytes = Encoding.UTF8.GetBytes("fake-bytes");
                    var stream = new MemoryStream(bytes);
                    return (Stream: stream, ContentType: "image/jpeg");
                });

            // Act
            var result = await _mediaAssetService.DownloadListingCaseMediaZip(listingCaseId, _testUser, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(DownloadZipResult.Success, result.Result);
            Assert.NotNull(result.ZipStream);
            Assert.True(result.ZipStream!.CanRead);
            Assert.True(result.ZipStream!.Length > 0);
            Assert.Equal($"listing-case-{listingCaseId}-media.zip", result.ZipFileName);
            _mockListingCaseRepository.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockAuthorizationService.Verify(a => a.AuthorizeAsync(_testUser, listingCase, "ListingCaseAccess"), Times.Once);
            _mockMediaAssetRepository.Verify(r => r.GetMediaAssetsByListingCaseId(listingCaseId), Times.Once);
            _mockBlobStorageService.Verify(b => b.Download(It.IsAny<string>()), Times.Exactly(2));
        }


        #endregion
    }
}
