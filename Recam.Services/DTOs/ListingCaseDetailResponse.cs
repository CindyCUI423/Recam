using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.DTOs
{
    public class ListingCaseDetailResponse
    {
        public ListingCaseDetailStatus Status { get; set; }
        public string? ErrorMessage { get; set; }
        public ListingCaseDto? ListingCaseInfo { get; set; }
        public List<AgentInfo> Agents { get; set; } = new();
    }

    public enum ListingCaseDetailStatus
    {
        Success,
        BadRequest,
        Forbidden,
    }
}
