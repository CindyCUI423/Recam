using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Models.Entities
{
    public class Agent
    {
        [Key]
        [ForeignKey(nameof(User))]
        public string Id { get; set; } // Share the same Id with User
        public User User { get; set; }
        public string AgentFirstName { get; set; }
        public string AgentLastName { get; set; }
        public string? AvatarUrl { get; set; }
        public string CompanyName { get; set; }
        public ICollection<AgentListingCase> AgentListingCases { get; set; } = new List<AgentListingCase>();
        public ICollection<AgentPhotographyCompany> AgentPhotographyCompanies { get; set; } = new List<AgentPhotographyCompany>();

    }
}
