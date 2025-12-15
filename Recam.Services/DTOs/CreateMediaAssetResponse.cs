using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.DTOs
{
    public class CreateMediaAssetResponse
    {
        public CreateMediaAssetResult Result { get; set; }
        public string? ErrorMessage { get; set; }
        public int? MediaAssetId { get; set; }

        public enum CreateMediaAssetResult
        {
            Success,
            BadRequest,
            Forbidden
        }
    }
}
