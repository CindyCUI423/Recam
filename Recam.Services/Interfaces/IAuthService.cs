using Recam.Services.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.Interfaces
{
    public interface IAuthService
    {
        Task<SignUpResponse> SignUp(SignUpRequest request);
        Task<LoginResponse> Login(LoginRequest request);
        Task<GetUsersResponse> GetAllUsers(int pageNumber, int pageSize);
        Task<GetCurrentUserInfoResponse> GetCurrentUserInfo(ClaimsPrincipal user);
        Task<UpdatePasswordResponse> UpdatePassword(UpdatePasswordRequest request, ClaimsPrincipal user);
    }
}
