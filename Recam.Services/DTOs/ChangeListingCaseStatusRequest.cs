using Recam.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.DTOs
{
    public class ChangeListingCaseStatusRequest
    {
        public ListingCaseStatus Status { get; set; }
    }
}
