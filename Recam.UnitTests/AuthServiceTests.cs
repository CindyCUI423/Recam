using AutoMapper;
using Azure.Core;
using DnsClient.Internal;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Recam.Models.Collections;
using Recam.Models.Entities;
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
using static Recam.Services.DTOs.GetCurrentUserInfoResponse;
using static Recam.Services.DTOs.UpdatePasswordResponse;

namespace Recam.UnitTests
{
    public class AuthServiceTests
    {
        private IAuthService _authService;
        private Mock<IAuthRepository> _mockAuthRepo;
        private Mock<IUserActivityLogRepository> _mockUserActivityLogRepo;
        private Mock<IConfiguration> _mockConfiguration;
        private Mock<IMapper> _mockMapper;
        private Mock<UserManager<User>> _mockUserManager;
        private Mock<SignInManager<User>> _mockSignInManager;
        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<ILogger<AuthService>> _mockLogger;

        public AuthServiceTests()
        {
            _mockAuthRepo = new Mock<IAuthRepository>();

            _mockUserActivityLogRepo = new Mock<IUserActivityLogRepository>();
            _mockUserActivityLogRepo.Setup(r => r.Insert(It.IsAny<UserActivityLog>())).Returns(Task.CompletedTask);

            _mockConfiguration = new Mock<IConfiguration>();

            _mockMapper = new Mock<IMapper>();

            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(
                userStore.Object, null, null, null, null, null, null, null, null);


            _mockSignInManager = new Mock<SignInManager<User>>(
                _mockUserManager.Object,
                Mock.Of<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<User>>(),
                null, null, null, null) ;

            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUnitOfWork.Setup(u => u.BeginTransaction()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.Commit()).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.Rollback()).Returns(Task.CompletedTask);

            _mockLogger = new Mock<ILogger<AuthService>>();

            _authService = new AuthService(
                _mockAuthRepo.Object, 
                _mockUserActivityLogRepo.Object, 
                _mockConfiguration.Object, 
                _mockMapper.Object, 
                _mockUserManager.Object, 
                _mockSignInManager.Object, 
                _mockUnitOfWork.Object,
                _mockLogger.Object);
        }

        private static ClaimsPrincipal BuildUser(string? userId, string? role)
        {
            var claims = new List<Claim>();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, authenticationType: "mock");

