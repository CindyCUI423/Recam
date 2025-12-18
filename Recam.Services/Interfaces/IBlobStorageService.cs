using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.Interfaces
{
    public interface IBlobStorageService
    {
        Task<string> Upload(Stream fileStream, string fileName, string contentType);
        Task<(Stream Stream, string ContentType)> Download(string fileName);
        Task Delete(string fileName);
    }
}
