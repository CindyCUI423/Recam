using Recam.Services.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.Interfaces
{
    public interface IMediaAssetService
    {
        Task<int> CreateMediaAsset(int id, CreateMediaAssetRequest request, string userId);
    }
}
