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
        Task<string> SignUp(SignUpRequest request);
        Task<LoginResponse> Login(LoginRequest request);

    }
}
