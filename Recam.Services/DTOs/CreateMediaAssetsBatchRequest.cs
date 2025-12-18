using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.DTOs
{
    public class CreateMediaAssetsBatchRequest
    {
        public Models.Enums.MediaType MediaType { get; set; }
        public List<string> MediaUrls { get; set; }
    }
}
