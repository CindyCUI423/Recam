using Recam.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.DTOs
{
    public class ChangeListingCaseStatusResponse
    {
        public ChangeListingCaseStatusResult Result { get; set; }
        public string? ErrorMessage { get; set; }  
        public ListingCaseStatus? oldStatus { get; set; }
        public ListingCaseStatus? newStatus { get; set; }

    }

    public enum ChangeListingCaseStatusResult
    {
        Success,
        InvalidId,
        Forbidden,
    }
}
