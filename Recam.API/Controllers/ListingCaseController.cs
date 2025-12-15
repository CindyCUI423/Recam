using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Recam.Common.Exceptions;
using Recam.Services.DTOs;
using Recam.Services.Interfaces;
using System.Security.Claims;
using static Recam.Services.DTOs.DeleteListingCaseResponse;

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
        [ProducesResponseType(typeof(CreateListingCaseResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateListingCase([FromBody] CreateListingCaseRequest request)
        {
            // Get user id from JWT
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // If userId is null, return Unauthorized
            if (string.IsNullOrWhiteSpace(null))
            {
                return Unauthorized(
                    new ErrorResponse(StatusCodes.Status401Unauthorized,
                        "User is not authenticated.",
                        "InvalidUser"));
            }

            var id = await _listingCaseService.CreateListingCase(request, userId);

            return StatusCode(StatusCodes.Status201Created, new CreateListingCaseResponse { ListingCaseId = id });
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

            // If userId is null, return Unauthorized
            if (string.IsNullOrWhiteSpace(null))
            {
                return Unauthorized(
                    new ErrorResponse(StatusCodes.Status401Unauthorized,
                        "User is not authenticated.",
                        "InvalidUser"));
            }

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

        /// <summary>
        /// Retrieves the details of a listing case identified by the specified ID for the authenticated user
        /// </summary>
        /// <remarks>This method requires the caller to be authenticated. Access to the listing case may
        /// be restricted based on the user's role or permissions.</remarks>
        /// <param name="id">The unique identifier of the listing case to retrieve. Must be a positive integer.</param>
        /// <returns>
        /// Listing case details including associated agents information on success
        /// </returns>
        [HttpGet("listings/{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ListingCaseDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetListingCaseById(int id)
        {
            var result = await _listingCaseService.GetListingCaseById(id, User);

            if (result.Status == ListingCaseDetailStatus.BadRequest)
            {
                return BadRequest(
                       new ErrorResponse(StatusCodes.Status400BadRequest,
                           result.ErrorMessage ?? "Listing case id must be a positive integer.",
                           "InvalidId"));
            }
            else if (result.Status == ListingCaseDetailStatus.Forbidden)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                       new ErrorResponse(StatusCodes.Status403Forbidden,
                           result.ErrorMessage ?? "You are not allowed to access this listing case.",
                           "Forbidden"));
            }
            else
            {
                return Ok(result);
            }
        }

        /// <summary>
        /// Updates the status of a listing case with the specified identifier.
        /// </summary>
        /// <remarks>This operation requires authentication. The user must be authorized to modify the
        /// specified listing case.</remarks>
        /// <param name="id">The unique identifier of the listing case to update.</param>
        /// <param name="request">An object containing the new status. Cannot be null.</param>
        /// <returns>
        /// Updated status on success
        /// </returns>
        [HttpPatch("listings/{id}/status")]
        [Authorize]
        [ProducesResponseType(typeof(ChangeListingCaseStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ChangeListingCaseStatus(int id, [FromBody] ChangeListingCaseStatusRequest request)
        {
            var result = await _listingCaseService.ChangeListingCaseStatus(id, request, User);

            if (result.Result == ChangeListingCaseStatusResult.InvalidId)
            {
                return BadRequest(
                       new ErrorResponse(StatusCodes.Status400BadRequest,
                           result.ErrorMessage ?? "Unable to find the resource. Please provide a valid listing case id.",
                           "InvalidId"));
            }
            else if (result.Result == ChangeListingCaseStatusResult.Forbidden)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                       new ErrorResponse(StatusCodes.Status403Forbidden,
                           result.ErrorMessage ?? "You are not allowed to access this listing case.",
                           "Forbidden"));
            }
            else
            {
                return Ok(result);
            }
        }

        /// <summary>
        /// Updates the details of an existing listing case with the specified identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the listing case to update.</param>
        /// <param name="request">A full listing case object containing the updated information for the listing case.</param>
        /// <returns>Returns a 204 No Content response if the update is successful
        /// </returns>
        [HttpPut("listings/{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateListingCase(int id, [FromBody] UpdateListingCaseRequest request)
        {
            var result = await _listingCaseService.UpdateListingCase(id, request, User);

            if (result.Result == UpdateListingCaseResult.BadRequest)
            {
                return BadRequest(
                       new ErrorResponse(StatusCodes.Status400BadRequest,
                           result.ErrorMessage ?? "Unable to find the resource. Please provide a valid listing case id.",
                           "InvalidId"));
            }
            else if (result.Result == UpdateListingCaseResult.Forbidden)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                       new ErrorResponse(StatusCodes.Status403Forbidden,
                           result.ErrorMessage ?? "You are not allowed to access this listing case.",
                           "Forbidden"));
            }
            else
            {
                return NoContent();
            }
        }

        /// <summary>
        /// Deletes the listing case with the specified identifier.
        /// </summary>
        /// <remarks>Requires authorization with the 'PhotographyCompanyPolicy'.</remarks>
        /// <param name="id">The unique identifier of the listing case to delete. Must correspond to an existing listing case.</param>
        /// <returns>A result indicating the outcome of the delete operation. Returns <see cref="NoContentResult"/> if the
        /// deletion is successful;
        /// </returns>
        [HttpDelete("listings/{id}")]
        [Authorize(Policy = "PhotographyCompanyPolicy")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteListingCase(int id)
        {
            var result = await _listingCaseService.DeleteListingCase(id, User);

            if (result.Result == DeleteListingCaseResult.InvalidId)
            {
                return BadRequest(
                       new ErrorResponse(StatusCodes.Status400BadRequest,
                           result.ErrorMessage ?? "Unable to find the resource. Please provide a valid listing case id.",
                           "InvalidId"));
            }
            else if (result.Result == DeleteListingCaseResult.Forbidden)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                       new ErrorResponse(StatusCodes.Status403Forbidden,
                           result.ErrorMessage ?? "You are not allowed to access this listing case.",
                           "Forbidden"));
            }
            else
            {
                return NoContent();
            }
        }


    }
}
