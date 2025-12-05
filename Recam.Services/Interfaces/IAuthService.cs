using Recam.Services.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.Interfaces
{
    public interface IAuthService
    {
        Task<SignUpResponse> SignUp(SignUpRequest request);
        Task<LoginResponse> Login(LoginRequest request);
        Task<GetUsersResponse> GetAllUsers(int pageNumber, int pageSize);
    }
}
