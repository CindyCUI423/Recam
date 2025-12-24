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
using static Recam.Services.DTOs.DownloadListingCaseMediaZipResponse;
using static Recam.Services.DTOs.GetFinalSelectedMediaResponse;
using static Recam.Services.DTOs.SelectMediaResponse;
using static Recam.Services.DTOs.SetHeroMediaResponse;

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

        /// <summary>
        /// Creates a batch of media assets for the specified listing case.
        /// </summary>
        /// <remarks>Requires the caller to be authorized under the PhotographyCompanyPolicy. Returns 400
        /// Bad Request if the listing case ID is invalid, 403 Forbidden if the user is not permitted to create media
        /// assets for the specified listing, and 500 Internal Server Error for unexpected failures.</remarks>
        /// <param name="id">The unique identifier of the listing case for which media assets are to be created. Must correspond to an
        /// existing listing case.</param>
        /// <param name="request">The request payload containing details of the media assets to be created. Cannot be null.</param>
        /// <returns>An <see cref="IActionResult"/> that represents the result of the operation. Returns a 201 Created response
        /// with the batch creation result if successful; otherwise, returns an error response with the appropriate
        /// status code.</returns>
        [HttpPost("listings/{id}/media/batch")]
        [Authorize(Policy = "PhotographyCompanyPolicy")]
        [ProducesResponseType(typeof(CreateMediaAssetsBatchResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateMediaAssetsBatch(int id, [FromBody] CreateMediaAssetsBatchRequest request)
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
        [ProducesResponseType(StatusCodes.Status200OK)]
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
        /// Returns a 400 Bad Request if the specified media asset does not exist or the blob name can not be found, a 403
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
            else if (result.Result == DeleteMediaAssetResult.Error)
            {
                return BadRequest(
                    new ErrorResponse(StatusCodes.Status400BadRequest,
                        result.ErrorMessage ?? "Unable to extract the valid blob name from media url.",
                        "BlobNameNotFound"));
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

        /// <summary>
        /// Sets the hero (cover) media asset for the specified listing case.
        /// </summary>
        /// <remarks>This endpoint is restricted to users with the Agent role. The specified media asset
        /// must belong to the listing case identified by <paramref name="id"/>. If the operation is not permitted or
        /// the media asset is invalid, an appropriate error response is returned.</remarks>
        /// <param name="id">The unique identifier of the listing case for which to set the hero media asset.</param>
        /// <param name="request">The request containing the ID of the media asset to set as the hero media. Cannot be null.</param>
        /// <returns>A result indicating the outcome of the operation. Returns a 204 No Content response if successful;
        /// otherwise, returns a 400 Bad Request or 403 Forbidden response with error details.</returns>
        [HttpPut("listings/{id}/cover")]
        [Authorize(Policy = "AgentPolicy")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> SetHeroMedia(int id, [FromBody] SetHeroMediaRequest request)
        {
            var result = await _mediaAssetService.SetHeroMedia(id, request.MediaAssetId, User);

            if (result.Result == SetHeroMediaResult.BadRequest)
            {
                return BadRequest(
                    new ErrorResponse(StatusCodes.Status400BadRequest,
                        result.ErrorMessage ?? $"Unable to find a media asset {request.MediaAssetId} that belongs to listing case {id}.",
                        "InvalidId"));
            }
            else if (result.Result == SetHeroMediaResult.Forbidden)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ErrorResponse(StatusCodes.Status403Forbidden,
                        result.ErrorMessage ?? "You are not allowed to access this media assets of this listing case.",
                        "Forbidden"));
            }
            else
            {
                return NoContent();
            }
        }

        /// <summary>
        /// Updates the set of selected media assets for the specified listing in a single batch operation.
        /// </summary>
        /// <remarks>The total number of selected media assets cannot exceed 10. Only users authorized
        /// under the 'AgentPolicy' can perform this operation.</remarks>
        /// <param name="id">The unique identifier of the listing for which the selected media assets are to be updated.</param>
        /// <param name="request">An object containing the details of the media assets to select. Cannot be null.</param>
        /// <returns>A status code indicating the result of the operation: 204 (No Content) if successful; 400 (Bad Request) if
        /// the request is invalid; 401 (Unauthorized) if the user is not authenticated; or 403 (Forbidden) if the user
        /// does not have permission to modify the listing.</returns>
        [HttpPatch("listings/{id}/selected-media")]
        [Authorize(Policy = "AgentPolicy")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> SelectMediaBatch(int id, [FromBody] SelectMediaRequest request)
        {
            var result = await _mediaAssetService.SelectMediaBatch(id, request, User);

            if (result.Result == SelectMediaResult.BadRequest)
            {
                return BadRequest(
                    new ErrorResponse(StatusCodes.Status400BadRequest,
                        result.ErrorMessage ?? "Unable to find the resource, or the total selected media assests are more than 10.",
                        "BadRequest"));
            }
            else if (result.Result == SelectMediaResult.Forbidden)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new ErrorResponse(StatusCodes.Status403Forbidden,
                        result.ErrorMessage ?? "You are not allowed to access this media assets of this listing case.",
                        "Forbidden"));
            }
            else
            {
                return NoContent();
            }

        }

        /// <summary>
        /// Retrieves the final set of media assets selected for a specified listing case.
        /// </summary>
        /// <remarks>Requires authentication. Returns status code 200 with the selected media assets if
        /// successful, 400 if the request is invalid, 401 if the user is unauthorized, or 403 if access to the listing
        /// case is forbidden.</remarks>
        /// <param name="id">The unique identifier of the listing case for which to retrieve the final selected media assets.</param>
        /// <returns>An <see cref="IActionResult"/> containing a <see cref="GetFinalSelectedMediaResponse"/> with the final
        /// selected media assets if successful; otherwise, an <see cref="ErrorResponse"/> with details about the error.</returns>
        [HttpGet("listings/{id}/final-selection")]
        [Authorize]
        [ProducesResponseType(typeof(GetFinalSelectedMediaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetFinalSelectedMedia(int id)
        {
            var result = await _mediaAssetService.GetFinalSelectedMediaForListingCase(id, User);

            if (result.Result == GetFinalSelectedMediaResult.BadRequest)
            {
                return BadRequest(
                    new ErrorResponse(StatusCodes.Status400BadRequest,
                        result.ErrorMessage ?? "Unable to find the resource. Please provide a valid listing case id.",
                        "BadRequest"));
            }
            else if (result.Result == GetFinalSelectedMediaResult.Forbidden)
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

        /// <summary>
        /// Downloads a ZIP archive containing all media files associated with the specified listing case.
        /// </summary>
        /// <remarks>Requires authentication. Returns status code 200 (OK) with the ZIP file on success,
        /// 400 (Bad Request) if the listing case ID is invalid, 401 (Unauthorized) if the user is not authenticated, or
        /// 403 (Forbidden) if the user does not have access to the listing case.</remarks>
        /// <param name="id">The unique identifier of the listing case whose media files are to be downloaded.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>An HTTP response containing the ZIP file of media assets if successful; otherwise, an error response with
        /// the appropriate status code.</returns>
        [HttpGet("listing/{id}/media-zip")]
        [Authorize]
        [Produces("application/zip")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DownloadListingCaseMediaZip(int id, CancellationToken ct)
        {
            var result = await _mediaAssetService.DownloadListingCaseMediaZip(id, User, ct);

            if (result.Result == DownloadZipResult.BadRequest)
            {
                return BadRequest(new ErrorResponse(
                    StatusCodes.Status400BadRequest,
                    result.ErrorMessage ?? "Unable to find the resource. Please provide a valid listing case id.",
                    "InvalidId"));
            }

            if (result.Result == DownloadZipResult.Forbidden)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse(
                    StatusCodes.Status403Forbidden,
                    result.ErrorMessage ?? "You are not allowed to access this listing case.",
                    "Forbidden"));
            }

            return File(result.ZipStream!, "application/zip", result.ZipFileName ?? $"listing-{id}-media.zip");
        }

    
    }
    
}
