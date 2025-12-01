using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.DTOs
{
    public class LoginResponse
    {
        public LoginStatus Status { get; set; }
        public string? ErrorMessage { get; set; }
        public UserDto? UserInfo { get; set; }
        public AgentInfo? AgentInfo { get; set; }
        public PhotographyCompanyInfo? PhotographyCompanyInfo { get; set; }
    }

    public enum LoginStatus
    {
        Success,
        UserNotFound,
        LockedOut,
        NotAllowed,
        InvalidCredentials,
        Error
    }
    public class UserDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class AgentInfo
    {
        public string AgentFirstName { get; set; }
        public string AgentLastName { get; set; }
        public string? AvatarUrl { get; set; }
        public string CompanyName { get; set; }
    }

    public class PhotographyCompanyInfo
    {
        public string PhotographyCompanyName { get; set; }
    }
}
