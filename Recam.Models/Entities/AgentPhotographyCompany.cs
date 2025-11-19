using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Models.Entities
{
    public class AgentPhotographyCompany

    {
        public string AgentId { get; set; }
        public string PhotographyCompanyId { get; set; }
        public Agent Agent { get; set; }
        public PhotographyCompany PhotographyCompany { get; set; }

    }
}
