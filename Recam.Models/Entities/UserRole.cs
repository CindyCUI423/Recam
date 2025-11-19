using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Models.Entities
{
    public class UserRole: IdentityUserRole<string>
    {
        public User User { get; set; }
        public Role Role { get; set; }
    }
}
