using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.DTOs
{
    public class SignUpResponse
    {
        public SignUpStatus Status { get; set; }
        public string? ErrorMessage { get; set; }
        public string? UserId { get; set; }
    }

    public enum SignUpStatus
    {
        Success,
        UserNameAlreadyExists,
        EmailAlreadyExists,
        CreateUserFailure,
        AssignRoleFailure
        
    }
}
