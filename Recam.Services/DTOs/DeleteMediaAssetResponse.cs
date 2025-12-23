using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.DTOs
{
    public class DeleteMediaAssetResponse
    {
        public DeleteMediaAssetResult Result { get; set; }
        public string? ErrorMessage { get; set; }
        public enum DeleteMediaAssetResult
        {
            Success,
            BadRequest,
            Forbidden,
            Error
        }
    }
}
