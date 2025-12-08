using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Recam.Common.Exceptions;
using Recam.Services.DTOs;
using Recam.Services.Interfaces;
using System.Security.Claims;

namespace Recam.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ListingCaseController : ControllerBase
    {
        private IListingCaseService _listingCaseService;
        public ListingCaseController(IListingCaseService listingCaseService)
        {
            _listingCaseService = listingCaseService;
        }

        /// <summary>
        /// For PhotographyCompanies to create new listing cases
        /// </summary>
        /// <param name="request">Necessary listing case information</param>
        /// <returns>
        /// ListingCase Id on success
        /// </returns>
        [HttpPost]
        [Authorize(Policy = "PhotographyCompanyPolicy")]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateListingCase([FromBody] CreateListingCaseRequest request)
        {
            // Get user id from JWT
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var id = await _listingCaseService.CreateListingCase(request, userId);

            return StatusCode(StatusCodes.Status201Created, new { listingCaseId = id });
        }

  
    }
}
