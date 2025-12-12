using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.DTOs
{
    public class DeleteListingCaseResponse
    {
        public DeleteListingCaseResult Result { get; set; }
        public string? ErrorMessage { get; set; }

        public enum DeleteListingCaseResult
        {
            Success,
            InvalidId,
            Forbidden
        }
    }
}
