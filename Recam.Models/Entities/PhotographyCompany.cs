using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Models.Entities
{
    public class PhotographyCompany
    {
        [Key]
        [ForeignKey(nameof(User))]
        public string Id { get; set; } // Share the same Id with User
        public User User { get; set; }
        public string PhotographyCompanyName { get; set; }
        public ICollection<AgentPhotographyCompany> AgentPhotographyCompanies { get; set; } = new List<AgentPhotographyCompany>();
    }
}