            return new ClaimsPrincipal(identity);
        }

        #region SignUp Tests

        [Fact]
        public async Task SignUp_UserNameExists_ReturnUserNameAlreadyExists()
        {
            // Arrange
            var userName = "testUser";

            var request = new SignUpRequest
            {
                UserName = userName,
                Email = "test@example.com",
                Password = "1234Abcd@",
                RoleType = "Agent",
            };

            var existingUser = new User
            {
                Id = "1",
                UserName = userName
            };

            _mockUserManager.Setup(m => m.FindByNameAsync(userName)).ReturnsAsync(existingUser);
            _mockUserManager.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync((User)null);

            // Act
            var result = await _authService.SignUp(request);

            // Assert
            Assert.Equal(SignUpStatus.UserNameAlreadyExists, result.Status);
            Assert.Equal("Username is already taken.", result.ErrorMessage);
            _mockUserManager.Verify(m => m.FindByNameAsync(userName), Times.Once);
            _mockUserManager.Verify(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.Commit(), Times.Never);
        }

        [Fact]
        public async Task SignUp_EmailExists_ReturnEmailAlreadyExists()
        {
            // Arrange
            var email = "testemail@test.com";

            var request = new SignUpRequest
            {
                UserName = "userName",
                Email = email,
                Password = "1234Abcd@",
                RoleType = "Agent",
            };

            var existingEmail = new User
            {
                Id = "1",
                Email = email,
            };

            _mockUserManager.Setup(m => m.FindByNameAsync(request.UserName)).ReturnsAsync((User)null);
            _mockUserManager.Setup(m => m.FindByEmailAsync(email)).ReturnsAsync(existingEmail);

            // Act
            var result = await _authService.SignUp(request);

            // Assert
            Assert.Equal(SignUpStatus.EmailAlreadyExists, result.Status);
            Assert.Equal("Email is already in use.", result.ErrorMessage);
            _mockUserManager.Verify(m => m.FindByEmailAsync(email), Times.Once);
            _mockUserManager.Verify(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.Commit(), Times.Never);
        }

        [Fact]
        public async Task SignUp_CreateUserFailed_ReturnsCreateUserFailure()
        {
            // Arrange
            var request = new SignUpRequest
            {
                UserName = "userName",
                Email = "testemail@test.com",
                Password = "1234",
                RoleType = "Agent",
            };

            _mockUserManager.Setup(m => m.FindByNameAsync(request.UserName)).ReturnsAsync((User)null);
            _mockUserManager.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync((User)null);

            var mappedUser = new User
            {
                Id = "1",
                UserName = request.UserName,
                Email = request.Email,
            };
            _mockMapper.Setup(m => m.Map<User>(request)).Returns(mappedUser);

            var error = new IdentityError
            {
                Description = "Password too weak"
            };
            _mockUserManager.Setup(m => m.CreateAsync(mappedUser, request.Password)).ReturnsAsync(IdentityResult.Failed(error));

            // Act
            var result = await _authService.SignUp(request);

            // Assert
            Assert.Equal(SignUpStatus.CreateUserFailure, result.Status);
            Assert.Contains("Password too weak", result.ErrorMessage);
            _mockUserManager.Verify(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.Rollback(), Times.Once);
            _mockUnitOfWork.Verify(u => u.Commit(), Times.Never);
            _mockUserManager.Verify(m => m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SignUp_AssignRoleFailed_ReturnsAssignRoleFailure()
        {
            // Arrange
            var request = new SignUpRequest
            {
                UserName = "userName",
                Email = "testemail@test.com",
                Password = "1234",
                RoleType = "PhotographyCompany",
                PhotographyCompanyInfo = new PhotographyCompanySignUpInfo
                {
                    PhotographyCompanyName = "TestCompany"
                }
            };

            _mockUserManager.Setup(m => m.FindByNameAsync(request.UserName)).ReturnsAsync((User)null);
            _mockUserManager.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync((User)null);

            var mappedUser = new User
            {
                Id = "1",
                UserName = request.UserName,
                Email = request.Email,
            };
            _mockMapper.Setup(m => m.Map<User>(request)).Returns(mappedUser);

            _mockUserManager.Setup(m => m.CreateAsync(mappedUser, request.Password)).ReturnsAsync(IdentityResult.Success);

            var photographyCompany = new PhotographyCompany
            {
                Id = "1",
                PhotographyCompanyName = request.PhotographyCompanyInfo.PhotographyCompanyName
            };
            _mockMapper.Setup(m => m.Map<PhotographyCompany>(request.PhotographyCompanyInfo)).Returns(photographyCompany);

            var error = new IdentityError
            {
                Description = "Role does not exist"
            };
            _mockUserManager.Setup(m => m.AddToRoleAsync(mappedUser, request.RoleType)).ReturnsAsync(IdentityResult.Failed(error));

            // Act
            var result = await _authService.SignUp(request);

            // Assert
            Assert.Equal(SignUpStatus.AssignRoleFailure, result.Status);
            Assert.Contains("Role does not exist", result.ErrorMessage);
            _mockAuthRepo.Verify(r => r.AddPhotographyCompany(It.Is<PhotographyCompany>(pc => pc.Id == "1")), Times.Once);
            _mockUserManager.Verify(m => m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.Rollback(), Times.Once);
            _mockUnitOfWork.Verify(u => u.Commit(), Times.Never);
        }

        [Fact]
        public async Task SignUp_CreateUserSuccess_CreateUserAndAssignRole()
        {
            // Arrange
            var request = new SignUpRequest
            {
                UserName = "userName",
                Email = "testemail@test.com",
                Password = "1234",
                RoleType = "PhotographyCompany",
                PhotographyCompanyInfo = new PhotographyCompanySignUpInfo
                {
                    PhotographyCompanyName = "TestCompany"
                }
            };

            _mockUserManager.Setup(m => m.FindByNameAsync(request.UserName)).ReturnsAsync((User)null);
            _mockUserManager.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync((User)null);

            var mappedUser = new User
            {
                Id = "1",
                UserName = request.UserName,
                Email = request.Email,
            };
            _mockMapper.Setup(m => m.Map<User>(request)).Returns(mappedUser);

            _mockUserManager.Setup(m => m.CreateAsync(mappedUser, request.Password)).ReturnsAsync(IdentityResult.Success);

            var photographyCompany = new PhotographyCompany
            {
                Id = "1",
                PhotographyCompanyName = request.PhotographyCompanyInfo.PhotographyCompanyName
            };
            _mockMapper.Setup(m => m.Map<PhotographyCompany>(request.PhotographyCompanyInfo)).Returns(photographyCompany);

            _mockUserManager.Setup(m => m.AddToRoleAsync(mappedUser, request.RoleType)).ReturnsAsync(IdentityResult.Success);

            _mockUserActivityLogRepo.Setup(r => r.Insert(It.IsAny<UserActivityLog>())).Returns(Task.CompletedTask);

            // Act
            var result = await _authService.SignUp(request);

            // Assert
            Assert.Equal(SignUpStatus.Success, result.Status);
            Assert.Equal("1", result.UserId);

            _mockAuthRepo.Verify(r => r.AddAgent(It.Is<Agent>(a => a.Id == "1")), Times.Never);
            _mockAuthRepo.Verify(r => r.AddPhotographyCompany(It.IsAny<PhotographyCompany>()), Times.Once);

            _mockUserManager.Verify(m => m.AddToRoleAsync(
                It.Is<User>(u => u == mappedUser),
                It.Is<string>(role => role == request.RoleType)
            ), Times.Once);

            _mockUnitOfWork.Verify(u => u.BeginTransaction(), Times.Once);
            _mockUnitOfWork.Verify(u => u.Commit(), Times.Once);
            _mockUnitOfWork.Verify(u => u.Rollback(), Times.Never);

            _mockUserActivityLogRepo.Verify(r => r.Insert(
                It.Is<UserActivityLog>(l => l.Action == "SignUp" && l.IsSuccessful)
            ), Times.Once);
        }

        #endregion

        #region Login Tests

        [Fact]
        public async Task Login_UserIsNull_ReturnsUserNotFound()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "testemail@test.com",
                Password = "1234Abcd@"
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync((User)null);

            // Act
            var result = await _authService.Login(request);

            // Assert
            Assert.Equal(LoginStatus.UserNotFound, result.Status);
            Assert.Equal("User not found.", result.ErrorMessage);
            _mockUserActivityLogRepo.Verify(r => r.Insert(
                It.Is<UserActivityLog>(l => 
                    l.Email == request.Email &&
                    l.Action == "Login" &&
                    l.IsSuccessful == false &&
                    l.Message == LoginStatus.UserNotFound.ToString()
                 )), Times.Once);
            _mockUserManager.Verify(m => m.FindByEmailAsync(request.Email), Times.Once);
            _mockSignInManager.Verify(m => m.CheckPasswordSignInAsync(It.IsAny<User>(), request.Password, It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Login_InvalidPassword_ReturnsInvalidCredentials()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "testemail@test.com",
                Password = "1234Abcd@"
            };

            var user = new User
            {
                Id = "1",
                UserName = "TestUser",
                Email = request.Email,
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync(user);

            _mockSignInManager.Setup(m => m.CheckPasswordSignInAsync(user, request.Password, true)).ReturnsAsync(SignInResult.Failed);

            // Act
            var result = await _authService.Login(request);

            // Assert
            Assert.Equal(LoginStatus.InvalidCredentials, result.Status);
            Assert.Equal("Incorrect email or password.", result.ErrorMessage);
            _mockSignInManager.Verify(m => m.CheckPasswordSignInAsync(user, request.Password, It.IsAny<bool>()), Times.Once);
            _mockUserActivityLogRepo.Verify(r => r.Insert(
                It.Is<UserActivityLog>(l =>
                    l.UserId == user.Id &&
                    l.UserName == user.UserName &&
                    l.Email == request.Email &&
                    l.Action == "Login" &&
                    l.IsSuccessful == false &&
                    l.Message == LoginStatus.InvalidCredentials.ToString()
                )), Times.Once);
        }

        [Fact]
        public async Task LoginAndJWTGenerator_LoginSuccess_ReturnsUserTokenAndInfo()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "testemail@test.com",
                Password = "1234Abcd@"
            };

            var agent = new Agent
            {
                Id = "1",
                AgentFirstName = "TestAFristName",
                AgentLastName = "TestALastName",
                CompanyName = "TestCompanyName",
            };

            var user = new User
            {
                Id = "1",
                UserName = "TestUser",
                Email = request.Email,
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(request.Email)).ReturnsAsync(user);

            _mockSignInManager.Setup(m => m.CheckPasswordSignInAsync(user, request.Password, true)).ReturnsAsync(SignInResult.Success);

            // Mock GetRolesAsync in JWTGenerator
            _mockUserManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Agent" });

            // Mock configurations in JWTGenerator
            _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns("jwt_key_for_test_fasbjkfaskfnsajf[");
            _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("test-issuer");
            _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns("test-audience");

            // Mock GetRolesAsync in Login
            _mockUserManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Agent" });

            _mockAuthRepo.Setup(r => r.GetAgentByUserId(user.Id)).ReturnsAsync(agent);

            var agentInfo = new AgentInfo
            {
                AgentFirstName = "TestAFristName",
                AgentLastName = "TestALastName",
                CompanyName = "TestCompanyName",
            };
            _mockMapper.Setup(m => m.Map<AgentInfo>(agent)).Returns(agentInfo);

            // Act
            var result = await _authService.Login(request);

            // Assert
            Assert.Equal(LoginStatus.Success, result.Status);
            Assert.NotNull(result.UserInfo);
            var userInfo = result.UserInfo;
            Assert.Equal(user.Id, userInfo.Id);
            Assert.Equal(user.UserName, userInfo.UserName);
            Assert.Equal(user.Email, userInfo.Email);
            Assert.Equal("Agent", userInfo.Role);
            Assert.False(string.IsNullOrEmpty(userInfo.Token));
            Assert.True(userInfo.ExpiresAt > DateTime.Now);
            Assert.NotNull(result.AgentInfo);
            Assert.Equal(agent.AgentFirstName, result.AgentInfo.AgentFirstName);
            _mockAuthRepo.Verify(r => r.GetAgentByUserId(user.Id), Times.Once);
            _mockUserActivityLogRepo.Verify(r => r.Insert(
                It.Is<UserActivityLog>(l => 
                    l.UserId == user.Id &&
                    l.UserName == user.UserName &&
                    l.Email == user.Email &&
                    l.Action == "Login" &&
                    l.IsSuccessful == true &&
                    l.Message == "Successfully logged in.")), Times.Once);
        }

        #endregion

        #region GetAllUsers Tests

        [Theory]
        [InlineData(0, 10)]
        [InlineData(3, 0)]
        [InlineData(-2, 5)]
        [InlineData(3, -4)]
        public async Task GetAllUsers_InvalidPageNumberOrSize_ReturnsError(int pageNumber, int pageSize)
        {
            // Act
            var result = await _authService.GetAllUsers(pageNumber, pageSize);

            // Assert
            Assert.Equal(GetUsersStatus.Error, result.Status);
            Assert.Equal("pageNumber and pageSize must be greater than 0.", result.ErrorMessage);
            _mockAuthRepo.Verify(r => r.GetUsersPaginated(pageNumber, pageSize), Times.Never);
        }

        [Fact]
        public async Task GetAllUsers_ValidPageNumberAndSize_ReturnsSuccessWithUsers()
        {
            // Arrange
            var pageNumber = 3;
            var pageSize = 3;

            var users = Enumerable.Range(1, 10).Select(u => new User
            { 
                Id = u.ToString(),
                UserName = $"u-{u}",
                Email = $"u{u}@test.com",
            }).ToList();

            var userDtos = Enumerable.Range(1, 10).Select(u => new UserDto
            {
                Id = u.ToString(),
                UserName = $"u-{u}",
                Email = $"u{u}@test.com",
            }).ToList();

            _mockAuthRepo.Setup(r => r.GetUsersPaginated(pageNumber, pageSize)).ReturnsAsync(users);

            _mockAuthRepo.Setup(r => r.GetUsersTotal()).ReturnsAsync(30);

            _mockMapper.Setup(m => m.Map<List<UserDto>>(users)).Returns(userDtos);

            // Act
            var result = await _authService.GetAllUsers(pageNumber, pageSize);

            // Assert
            Assert.Equal(GetUsersStatus.Success, result.Status);
            Assert.Equal(userDtos, result.Users);
            Assert.Equal(30, result.TotalCount);
            _mockAuthRepo.Verify(r => r.GetUsersPaginated(pageNumber, pageSize), Times.Once);
            _mockAuthRepo.Verify(r => r.GetUsersTotal(), Times.Once);
            _mockMapper.Verify(m => m.Map<List<UserDto>>(users), Times.Once);
        }

        #endregion

        #region GetCurrentUserInfo Tests

        [Fact]
        public async Task GetCurrentUserInfo_InvalidUserId_ReturnUserNotFound()
        {
            // Arrange
            var user = BuildUser(null, "Agent");

            // Act
            var result = await _authService.GetCurrentUserInfo(user);

            // Assert
            Assert.Equal(GetCurrentUserInfoResult.UserNotFound, result.Result);
            Assert.Equal("Unable to find the user id due to user id claim missing.", result.ErrorMessage);
            _mockAuthRepo.Verify(r => r.GetAssignedListingCaseIds(It.IsAny<string>()), Times.Never);
            _mockAuthRepo.Verify(r => r.GetAssociatedListingCaseIds(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetCurrentUserInfo_InvalidRole_ReturnSuccessWithEmptyListingCaseIds()
        {
            // Arrange
            var userId = "test-user";
            var role = "other-role";
            var user = BuildUser(userId, role);

            // Act
            var result = await _authService.GetCurrentUserInfo(user);

            // Assert
            Assert.Equal(GetCurrentUserInfoResult.Success, result.Result);
            Assert.Equal(userId, result.Id);
            Assert.Equal(role, result.Role);
            Assert.NotNull(result.ListingCaseIds);
            Assert.Empty(result.ListingCaseIds);
            _mockAuthRepo.Verify(r => r.GetAssignedListingCaseIds(It.IsAny<string>()), Times.Never);
            _mockAuthRepo.Verify(r => r.GetAssociatedListingCaseIds(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetCurrentUserInfo_ValidRole_ReturnSuccess()
        {
            // Arrange
            var userId = "test-user";
            var role = "Agent";
            var user = BuildUser(userId, role);

            var listingCaseIds = new List<int> { 1, 2, 3};

            _mockAuthRepo.Setup(r => r.GetAssignedListingCaseIds(userId)).ReturnsAsync(listingCaseIds);

            // Act
            var result = await _authService.GetCurrentUserInfo(user);

            // Assert
            Assert.Equal(GetCurrentUserInfoResult.Success, result.Result);
            Assert.Equal(userId, result.Id);
            Assert.Equal(role, result.Role);
            Assert.Equal(listingCaseIds, result.ListingCaseIds);
            _mockAuthRepo.Verify(r => r.GetAssignedListingCaseIds(It.IsAny<string>()), Times.Once);
            _mockAuthRepo.Verify(r => r.GetAssociatedListingCaseIds(It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region UpdatePassword Tests

        [Fact]
        public async Task UpdatePassword_InvalidUserId_ReturnUserNotFound()
        {
            // Arrange
            var user = BuildUser(null, "Agent");

            var request = new UpdatePasswordRequest
            {
                CurrentPassword = "1234Abcd!",
                NewPassword = "NewP@ssword890"
            };

            // Act
            var result = await _authService.UpdatePassword(request, user);

            // Assert
            Assert.Equal(UpdatePasswordResult.UserNotFound, result.Result);
            Assert.Equal("Unable to find the user id due to user id claim missing.", result.ErrorMessage);
            _mockUserManager.Verify(m => m.ChangePasswordAsync(It.IsAny<User>(), request.CurrentPassword, request.NewPassword), Times.Never);
        }

        [Fact]
        public async Task UpdatePassword_UserNotFound_ReturnUserNotFound()
        {
            // Arrange
            var userId = "invalid-user";
            var user = BuildUser(userId, "Agent");

            var request = new UpdatePasswordRequest
            {
                CurrentPassword = "1234Abcd!",
                NewPassword = "NewP@ssword890"
            };

            _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync((User)null);

            // Act
            var result = await _authService.UpdatePassword(request, user);

            // Assert
            Assert.Equal(UpdatePasswordResult.UserNotFound, result.Result);
            Assert.Equal("Unable to find the user in db.", result.ErrorMessage);
            _mockUserManager.Verify(m => m.ChangePasswordAsync(It.IsAny<User>(), request.CurrentPassword, request.NewPassword), Times.Never);
        }

        [Fact]
        public async Task UpdatePassword_CurrentPasswordMismatch_ReturnsInvalidCurrentPassword()
        {
            // Arrange
            var userId = "u1";

            var user = BuildUser(userId, "Agent");

            var dbUser = new User { Id = userId, UserName="test-user", Email="u1@example.com"};

            var request = new UpdatePasswordRequest
            {
                CurrentPassword = "WrongCurrentPassword",
                NewPassword = "NewP@ssword890"
            };

            _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(dbUser);

            var identityErrors = new[] { new IdentityError { Code = "PasswordMismatch", Description = "wrong password" } };

            _mockUserManager.Setup(m => m.ChangePasswordAsync(dbUser, request.CurrentPassword, request.NewPassword))
                            .ReturnsAsync(IdentityResult.Failed(identityErrors));

            _mockUserActivityLogRepo.Setup(r => r.Insert(It.IsAny<UserActivityLog>())).Returns(Task.CompletedTask);

            // Act
            var result = await _authService.UpdatePassword(request, user);

            // Assert
            Assert.Equal(UpdatePasswordResult.InvalidCurrentPassword, result.Result);
            Assert.Equal("Current password is incorrect.", result.ErrorMessage);
            _mockUserManager.Verify(m => m.ChangePasswordAsync(It.IsAny<User>(), request.CurrentPassword, request.NewPassword), Times.Once);
            _mockUserActivityLogRepo.Verify(r => r.Insert(It.IsAny<UserActivityLog>()), Times.Once);
        }

        [Fact]
        public async Task UpdatePassword_NewPasswordInvalid_ReturnsInvalidNewPassword()
        {
            // Arrange
            var userId = "u1";

            var user = BuildUser(userId, "Agent");

            var dbUser = new User { Id = userId, UserName = "test-user", Email = "u1@example.com" };

            var request = new UpdatePasswordRequest
            {
                CurrentPassword = "1234Abcd!",
                NewPassword = "InvalidNewPassword"
            };

            _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(dbUser);

            var identityErrors = new[] { new IdentityError { Code = "PasswordRequiresNumber", Description = "Password must include at least one digital number" } };

            _mockUserManager.Setup(m => m.ChangePasswordAsync(dbUser, request.CurrentPassword, request.NewPassword))
                            .ReturnsAsync(IdentityResult.Failed(identityErrors));

            _mockUserActivityLogRepo.Setup(r => r.Insert(It.IsAny<UserActivityLog>())).Returns(Task.CompletedTask);

            // Act
            var result = await _authService.UpdatePassword(request, user);

            // Assert
            Assert.Equal(UpdatePasswordResult.InvalidNewPassword, result.Result);
            Assert.Contains("Password must include at least one digital number", result.ErrorMessage);
            _mockUserManager.Verify(m => m.ChangePasswordAsync(It.IsAny<User>(), request.CurrentPassword, request.NewPassword), Times.Once);
            _mockUserActivityLogRepo.Verify(r => r.Insert(It.IsAny<UserActivityLog>()), Times.Once);
        }

        [Fact]
        public async Task UpdatePassword_WhenExceptionThrown_ReturnsError()
        {
            // Arrange
            var userId = "u1";

            _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ThrowsAsync(new Exception("something wrong"));

            var user = BuildUser(userId, "Agent");

            var request = new UpdatePasswordRequest
            {
                CurrentPassword = "1234Abcd!",
                NewPassword = "InvalidNewPassword"
            };

            // Act
            var result = await _authService.UpdatePassword(request, user);

            // Assert
            Assert.Equal(UpdatePasswordResult.Error, result.Result);
            Assert.Equal("An unexpected error occurred.", result.ErrorMessage);
        }

        [Fact]
        public async Task UpdatePassword_WhenSucceed_ReturnsSuccess()
        {
            // Arrange
            var userId = "u1";

            var user = BuildUser(userId, "Agent");

            var dbUser = new User { Id = userId, UserName = "test-user", Email = "u1@example.com" };

            var request = new UpdatePasswordRequest
            {
                CurrentPassword = "1234Abcd!",
                NewPassword = "InvalidNewPassword"
            };

            _mockUserManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(dbUser);

            _mockUserManager.Setup(m => m.ChangePasswordAsync(dbUser, request.CurrentPassword, request.NewPassword))
                            .ReturnsAsync(IdentityResult.Success);

            _mockUserActivityLogRepo.Setup(r => r.Insert(It.IsAny<UserActivityLog>())).Returns(Task.CompletedTask);

            // Act
            var result = await _authService.UpdatePassword(request, user);

            // Assert
            Assert.Equal(UpdatePasswordResult.Success, result.Result);
            _mockUserManager.Verify(m => m.ChangePasswordAsync(It.IsAny<User>(), request.CurrentPassword, request.NewPassword), Times.Once);
            _mockUserActivityLogRepo.Verify(r => r.Insert(It.IsAny<UserActivityLog>()), Times.Once);
        }



        #endregion
    }
}
