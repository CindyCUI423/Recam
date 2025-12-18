using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.DTOs
{
    public class CreateMediaAssetsBatchResponse
    {
        public CreateMediaAssetsBatchResult Result { get; set; }
        public string? ErrorMessage { get; set; }
        public List<int>? MediaAssetIds { get; set; }

        public enum CreateMediaAssetsBatchResult
        {
            Success,
            BadRequest,
            Forbidden
        }
    }
}
