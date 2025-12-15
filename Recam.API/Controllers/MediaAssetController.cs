using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Recam.Common.Exceptions;
using Recam.Models.Entities;
using Recam.Services.DTOs;
using Recam.Services.Interfaces;
using System.Security.Claims;
using static Recam.Services.DTOs.CreateMediaAssetResponse;
using static Recam.Services.DTOs.DeleteMediaAssetResponse;

namespace Recam.API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class MediaAssetController : ControllerBase
    {
        private IMediaAssetService _mediaAssetService;
        private UserManager<User> _userManager;
        public MediaAssetController(IMediaAssetService mediaAssetService, UserManager<User> userManager)
        {
            _mediaAssetService = mediaAssetService;
            _userManager = userManager;
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
