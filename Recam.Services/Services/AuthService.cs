using AutoMapper;
using DnsClient.Internal;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using Recam.Common.Exceptions;
using Recam.Models.Collections;
using Recam.Models.Entities;
using Recam.Repositories.Interfaces;
using Recam.Services.DTOs;
using Recam.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static Recam.Services.DTOs.GetCurrentUserInfoResponse;
using static Recam.Services.DTOs.UpdatePasswordResponse;
using Role = Recam.Models.Entities.Role;

namespace Recam.Services.Services
{
    public class AuthService : IAuthService
    {

        private readonly IAuthRepository _authRepository;
        private readonly IUserActivityLogRepository _userActivityLogRepository;
        private readonly IConfiguration _configuration;
        private IMapper _mapper;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IAuthRepository authRepository, IUserActivityLogRepository userActivityLogRepository,
            IConfiguration configuration, IMapper mapper,
            UserManager<User> userManager, SignInManager<User> signInManager,
            IUnitOfWork unitOfWork, ILogger<AuthService> logger)
        {
            _authRepository = authRepository;
            _userActivityLogRepository = userActivityLogRepository;
            _configuration = configuration;
            _mapper = mapper;
            _userManager = userManager;
            _signInManager = signInManager;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<SignUpResponse> SignUp(SignUpRequest request)
        {
            _logger.LogInformation(
                "Start signing up new user. UserName={UserName}, Email={Email}",
                request.UserName,
                request.Email);
            
            await _unitOfWork.BeginTransaction();

            var roleType = request.RoleType.Trim();

            try
            {
                // Check if Username is unique
                var existingByName = await _userManager.FindByNameAsync(request.UserName);
                if (existingByName != null)
                {
                    _logger.LogWarning(
                        "Failed to sign up new user because user name already exists. UserName={UserName}",
                        request.UserName);

                    return new SignUpResponse
                    {
                        Status = SignUpStatus.UserNameAlreadyExists,
                        ErrorMessage = "Username is already taken."
                    };
                }

                // Check if Email is unique
                var existingByEmail = await _userManager.FindByEmailAsync(request.Email);
                if (existingByEmail != null)
                {
                    _logger.LogWarning(
                        "Failed to sign up new user because emial already exists. Email={Email}",
                        request.Email);

                    return new SignUpResponse
                    {
                        Status = SignUpStatus.EmailAlreadyExists,
                        ErrorMessage = "Email is already in use."
                    };
                }

                // Create new user
                var user = _mapper.Map<User>(request);
                user.IsDeleted = false;
                user.CreatedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Persisting new user. UserName={UserName}, Email={Email}",
                    request.UserName,
                    request.Email);

                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    // Get message from IdentityResult
                    var message = string.Join(";", result.Errors.Select(e => e.Description));

                    await _unitOfWork.Rollback();

                    _logger.LogError(
                        "Failed to create new user through User Manager. UserName={UserName}, Email={Email}",
                        request.UserName,
                        request.Email);

                    return new SignUpResponse
                    {
                        Status = SignUpStatus.CreateUserFailure,
                        ErrorMessage= message
                    };
                }

                // Create Agent or PhotographyCompany
                if (roleType == "Agent" && request.AgentInfo != null)
                {
                    var agent = _mapper.Map<Agent>(request.AgentInfo);
                    agent.Id = user.Id;

                    _logger.LogInformation(
                        "Creating the agent for the new user. UserName={UserName}, Email={Email}",
                        request.UserName,
                        request.Email);

                    await _authRepository.AddAgent(agent);
                }

                if (roleType == "PhotographyCompany" && request.PhotographyCompanyInfo != null)
                {
                    var photograhyCompany = _mapper.Map<PhotographyCompany>(request.PhotographyCompanyInfo);
                    photograhyCompany.Id = user.Id;

                    _logger.LogInformation(
                        "Creating the photography company for the new user. UserName={UserName}, Email={Email}",
                        request.UserName,
                        request.Email);

                    await _authRepository.AddPhotographyCompany(photograhyCompany);
                }

                // Assign Identity Role
                var userRoleResult = await _userManager.AddToRoleAsync(user, roleType);
                if (!userRoleResult.Succeeded)
                {
                    var message = string.Join(";", userRoleResult.Errors.Select(e => e.Description));

                    await _unitOfWork.Rollback();

                    _logger.LogError(
                        "Failed to assign role to the new user through User Manager. UserName={UserName}, Email={Email}, Role={Role}",
                        request.UserName,
                        request.Email,
                        roleType);

                    return new SignUpResponse
                    {
                        Status = SignUpStatus.AssignRoleFailure,
                        ErrorMessage = message
                    };
                }

                await _unitOfWork.Commit();

                // Log user activity to MongoDB when successfully signed up
                await LogUserActivity(user.Id, user.UserName, user.Email, "SignUp", result.Succeeded, "Successfully signed up.");

                _logger.LogInformation(
                    "SignUp completed. UserId={UserId}, UserName={UserName}, Email={Email}, Role={Role}",
                    user.Id,
                    user.UserName,
                    user.Email,
                    roleType);

                // On success
                return new SignUpResponse
                {
                    Status = SignUpStatus.Success,
                    UserId = user.Id,
                };

            }
            catch (Exception ex)
            {
                await _unitOfWork.Rollback();

                _logger.LogError(
                        ex,
                        "Failed to sign up the new user. UserName={UserName}, Email={Email}, Role={Role}",
                        request.UserName,
                        request.Email,
                        roleType);

                throw;
            }
        }

        public async Task<LoginResponse> Login(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            _logger.LogInformation(
                "Start logining. Email={Email}",
                request.Email);

            // User not found
            if (user == null)
            {
                // Log failed user login attempts to MongoDB for security purpose
                await LogUserActivity(userId: null, userName: null, request.Email, "Login", false, LoginStatus.UserNotFound.ToString());

                _logger.LogWarning(
                    "Failed to find the user. Email={Email}",
                    request.Email);

                return new LoginResponse
                {
                    Status = LoginStatus.UserNotFound,
                    ErrorMessage = "User not found.",
                };
            }

            var loginResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            // Login failure
            if (!loginResult.Succeeded)
            {
                var status = loginResult.IsLockedOut
                    ? LoginStatus.LockedOut
                    : loginResult.IsNotAllowed
                        ? LoginStatus.NotAllowed
                        : LoginStatus.InvalidCredentials;

                // Log failed user login attempts to MongoDB for security purpose
                await LogUserActivity(user.Id, user.UserName, user.Email, "Login", false, status.ToString());

                _logger.LogWarning(
                    "Login in error. Email={Email}",
                    request.Email);

                return new LoginResponse
                {
                    Status = status,
                    ErrorMessage = status switch
                    {
                        LoginStatus.LockedOut => "Your account is locked out. Please try again later.",
                        LoginStatus.NotAllowed => "Your are not allowed to login. Please verify your email first.",
                        _ => "Incorrect email or password."
                    }
                };
            }

            // Login success
            _logger.LogInformation(
                "Generating JWT. Email={Email}",
                request.Email);

            var token = await JWTGenerator(user, 7);

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles[0];

            // Log successful user login activity
            await LogUserActivity(user.Id, user.UserName, user.Email, "Login", true, "Successfully logged in.");

            var response = new LoginResponse
            {
                Status = LoginStatus.Success,
                UserInfo = new UserLoginDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Role = role,
                    Token = token,
                    ExpiresAt = DateTime.Now.AddDays(7)
                }

            };

            if (role == "Agent")
            {
                var agent = await _authRepository.GetAgentByUserId(user.Id);

                if (agent != null)
                {
                    response.AgentInfo = _mapper.Map<AgentInfo>(agent);
                }
            }
            else if (role == "PhotographyCompany")
            {
                var photographyCompany = await _authRepository.GetPhotographyCompanyByUserId(user.Id);

                if (photographyCompany != null)
                {
                    response.PhotographyCompanyInfo = _mapper.Map<PhotographyCompanyInfo>(photographyCompany);
                }
            }

            _logger.LogInformation(
                "Login completed. UserId={UserId}, Email={Email}, Role={Role}",
                user.Id,
                user.Email,
                role);

            return response;
        }

        private async Task<string> JWTGenerator(User user, int days)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
            };

            var roles = await _userManager.GetRolesAsync(user);

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));

            var signature = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(days),
                signingCredentials: signature
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<GetUsersResponse> GetAllUsers(int pageNumber, int pageSize)
        {
            _logger.LogInformation(
                  "Start retrieving all users. PageNumber={PageNumber}, PageSize={PageSize}",
                  pageNumber,
                  pageSize);

            if (pageNumber < 1 || pageSize < 1)
            {
                _logger.LogWarning(
                    "Invalid pageNumber or pageSize when retrieving all users. PageNumber={PageNumber}, PageSize={PageSize}",
                    pageNumber,
                    pageSize);

                return new GetUsersResponse
                {
                    Status = GetUsersStatus.Error,
                    ErrorMessage = "pageNumber and pageSize must be greater than 0."
                };
            }

            _logger.LogInformation(
                  "Retrieving all users.");

            var users = await _authRepository.GetUsersPaginated(pageNumber, pageSize);
            var totalCount = await _authRepository.GetUsersTotal();

            var userDtos = _mapper.Map<List<UserDto>>(users);

            var userIds = userDtos.Select(x => x.Id).ToList();

            _logger.LogInformation(
                  "GetAllUsers completed. TotalUsersCount={TotalUsersCount}, PageNumber={PageNumber}, PageSize={PageSize}, UserIds={UserIds}",
                  totalCount,
                  pageNumber,
                  pageSize,
                  userIds);

            return new GetUsersResponse
            {
                Status = GetUsersStatus.Success,
                Users = userDtos,
                TotalCount = totalCount
            };
        }

        public async Task<GetCurrentUserInfoResponse> GetCurrentUserInfo(ClaimsPrincipal user)
        {
            // Get the user id
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Unable to find the user id due to user id claim missing.");
                
                return new GetCurrentUserInfoResponse
                {
                    Result = GetCurrentUserInfoResult.UserNotFound,
                    ErrorMessage = "Unable to find the user id due to user id claim missing."
                };
            }

            // Get the user role
            var role = user.FindFirstValue(ClaimTypes.Role);


            _logger.LogInformation(
                  "Start retrieving the current user information. UserId={UserId}, Role={Role}",
                  userId,
                  role);

            // Get assigned listing case ids
            var listingIds = new List<int>();

            if (role == "Agent")
            {
                listingIds = await _authRepository.GetAssignedListingCaseIds(userId);
            }
            else if (role == "PhotographyCompany")
            {
                listingIds = await _authRepository.GetAssociatedListingCaseIds(userId);
            }

            _logger.LogInformation(
                "GetCurrentUserInfo completed. UserId={UserId}, Role={Role}, ListingCaseIds={ListingCaseIds}",
                userId,
                role,
                listingIds);

            return new GetCurrentUserInfoResponse
            {
                Result = GetCurrentUserInfoResult.Success,
                Id = userId,
                Role = role,
                ListingCaseIds = listingIds
            };

        }

        public async Task<UpdatePasswordResponse> UpdatePassword(UpdatePasswordRequest request, ClaimsPrincipal user)
        {
            // Get the user id
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Unable to find the user id due to user id claim missing.");

                return new UpdatePasswordResponse
                {
                    Result = UpdatePasswordResult.UserNotFound,
                    ErrorMessage = "Unable to find the user id due to user id claim missing."
                };
            }

            try
            {
                // Find the user from db
                var dbUser = await _userManager.FindByIdAsync(userId);

                if (dbUser == null)
                {
                    _logger.LogWarning(
                        "Unable to find the user in db. UserId={UserId}",
                        userId);

                    return new UpdatePasswordResponse
                    {
                        Result = UpdatePasswordResult.UserNotFound,
                        ErrorMessage = "Unable to find the user in db."
                    };
                }

                // Update the password
                var result = await _userManager.ChangePasswordAsync(
                    dbUser,
                    request.CurrentPassword,
                    request.NewPassword);

                if (!result.Succeeded)
                {
                    var errorCodes = result.Errors.Select(e => e.Code).ToList();
                    var message = string.Join(";", result.Errors.Select(e => e.Description));

                    // Invalid current password
                    if (errorCodes.Any(c => string.Equals(c, "PasswordMismatch", StringComparison.OrdinalIgnoreCase)))
                    {
                        // Log the user activity
                        await LogUserActivity(userId, dbUser.UserName, dbUser.Email, "ChangePassword", false, "Failed to update password because of the wrong current password");

                        _logger.LogWarning(
                               "Failed to update password because of the wrong current password. UserId={UserId}",
                               userId);

                        return new UpdatePasswordResponse
                        {
                            Result = UpdatePasswordResult.InvalidCurrentPassword,
                            ErrorMessage = "Current password is incorrect."
                        };
                    }

                    // Log the user activity
                    await LogUserActivity(userId, dbUser.UserName, dbUser.Email, "ChangePassword", false, message);

                    // Fail the password policy
                    _logger.LogWarning(
                            "Failed to update password due to password policy. UserId={UserId}, Errors={Errors}",
                            userId,
                            message);

                    return new UpdatePasswordResponse
                    {
                        Result = UpdatePasswordResult.InvalidNewPassword,
                        ErrorMessage = message
                    };

                }

                // Log the user activity
                await LogUserActivity(userId, dbUser.UserName, dbUser.Email, "ChangePassword", true, null);

                _logger.LogInformation(
                    "Password updated successfully. UserId={UserId}, Email={Email}",
                    userId,
                    dbUser.Email);

                return new UpdatePasswordResponse
                {
                    Result = UpdatePasswordResult.Success
                };
            }
            catch (Exception ex) 
            {
                _logger.LogError(
                    ex, 
                    "Unexpected error when updating password. UserId={UserId}",
                    userId);

                return new UpdatePasswordResponse
                {
                    Result = UpdatePasswordResult.Error,
                    ErrorMessage = "An unexpected error occurred."
                };
            }


        }

        private async Task LogUserActivity(string? userId, string? userName, string email, string action, bool result, string? message)
        {
            var log = new UserActivityLog
            {
                UserId = userId,
                UserName = userName,
                Email = email,
                OccurredAt = DateTime.UtcNow,
                Action = action,
                IsSuccessful = result,
                Message = message,
            };

            try
            {
                await _userActivityLogRepository.Insert(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to log user activity. " +
                    "UserId={UserId}, Action={Action}, Result={Result}",
                    userId,
                    action,
                    result
                );
            }
        }

        
    }
}
