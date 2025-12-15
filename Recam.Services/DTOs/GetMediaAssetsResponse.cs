using Recam.Models.Entities;
using Recam.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.DTOs
{
    public class GetMediaAssetsResponse
    {
        public GetMediaAssetsResult Result { get; set; }
        public string? ErrorMessage { get; set; }
        public List<MediaAssetDto>? MediaAssets  { get; set; }
    }

    public enum GetMediaAssetsResult
    {
        Success,
        BadRequest,
        Forbidden,
    }

    public class MediaAssetDto
    {
        public int Id { get; set; }
        public MediaType MediaType { get; set; }
        public string MediaUrl { get; set; }
        public DateTime UploadedAt { get; set; }
        public bool IsSelect { get; set; }
        public bool IsHero { get; set; }
        public int ListingCaseId { get; set; }
        public string UserId { get; set; }
    }
}
