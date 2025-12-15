using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Recam.Common.Exceptions;
using Recam.Models.Entities;
using Recam.Services.DTOs;
using Recam.Services.Interfaces;
using System.Security.Claims;

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
            // Get user id from JWT
            var userId = _userManager.GetUserId(User);

            // If userId is null, return Unauthorized
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new ErrorResponse(
                    StatusCodes.Status401Unauthorized,
                    "User cannot found.",
                    "InvalidUser"));
            }

            var mediaAssetId = await _mediaAssetService.CreateMediaAsset(id, request, userId);

            return StatusCode(StatusCodes.Status201Created, new CreateMediaAssetResponse { MediaAssetId = mediaAssetId });
        }
    }
}
