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

        /// <summary>
        /// Get listing cases (support pagination):
        /// - PhotographyCompany users can only get the listcase created under that account
        /// - Agent users can only get those assigned to them
        /// </summary>
        /// <param name="pageNumber">Number of the page</param>
        /// <param name="pageSize">Numbers of users retrieved per page</param>
        /// <returns>
        /// All the details of each list case on success
        /// </returns>
        [HttpGet("listings")]
        [Authorize]
        [ProducesResponseType(typeof(GetListingCasesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAllListingCases([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            // Get user id from JWT
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get user's role from JWT
            var role = User.FindFirstValue(ClaimTypes.Role);

            var result = await _listingCaseService.GetListingCasesByUser(pageNumber, pageSize, userId, role);

            if (result.Status == GetListingCasesStatus.BadRequest)
            {
                return BadRequest(
                       new ErrorResponse(StatusCodes.Status400BadRequest,
                           result.ErrorMessage ?? "pageNumber and pageSize must be greater than 0.",
                           "InvalidPagination"));
            }
            else if (result.Status == GetListingCasesStatus.Unauthorized)
            {
                return Unauthorized(
                       new ErrorResponse(StatusCodes.Status401Unauthorized,
                           result.ErrorMessage ?? "User is not authenticated or invalid user role.",
                           "Unauthorized"));
            }
            else
            {
                return Ok(result);
            }
        }


    }
}
