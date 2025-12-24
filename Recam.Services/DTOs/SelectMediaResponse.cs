using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.DTOs
{
    public class SelectMediaResponse
    {
        public SelectMediaResult Result { get; set; }
        public string? ErrorMessage { get; set; }

        public enum SelectMediaResult
        {
            Success,
            BadRequest,
            Forbidden,
        }
    }
}
