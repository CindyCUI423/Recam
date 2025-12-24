using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.DTOs
{
    public class GetFinalSelectedMediaResponse
    {
        public GetFinalSelectedMediaResult Result { get; set; }
        public string? ErrorMessage { get; set; }
        public List<MediaAssetDto>? SelectedMediaAssets { get; set; }

        public enum GetFinalSelectedMediaResult
        {
            Success,
            BadRequest,
            Forbidden,
        }
    }
}
