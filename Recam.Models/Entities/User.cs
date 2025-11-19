using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Models.Entities
{
    public class User: IdentityUser
    {
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public Agent Agent { get; set; }
        public PhotographyCompany PhotographyCompany { get; set; }
        public ICollection<MediaAsset> MediaAssets { get; set; } = new List<MediaAsset>();
    }
}

