using Microsoft.AspNetCore.Http;
using Recam.Models.Enums;
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
        Task<CreateMediaAssetResponse> CreateMediaAsset(int id, CreateMediaAssetRequest request, ClaimsPrincipal user);
        Task<CreateMediaAssetsBatchResponse> CreateMediaAssetsBatch(int id, CreateMediaAssetsBatchRequest request, ClaimsPrincipal user);
        Task<DeleteMediaAssetResponse> DeleteMediaAsset(int id, ClaimsPrincipal user);
        Task<GetMediaAssetsResponse> GetMediaAssetsByListingCaseId(int id, ClaimsPrincipal user);
        Task<SetHeroMediaResponse> SetHeroMedia(int listingCaseId, int mediaAssetId, ClaimsPrincipal user);
        Task<SelectMediaResponse> SelectMediaBatch(int id, SelectMediaRequest request, ClaimsPrincipal user);
        Task<GetFinalSelectedMediaResponse> GetFinalSelectedMediaForListingCase(int listingCaseId, ClaimsPrincipal user);
        Task<DownloadListingCaseMediaZipResponse> DownloadListingCaseMediaZip(int listingCaseId, ClaimsPrincipal user, CancellationToken ct);

    }
}
