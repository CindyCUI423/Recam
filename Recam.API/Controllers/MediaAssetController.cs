using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Recam.Common.Exceptions;
using Recam.Models.Entities;
using Recam.Models.Enums;
using Recam.Services.DTOs;
using Recam.Services.Interfaces;
using System.Security.Claims;
using static Recam.Services.DTOs.CreateMediaAssetResponse;
using static Recam.Services.DTOs.CreateMediaAssetsBatchResponse;
using static Recam.Services.DTOs.DeleteMediaAssetResponse;

namespace Recam.API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class MediaAssetController : ControllerBase
    {
        private IMediaAssetService _mediaAssetService;
        private UserManager<User> _userManager;
        private IBlobStorageService _blobStorageService;
        public MediaAssetController(IMediaAssetService mediaAssetService, UserManager<User> userManager, IBlobStorageService blobStorageService)
        {
            _mediaAssetService = mediaAssetService;
            _userManager = userManager;
            _blobStorageService = blobStorageService;
        }

        /// <summary>
        /// Uploads one or more media files to the server and returns the URLs of the uploaded assets.
        /// </summary>
        /// <remarks>Only media type Photo supports uploading multiple files in a single request. For
        /// other media types, the request must contain exactly one file. All files must be non-empty. The user must be
        /// authorized with the PhotographyCompanyPolicy.</remarks>
        /// <param name="files">The collection of files to upload. For media type Photo, multiple files are allowed; for other types, only a
        /// single file is permitted. Each file must have a nonzero length.</param>
        /// <param name="type">The type of media being uploaded. Determines whether multiple files are allowed.</param>
        /// <returns>A 201 Created response containing the URLs of the uploaded media assets if the upload is successful;
        /// otherwise, a 400 Bad Request or an appropriate error response.</returns>
        [HttpPost("upload")]
        [Authorize(Policy = "PhotographyCompanyPolicy")]
        [ProducesResponseType(typeof(UploadMediaAssetsResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UploadMediaAsset([FromForm] List<IFormFile> files, [FromForm] Models.Enums.MediaType type)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new ErrorResponse(400, "Files can not be null or empty", "InvalidFiles"));
            }

            // Only photo type allow multiple files upload
            if (type != Models.Enums.MediaType.Photo && files.Count != 1)
            {
                return BadRequest(new ErrorResponse(400, "Only Photo type allows multiple files upload.", "InvalidFilesCount"));
            }

            List<string> urls = new List<string>();

            foreach (var file in files)
            {
                // Ensure file byte count > 0
                if (file.Length <= 0)
                {
                    return BadRequest(new ErrorResponse(400, "Empty file is not allowed.", "InvalidFile"));
                }

                // Create a unique file name (avoid using the original file name in case there's special/invalid character)
                var ext = Path.GetExtension(file.FileName);
                var blobName = $"{Guid.NewGuid():N}{ext}";

                using var stream = file.OpenReadStream(); // File --> Stream
                var url = await _blobStorageService.Upload(stream, blobName, file.ContentType);

                urls.Add(url);
            }

            return StatusCode(StatusCodes.Status201Created, new UploadMediaAssetsResponse { Urls = urls });
        }

        /// <summary>
        /// Creates a new media asset and associates it with the specified listing.
        /// </summary>
        /// <remarks>
        /// This action requires authentication and is restricted to users authorized under the
        /// PhotographyCompanyPolicy.
        /// </remarks>
        /// <param name="id">The unique identifier of the listing to which the media asset will be added.</param>
        /// <param name="request">The details of the media asset to create. Cannot be null.</param>
        /// <returns>A 201 Created response containing the identifier of the newly created media asset.</returns>
        [HttpPost("listings/{id}/media")]
        [Authorize(Policy = "PhotographyCompanyPolicy")]
        [ProducesResponseType(typeof(CreateMediaAssetResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateMediaAsset(int id, [FromBody] CreateMediaAssetRequest request)
        {
            var result = await _mediaAssetService.CreateMediaAsset(id, request, User);

            if (result.Result == CreateMediaAssetResult.BadRequest)
            {
                return BadRequest(
                    new ErrorResponse(StatusCodes.Status400BadRequest,
                        result.ErrorMessage ?? "Unable to find the resource. Please provide a valid listing case id.",
                        "InvalidId"));
            }
            else if (result.Result == CreateMediaAssetResult.Forbidden)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ErrorResponse(StatusCodes.Status403Forbidden,
                        result.ErrorMessage ?? "You are not allowed to create media asset for this listing case.",
                        "Forbidden"));
            }
            else
            {
                return StatusCode(StatusCodes.Status201Created, result);
            }
        }


        [HttpPost("listings/{id}/media/batch")]
        [Authorize(Policy = "PhotographyCompanyPolicy")]
        [ProducesResponseType(typeof(CreateMediaAssetsBatchResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateMediaAsset(int id, [FromBody] CreateMediaAssetsBatchRequest request)
        {
            var result = await _mediaAssetService.CreateMediaAssetsBatch(id, request, User);

            if (result.Result == CreateMediaAssetsBatchResult.BadRequest)
            {
                return BadRequest(
                    new ErrorResponse(StatusCodes.Status400BadRequest,
                        result.ErrorMessage ?? "Unable to find the resource. Please provide a valid listing case id.",
                        "InvalidId"));
            }
            else if (result.Result == CreateMediaAssetsBatchResult.Forbidden)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ErrorResponse(StatusCodes.Status403Forbidden,
                        result.ErrorMessage ?? "You are not allowed to create media asset for this listing case.",
                        "Forbidden"));
            }
            else
            {
                return StatusCode(StatusCodes.Status201Created, result);
            }
        }


        /// <summary>
        /// Returns the specified media asset as a downloadable file stream.
        /// </summary>
        /// <remarks>The caller must be authorized to access the requested media asset. If the file does
        /// not exist or the request is invalid, an error response is returned with details.</remarks>
        /// <param name="fileName">The name of the media asset file to download. Cannot be null or empty.</param>
        /// <returns>An <see cref="FileStreamResult"/> containing the requested media asset if found; otherwise, an error
        /// response.</returns>
        [HttpGet("download")]
        [Authorize]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DownloadMediaAsset(string fileName)
        {
            var (stream, contentType)  = await _blobStorageService.Download(fileName);

            // Stream --> File
            return File(stream, contentType, fileDownloadName: fileName);
        }

        /// <summary>
        /// Deletes the media asset with the specified identifier.
        /// </summary>
        /// <remarks>
        /// Requires authentication and the PhotographyCompanyPolicy authorization policy.
        /// Returns a 400 Bad Request if the specified media asset does not exist or the identifier is invalid, a 403
        /// Forbidden if the user does not have permission to delete the asset, and a 401 Unauthorized if the user is
        /// not authenticated.
        /// </remarks>
        /// <param name="id">The unique identifier of the media asset to delete. Must correspond to an existing media asset.</param>
        /// <returns>
        /// A 204 No Content response if the media asset is successfully deleted.
        /// </returns>
        [HttpDelete("id")]
        [Authorize(Policy = "PhotographyCompanyPolicy")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteMediaAsset(int id)
        {
            var result = await _mediaAssetService.DeleteMediaAsset(id, User);

            if (result.Result == DeleteMediaAssetResult.BadRequest)
            {
                return BadRequest(
                    new ErrorResponse(StatusCodes.Status400BadRequest,
                        result.ErrorMessage ?? "Unable to find the resource. Please provide a valid media asset id.",
                        "InvalidId"));
            }
            else if (result.Result == DeleteMediaAssetResult.Forbidden)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ErrorResponse(StatusCodes.Status403Forbidden,
                        result.ErrorMessage ?? "You are not allowed to access this media asset.",
                        "Forbidden"));
            }
            else
            {
                return NoContent();
            }
        }

        /// <summary>
        /// Retrieves the collection of media assets associated with the specified listing case.
        /// </summary>
        /// <remarks>
        /// Requires authentication. Returns status code 200 (OK) with the media assets on
        /// success, 400 (Bad Request) if the listing case ID is invalid, 401 (Unauthorized) if the user is not
        /// authenticated, or 403 (Forbidden) if the user does not have permission to access the media assets for the
        /// specified listing case.
        /// </remarks>
        /// <param name="id">The unique identifier of the listing case for which to retrieve media assets. Must be a valid, existing
        /// listing case ID.</param>
        /// <returns>
        /// A list of media assets of the sopecified listing case on success.
        /// </returns>
        [HttpGet("listings/{id}/media")]
        [Authorize]
        [ProducesResponseType(typeof(GetMediaAssetsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMediaAssetsByListingCaseId(int id)
        {
            var result = await _mediaAssetService.GetMediaAssetsByListingCaseId(id, User);

            if (result.Result == GetMediaAssetsResult.BadRequest)
            {
                return BadRequest(
                    new ErrorResponse(StatusCodes.Status400BadRequest,
                        result.ErrorMessage ?? "Unable to find the resource. Please provide a valid listing case id.",
                        "InvalidId"));
            }
            else if (result.Result == GetMediaAssetsResult.Forbidden)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ErrorResponse(StatusCodes.Status403Forbidden,
                        result.ErrorMessage ?? "You are not allowed to access this media assets of this listing case.",
                        "Forbidden"));
            }
            else
            {
                return Ok(result);
            }

        }
    }
}
