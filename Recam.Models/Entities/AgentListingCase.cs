using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Models.Entities
{
    public class AgentListingCase
    {
        public string AgentId { get; set; }
        public int ListingCaseId { get; set; }
        public Agent Agent { get; set; }
        public ListingCase ListingCase { get; set; }
    }
}
