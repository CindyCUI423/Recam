using Recam.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.DTOs
{
    public class GetUsersResponse
    {
        public GetUsersStatus Status { get; set; }
        public string? ErrorMessage { get; set; }
        public List<UserDto>? Users { get; set; } = new();
        public int? TotalCount { get; set; }
    }

    public enum GetUsersStatus
    {
        Success,
        Error
    }
    
    public class UserDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }
}
