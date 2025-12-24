using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.DTOs
{
    public class DownloadListingCaseMediaZipResponse
    {
        public DownloadZipResult Result { get; set; }
        public string? ErrorMessage { get; set; }
        public Stream? ZipStream { get; set; }
        public string? ZipFileName { get; set; }

        public enum DownloadZipResult
        { 
            Success,
            BadRequest,
            Forbidden,
        };
    }
}
