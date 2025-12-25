using AutoMapper;
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
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static Recam.Services.DTOs.DeleteListingCaseResponse;

namespace Recam.UnitTests
{
    public class ListingCaseServiceTests
    {
        private IListingCaseService _listingCaseService;
        private Mock<IListingCaseRepository> _mockListingCaseRepo;
        private Mock<ICaseHistoryRepository> _mockCaseHistoryRepo;
        private Mock<IMapper> _mockMapper;
        private Mock<IAuthorizationService> _mockAuthService;
        private Mock<ILogger<ListingCaseService>> _mockLogger;
        private readonly ClaimsPrincipal _testUser;

        public ListingCaseServiceTests()
        {
            _mockListingCaseRepo = new Mock<IListingCaseRepository>();

            _mockCaseHistoryRepo = new Mock<ICaseHistoryRepository>();
            _mockCaseHistoryRepo.Setup(r => r.Insert(It.IsAny<CaseHistory>())).Returns(Task.CompletedTask);
                

            _mockMapper = new Mock<IMapper>();

            _mockAuthService = new Mock<IAuthorizationService>();
            _mockAuthService.Setup(s => s.AuthorizeAsync(
                                            It.IsAny<ClaimsPrincipal>(), 
                                            It.IsAny<ListingCase>(), 
                                            "ListingCaseAccess"))
                            .ReturnsAsync(AuthorizationResult.Success());

            _mockLogger = new Mock<ILogger<ListingCaseService>>();

            _testUser = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "test-user")
                }, "mock"));

            _listingCaseService = new ListingCaseService(
                _mockListingCaseRepo.Object,
                _mockCaseHistoryRepo.Object,
                _mockMapper.Object,
                _mockAuthService.Object,
                _mockLogger.Object
                );
        }

        #region CreateListingCase Tests

        [Fact]
        public async Task CreateListingCase_ValidInput_CreateSucceeds()
        {
            // Arrange
            var userId = "1";

            var request = new CreateListingCaseRequest
            {
                Title = "Test listing case",
                Street = "123 Main St",
                City = "Sydney",
                State = "NSW",
                Postcode = 2000,
                Bedrooms = 3,
                Bathrooms = 2,
                Garages = 1,
                FloorArea = 120.5,
                PropertyType = PropertyType.House
            };

            var mappedListingCase = new ListingCase
            {
                Id = 10,
                Title = request.Title,
                Street = request.Street,
                City = request.City,
                State = request.State,
                Postcode = request.Postcode,
                Bedrooms = request.Bedrooms,
                Bathrooms = request.Bathrooms,
                Garages = request.Garages,
                FloorArea = request.FloorArea,
                PropertyType = request.PropertyType,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
            };

            _mockMapper.Setup(m => m.Map<ListingCase>(It.IsAny<CreateListingCaseRequest>())).Returns(mappedListingCase);

            _mockListingCaseRepo.Setup(r => r.AddListingCase(It.IsAny<ListingCase>())).Returns(Task.CompletedTask);

            _mockListingCaseRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            _mockCaseHistoryRepo.Setup(r => r.Insert(It.IsAny<CaseHistory>())).Returns(Task.CompletedTask);

            // Act
            var result = await _listingCaseService.CreateListingCase(request, userId);

            // Assert
            _mockListingCaseRepo.Verify(r => r.AddListingCase(It.IsAny<ListingCase>()), Times.Once);
            _mockListingCaseRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
            _mockCaseHistoryRepo.Verify(r => r.Insert(It.IsAny<CaseHistory>()), Times.Once);
        }

        #endregion

        #region GetListingCasesByUser Tests

        [Theory]
        [InlineData(-2, 10)]
        [InlineData(3, -5)]
        [InlineData(0, 5)]
        [InlineData(1, 0)]
        public async Task GetListingCasesByUser_InvalidPageNumberAndSize_ReturnsBadRequest(int pageNumber, int pageSize)
        {
            // Arrange
            var userId = "1";
            var role = "Agent";

            // Act
            var result = await _listingCaseService.GetListingCasesByUser(pageNumber, pageSize, userId, role);

            // Assert
            Assert.Equal(GetListingCasesStatus.BadRequest, result.Status);
            Assert.Equal("pageNumber and pageSize must be greater than 0.", result.ErrorMessage);
            _mockListingCaseRepo.Verify(r => r.GetListingCasesForAgent(It.IsAny<string>()), Times.Never);
            _mockListingCaseRepo.Verify(r => r.GetListingCasesForPhotographyCompany(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetListingCasesByUser_InvalidUserRole_ReturnsUnauthorized()
        {
            // Arrange
            var pageNumber = 3;
            var pageSize = 10;

            var userId = "1";
            var role = "InvalidRole";

            // Act
            var result = await _listingCaseService.GetListingCasesByUser(pageNumber, pageSize, userId, role);

            // Assert
            Assert.Equal(GetListingCasesStatus.Unauthorized, result.Status);
            Assert.Equal("Invalid user role.", result.ErrorMessage);
            _mockListingCaseRepo.Verify(r => r.GetListingCasesForAgent(It.IsAny<string>()), Times.Never);
            _mockListingCaseRepo.Verify(r => r.GetListingCasesForPhotographyCompany(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetListingCasesByUser_ValidInput_ReturnsListingCases()
        {
            // Arrange
            var pageNumber = 2;
            var pageSize = 2;

            var userId = "1";
            var role = "Agent";

            _mockListingCaseRepo.Setup(r => r.GetListingCasesForAgent(userId))
                .ReturnsAsync(new List<ListingCase>
                {
                    new ListingCase { Id = 1, Title = "Listing Case 1", CreatedAt = DateTime.Now.AddSeconds(1) },
                    new ListingCase { Id = 2, Title = "Listing Case 2", CreatedAt = DateTime.Now.AddSeconds(2) },
                    new ListingCase { Id = 3, Title = "Listing Case 3", CreatedAt = DateTime.Now.AddSeconds(3) },
                    new ListingCase { Id = 4, Title = "Listing Case 4", CreatedAt = DateTime.Now.AddSeconds(4) }
                });

            var listingCasesDto = new List<ListingCaseDto>
                {
                    new ListingCaseDto { Id = 4, Title = "Listing Case 4", CreatedAt = DateTime.Now.AddSeconds(4) },
                    new ListingCaseDto { Id = 3, Title = "Listing Case 3", CreatedAt = DateTime.Now.AddSeconds(3) },
                };

            var listingCases = new List<ListingCase>
                {
                    new ListingCase { Id = 4, Title = "Listing Case 4", CreatedAt = DateTime.Now.AddSeconds(4) },
                    new ListingCase { Id = 3, Title = "Listing Case 3", CreatedAt = DateTime.Now.AddSeconds(3) },
                };

            _mockMapper.Setup(m => m.Map<List<ListingCaseDto>>(It.Is<List<ListingCase>>(l => l.Count == 2))).Returns(listingCasesDto);
                 
            // Act
            var result = await _listingCaseService.GetListingCasesByUser(pageNumber, pageSize, userId, role);

            // Assert
            Assert.Equal(GetListingCasesStatus.Success, result.Status);
            Assert.Equal(listingCasesDto, result.ListingCases);
            Assert.Equal(4, result.TotalCount);
            _mockListingCaseRepo.Verify(r => r.GetListingCasesForAgent(It.IsAny<string>()), Times.Once);
            _mockMapper.Verify(m => m.Map<List<ListingCaseDto>>(It.IsAny<List<ListingCase>?>()));
        }

        #endregion

        #region GetListingCaseById Tests

        [Fact]
        public async Task GetListingCaseById_ListingCaseNotFound_ReturnsBadRequest()
        {
            // Arrange
            var listingCaseId = 1;

            _mockListingCaseRepo.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync((ListingCase)null);

            // Act 
            var result = await _listingCaseService.GetListingCaseById(listingCaseId, _testUser);

            // Assert
            Assert.Equal(ListingCaseDetailStatus.BadRequest, result.Status);
            Assert.Equal("Unable to find the resource. Please provide a valid listing case id.", result.ErrorMessage);
            _mockListingCaseRepo.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
        }

        [Fact]
        public async Task GetListingCaseById_NoAccess_ReturnsForbidden()
        {
            // Arrange
            var listingCaseId = 1;

            var listingCase = new ListingCase
            {
                Id = 1,
                Title = "Test Listing Case",
                UserId = "different-user"
            }; 

            _mockListingCaseRepo.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync(listingCase);

            // Override authorization setup to fail
            _mockAuthService.Setup(s => s.AuthorizeAsync(
                                            It.IsAny<ClaimsPrincipal>(),
                                            It.IsAny<ListingCase>(),
                                            "ListingCaseAccess"))
                            .ReturnsAsync(AuthorizationResult.Failed());

            // Act 
            var result = await _listingCaseService.GetListingCaseById(listingCaseId, _testUser);

            // Assert
            Assert.Equal(ListingCaseDetailStatus.Forbidden, result.Status);
            Assert.Equal("You are not allowed to access this listing case.", result.ErrorMessage);
            _mockListingCaseRepo.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockAuthService.Verify(s => s.AuthorizeAsync(_testUser, listingCase, "ListingCaseAccess"), Times.Once);

        }

        [Fact]
        public async Task GetListingCaseById_HasAccess_ReturnsListingCaseDetail()
        {
            // Arrange
            var listingCaseId = 1;

            var listingCase = new ListingCase
            {
                Id = 1,
                Title = "Test Listing Case",
                UserId = "different-user"
            };

            _mockListingCaseRepo.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync(listingCase);

            var listingCaseDto = new ListingCaseDto
            {
                Id = 1,
                Title = "Test Listing Case",
                UserId = "different-user"

            };

            _mockMapper.Setup(m => m.Map<ListingCaseDto>(listingCase)).Returns(listingCaseDto);

            var mappedAgents = new List<AgentInfo>
            {
                new AgentInfo { AgentFirstName = "FN", AgentLastName = "LN", CompanyName = "CN" }
            };

            _mockMapper.Setup(m => m.Map<List<AgentInfo>>(It.IsAny<List<Agent>>())).Returns(mappedAgents);

            // Act
            var result = await _listingCaseService.GetListingCaseById(listingCaseId, _testUser);

            // Assert
            Assert.Equal(ListingCaseDetailStatus.Success, result.Status);
            Assert.Equal(listingCaseDto, result.ListingCaseInfo);
            Assert.Equal(mappedAgents, result.Agents);
            _mockListingCaseRepo.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockAuthService.Verify(s => s.AuthorizeAsync(_testUser, listingCase, "ListingCaseAccess"), Times.Once);
            _mockMapper.Verify(m => m.Map<ListingCaseDto>(It.IsAny<ListingCase?>()), Times.Once);
            _mockMapper.Verify(m => m.Map<List<AgentInfo>>(It.IsAny<List<Agent>?>()), Times.Once);
        }

        #endregion

        #region UpdateListingCase Tests

        [Fact]
        public async Task UpdateListingCase_ListingCaseNotFound_ReturnsBadRequest()
        {
            // Arrange
            var listingCaseId = 1;

            var request = new UpdateListingCaseRequest
            {
                Title = "Updated Title"
            };

            var listingCase = new ListingCase
            {
                Id = 1,
                Title = "Test Listing Case",
                UserId = "different-user"
            };

            _mockListingCaseRepo.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync((ListingCase)null);

            // Act 
            var result = await _listingCaseService.UpdateListingCase(listingCaseId, request, _testUser);

            // Assert
            Assert.Equal(UpdateListingCaseResult.BadRequest, result.Result);
            Assert.Equal("Unable to find the resource. Please provide a valid listing case id.", result.ErrorMessage);
            _mockListingCaseRepo.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockListingCaseRepo.Verify(r => r.UpdateListingCase(listingCase), Times.Never);
        }

        public async Task UpdateListingCase_NoAccess_ReturnsForbidden()
        {
            // Arrange
            var listingCaseId = 1;

            var request = new UpdateListingCaseRequest
            {
                Title = "Updated Title"
            };

            var listingCase = new ListingCase
            {
                Id = 1,
                Title = "Test Listing Case",
                UserId = "different-user"
            };

            _mockListingCaseRepo.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync(listingCase);

            // Override authorization setup to fail
            _mockAuthService.Setup(s => s.AuthorizeAsync(
                                            It.IsAny<ClaimsPrincipal>(),
                                            It.IsAny<ListingCase>(),
                                            "ListingCaseAccess"))
                            .ReturnsAsync(AuthorizationResult.Failed());

            // Act
            var result = await _listingCaseService.UpdateListingCase(listingCaseId, request, _testUser);

            // Assert
            Assert.Equal(UpdateListingCaseResult.Forbidden, result.Result);
            Assert.Equal("You are not allowed to access this listing case.", result.ErrorMessage);
            _mockListingCaseRepo.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockAuthService.Verify(s => s.AuthorizeAsync(_testUser, listingCase, "ListingCaseAccess"), Times.Once);
            _mockListingCaseRepo.Verify(r => r.UpdateListingCase(listingCase), Times.Never);
        }

        [Fact]
        public async Task UpdateListingCase_RepoUpdateFailure_ThrowsException()
        { 
            // Arrange
            var listingCaseId = 1;

            var request = new UpdateListingCaseRequest
            {
                Title = "Updated Title",
                City = "Melbourne",
                Bathrooms = 5
            };

            var listingCase = new ListingCase
            {
                Id = 1,
                Title = "Test Listing Case",
                City = "Sydney",
                Bathrooms = 2,
                UserId = "test-user"
            };

            _mockListingCaseRepo.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync(listingCase);

            _mockMapper
                .Setup(m => m.Map(
                    It.IsAny<UpdateListingCaseRequest>(),
                    It.IsAny<ListingCase>()))
                .Returns((UpdateListingCaseRequest request, ListingCase dest) => dest);

            _mockListingCaseRepo.Setup(r => r.UpdateListingCase(It.IsAny<ListingCase>())).ReturnsAsync(0);

            // Act & Assert
            var exceprion = await Assert.ThrowsAsync<Exception>(async () =>
            {
                await _listingCaseService.UpdateListingCase(listingCaseId, request, _testUser);
            });
            exceprion.Message.Should().Be("Failed to update listing case.");
        }

        [Fact]
        public async Task UpdateListingCase_HasAccess_ReturnsSuccess()
        {
            // Arrange
            var listingCaseId = 1;

            var request = new UpdateListingCaseRequest
            {
                Title = "Updated Title",
                City = "Melbourne",
                Bathrooms = 5
            };

            var listingCase = new ListingCase
            {
                Id = 1,
                Title = "Test Listing Case",
                City = "Sydney",
                Bathrooms = 2,
                UserId = "test-user"
            };

            _mockListingCaseRepo.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync(listingCase);

            _mockMapper
                .Setup(m => m.Map(
                    It.IsAny<UpdateListingCaseRequest>(),
                    It.IsAny<ListingCase>()))
                .Returns((UpdateListingCaseRequest request, ListingCase dest) => dest);

            _mockListingCaseRepo.Setup(r => r.UpdateListingCase(It.IsAny<ListingCase>())).ReturnsAsync(1);

            _mockCaseHistoryRepo.Setup(r => r.Insert(It.IsAny<CaseHistory>())).Returns(Task.CompletedTask);

            // Act
            var result = await _listingCaseService.UpdateListingCase(listingCaseId, request, _testUser);

            // Assert
            Assert.Equal(UpdateListingCaseResult.Success, result.Result);
            _mockListingCaseRepo.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockAuthService.Verify(s => s.AuthorizeAsync(_testUser, listingCase, "ListingCaseAccess"), Times.Once);
            _mockListingCaseRepo.Verify(r => r.UpdateListingCase(It.IsAny<ListingCase>()), Times.Once);
            _mockCaseHistoryRepo.Verify(r => r.Insert(It.IsAny<CaseHistory>()), Times.Once);
        }


        #endregion

        #region ChangeListingCaseStatus Tests

        [Fact]
        public async Task ChangeListingCaseStatus_ListingCaseNotFound_ReturnsInvalId()
        {
            // Arrange
            var listingCaseId = 1;

            _mockListingCaseRepo.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync((ListingCase)null);

            // Act 
            var result = await _listingCaseService.DeleteListingCase(listingCaseId, _testUser);

            // Assert
            Assert.Equal(DeleteListingCaseResult.InvalidId, result.Result);
            Assert.Equal("Unable to find the resource. Please provide a valid listing case id.", result.ErrorMessage);
            _mockListingCaseRepo.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockListingCaseRepo.Verify(r => r.ChangeListingCaseStatus(listingCaseId, It.IsAny<ListingCaseStatus>()), Times.Never);
        }

        [Fact]
        public async Task ChangeListingCaseStatus_NoAccess_ReturnsForbidden()
        {
            // Arrange
            var listingCaseId = 1;

            var listingCase = new ListingCase
            {
                Id = 1,
                Title = "Test Listing Case",
                ListingCaseStatus = ListingCaseStatus.Created,
                UserId = "different-user"
            };

            var request = new ChangeListingCaseStatusRequest
            {
                Status = ListingCaseStatus.Pending
            };

            _mockListingCaseRepo.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync(listingCase);

            // Override authorization setup to fail
            _mockAuthService.Setup(s => s.AuthorizeAsync(
                                            It.IsAny<ClaimsPrincipal>(),
                                            It.IsAny<ListingCase>(),
                                            "ListingCaseAccess"))
                            .ReturnsAsync(AuthorizationResult.Failed());

            // Act
            var result = await _listingCaseService.ChangeListingCaseStatus(listingCaseId, request, _testUser);

            // Assert
            Assert.Equal(ChangeListingCaseStatusResult.Forbidden, result.Result);
            Assert.Equal("You are not allowed to access this listing case.", result.ErrorMessage);
            _mockListingCaseRepo.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockAuthService.Verify(s => s.AuthorizeAsync(_testUser, listingCase, "ListingCaseAccess"), Times.Once);
            _mockListingCaseRepo.Verify(r => r.ChangeListingCaseStatus(listingCaseId, It.IsAny<ListingCaseStatus>()), Times.Never);
        }

        [Fact]
        public async Task ChangeListingCaseStatus_RepoUpdateFailure_ThrowsException()
        {
            // Arrange
            var listingCaseId = 1;

            var listingCase = new ListingCase
            {
                Id = 1,
                Title = "Test Listing Case",
                ListingCaseStatus = ListingCaseStatus.Created,
                UserId = "test-user"
            };

            var request = new ChangeListingCaseStatusRequest
            {
                Status = ListingCaseStatus.Pending
            };

            _mockListingCaseRepo.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync(listingCase);
            _mockListingCaseRepo.Setup(r => r.ChangeListingCaseStatus(listingCaseId, ListingCaseStatus.Pending)).ReturnsAsync(0);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(async () =>
            {
                await _listingCaseService.ChangeListingCaseStatus(listingCaseId, request, _testUser);
            });
            exception.Message.Should().Be("Failed to change listing case status.");
        }

        [Fact]
        public async Task ChangeListingCaseStatus_HasAccess_ReturnsSuccess()
        {
            // Arrange
            var listingCaseId = 1;

            var listingCase = new ListingCase
            {
                Id = 1,
                Title = "Test Listing Case",
                ListingCaseStatus = ListingCaseStatus.Created,
                UserId = "test-user"
            };

            var request = new ChangeListingCaseStatusRequest
            {
                Status = ListingCaseStatus.Pending
            };

            _mockListingCaseRepo.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync(listingCase);
            _mockListingCaseRepo.Setup(r => r.ChangeListingCaseStatus(listingCaseId, ListingCaseStatus.Pending)).ReturnsAsync(1);
            _mockListingCaseRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _mockCaseHistoryRepo.Setup(r => r.Insert(It.IsAny<CaseHistory>())).Returns(Task.CompletedTask);

            // Act
            var result = await _listingCaseService.ChangeListingCaseStatus(listingCaseId, request, _testUser);

            // Assert
            Assert.Equal(ChangeListingCaseStatusResult.Success, result.Result);
            Assert.Equal(ListingCaseStatus.Created, result.oldStatus);
            Assert.Equal(ListingCaseStatus.Pending, result.newStatus);
            _mockListingCaseRepo.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockAuthService.Verify(s => s.AuthorizeAsync(_testUser, listingCase, "ListingCaseAccess"), Times.Once);
            _mockListingCaseRepo.Verify(r => r.ChangeListingCaseStatus(listingCaseId, ListingCaseStatus.Pending), Times.Once);
            _mockListingCaseRepo.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
        }

        #endregion

        #region DeleteListingCase Tests

        [Fact]
        public async Task DeleteListingCase_ListingCaseNotFound_ReturnsInvalid()
        {
            // Arrange
            var listingCaseId = 1;

            _mockListingCaseRepo.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync((ListingCase)null);

            // Act 
            var result = await _listingCaseService.DeleteListingCase(listingCaseId, _testUser);

            // Assert
            Assert.Equal(DeleteListingCaseResult.InvalidId, result.Result);
            Assert.Equal("Unable to find the resource. Please provide a valid listing case id.", result.ErrorMessage);
            _mockListingCaseRepo.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockListingCaseRepo.Verify(r => r.DeleteListingCase(listingCaseId), Times.Never);
            _mockListingCaseRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteListingCase_NoAccess_ReturnsForbidden()
        {
            // Arrange
            var listingCaseId = 1;
            var listingCase = new ListingCase
            {
                Id = 1,
                Title = "Test Listing Case",
                UserId = "different-user"
            };

            _mockListingCaseRepo.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync(listingCase);

            // Override authorization setup to fail
            _mockAuthService.Setup(s => s.AuthorizeAsync(
                                            It.IsAny<ClaimsPrincipal>(),
                                            It.IsAny<ListingCase>(),
                                            "ListingCaseAccess"))
                            .ReturnsAsync(AuthorizationResult.Failed());

            // Act 
            var result = await _listingCaseService.DeleteListingCase(listingCaseId, _testUser);

            // Assert
            Assert.Equal(DeleteListingCaseResult.Forbidden, result.Result);
            Assert.Equal("You are not allowed to access this listing case.", result.ErrorMessage);
            _mockListingCaseRepo.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockAuthService.Verify(s => s.AuthorizeAsync(_testUser, listingCase, "ListingCaseAccess"), Times.Once);
            _mockListingCaseRepo.Verify(r => r.DeleteListingCase(listingCaseId), Times.Never);
            _mockListingCaseRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteListingCase_RepoDeleteFailure_ThrowsException()
        {
            // Arrange
            var listingCaseId = 1;
            var listingCase = new ListingCase
            {
                Id = 1,
                Title = "Test Listing Case",
                UserId = "test-user"
            };

            _mockListingCaseRepo.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync(listingCase);
            _mockListingCaseRepo.Setup(r => r.DeleteListingCase(listingCaseId)).ReturnsAsync(0);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(async () =>
            {
                await _listingCaseService.DeleteListingCase(listingCaseId, _testUser);
            });
            exception.Message.Should().Be("Failed to delete listing case.");
        }

        [Fact]
        public async Task DeleteListingCase_HasAccess_ReturnsSuccess()
        {
            // Arrange
            var listingCaseId = 1;
            var listingCase = new ListingCase
            {
                Id = 1,
                Title = "Test Listing Case",
                UserId = "test-user"
            };

            _mockListingCaseRepo.Setup(r => r.GetListingCaseById(listingCaseId)).ReturnsAsync(listingCase);

            _mockListingCaseRepo.Setup(r => r.DeleteListingCase(listingCaseId)).ReturnsAsync(1);
            _mockListingCaseRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            _mockCaseHistoryRepo.Setup(r => r.Insert(It.IsAny<CaseHistory>())).Returns(Task.CompletedTask);

            // Act
            var result = await _listingCaseService.DeleteListingCase(listingCaseId, _testUser);

            // Assert
            Assert.Equal(DeleteListingCaseResult.Success, result.Result);
            _mockListingCaseRepo.Verify(r => r.GetListingCaseById(listingCaseId), Times.Once);
            _mockAuthService.Verify(s => s.AuthorizeAsync(_testUser, listingCase, "ListingCaseAccess"), Times.Once);
            _mockListingCaseRepo.Verify(r => r.DeleteListingCase(listingCaseId), Times.Once);
            _mockListingCaseRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
            _mockCaseHistoryRepo.Verify(r => r.Insert(It.IsAny<CaseHistory>()), Times.Once);
        }

        #endregion


    }
}
