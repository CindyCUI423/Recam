using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
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
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.Services
{
    public class AuthService : IAuthService
    {

        private readonly IAuthRepository _authRepository;
        private readonly IUserActivityLogRepository _userActivityLogRepository;
        private readonly IConfiguration _configuration;
        private IMapper _mapper;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IValidator<SignUpRequest> _signUpValidator;
        private readonly IUnitOfWork _unitOfWork;

        public AuthService(IAuthRepository authRepository, IUserActivityLogRepository userActivityLogRepository,
            IConfiguration configuration, IMapper mapper,
            UserManager<User> userManager, RoleManager<Role> roleManager, IUnitOfWork unitOfWork)
        {
            _authRepository = authRepository;
            _userActivityLogRepository = userActivityLogRepository;
            _configuration = configuration;
            _mapper = mapper;
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<string> SignUp(SignUpRequest request)
        {
            await _unitOfWork.BeginTransaction();

            var roleType = request.RoleType.Trim();

            // Check if Username is unique
            var existingByName = await _userManager.FindByEmailAsync(request.UserName);
            if (existingByName != null)
            {
                throw new ConflictException("Username is already taken.");
            }

            // Check if Email is unique
            var existingByEmail = await _userManager.FindByEmailAsync(request.Email);
            if (existingByEmail != null)
            {
                throw new ConflictException("Email is already in use.");
            }

            // Create new user
            var user = _mapper.Map<User>(request);
            user.IsDeleted = false;
            user.CreatedAt = DateTime.UtcNow;

            IdentityResult result;

            try
            {
                result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    throw new Exception("Failed to create the user through UserManager");
                }

                // Create Agent or PhotographyCompany
                if (roleType == "Agent" && request.AgentInfo != null)
                {
                    var agent = _mapper.Map<Agent>(request.AgentInfo);
                    agent.Id = user.Id;

                    await _authRepository.AddAgent(agent);
                }

                if (roleType == "PhotographyCompany" && request.PhotographyCompanyInfo != null)
                {
                    var photograhyCompany = _mapper.Map<PhotographyCompany>(request.PhotographyCompanyInfo);
                    photograhyCompany.Id = user.Id;

                    await _authRepository.AddPhotographyCompany(photograhyCompany);
                }

                // Assign Identity Role
                var userRoleResult = await _userManager.AddToRoleAsync(user, roleType);
                if (!userRoleResult.Succeeded)
                {
                    throw new Exception("Failed to assign user role.");
                }

                await _unitOfWork.Commit();

                // Log user activity to MongoDB when successfully signed up
                await LogUserActivity(user.Id, user.UserName, "SignUp", result);

            }
            catch (Exception)
            {
                await _unitOfWork.Rollback();
                throw;
            }

            
            

            // Return user Id when successful
            return user.Id;
        }

        private string JWTGenerator(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
            };

            var roles = _userManager.GetRolesAsync(user).Result;

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
                expires: DateTime.Now.AddDays(7),
                signingCredentials: signature
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task LogUserActivity(string userId, string userName, string action, IdentityResult result)
        {
            var log = new UserActivityLog
            {
                UserId = userId,
                UserName = userName,
                OccurredAt = DateTime.UtcNow,
                Action = action,
                IsSuccessful = result.Succeeded
            };

            try
            {
                await _userActivityLogRepository.Insert(log);
            }
            catch (Exception exception)
            {
                // TODO: 日志写失败不要影响业务流程，只在内部记录一下即可
            }
        }

    }
}
