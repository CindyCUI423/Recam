using AutoMapper;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Recam.Common.Exceptions;
using Recam.Models.Entities;
using Recam.Services.DTOs;
using Recam.Services.Interfaces;
using static Recam.Services.DTOs.GetCurrentUserInfoResponse;

namespace Recam.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private IAuthService _authService;

        public AuthController(IAuthService authService)
        { 
            _authService = authService; 
        }

        /// <summary>
        /// New user sign up as an agent or photography company
        /// </summary>
        /// <param name="request">Necessary sign up information</param>
        /// <returns>
        /// User's Id on success
        /// </returns>
        [HttpPost("signup")]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
        {
            var result = await _authService.SignUp(request);

            switch (result.Status)
            {
                case SignUpStatus.Success:
                    return Created(string.Empty, result.UserId);

                case SignUpStatus.UserNameAlreadyExists:
                case SignUpStatus.EmailAlreadyExists:
                    return StatusCode(StatusCodes.Status409Conflict, 
                        new ErrorResponse(StatusCodes.Status409Conflict,
                            result.ErrorMessage ?? "User already exists.",
                            result.Status.ToString()));

                case SignUpStatus.CreateUserFailure:
                    return BadRequest(
                        new ErrorResponse(StatusCodes.Status400BadRequest,
                            result.ErrorMessage ?? "Failed to create the user. Please check if your password meet all the requirements.", 
                            result.Status.ToString()));

                case SignUpStatus.AssignRoleFailure:
                    return BadRequest(
                        new ErrorResponse(StatusCodes.Status400BadRequest,
                            result.ErrorMessage ?? "Invalid role type.",
                            result.Status.ToString()));

                default:
                    return BadRequest(
                        new ErrorResponse(StatusCodes.Status400BadRequest,
                            result.ErrorMessage ?? "Sign up failed for some reasons.",
                            result.Status.ToString()));
            }
        }

        /// <summary>
        /// User login
        /// </summary>
        /// <param name="request">Necessary login information</param>
        /// <returns>
        /// User's information and token on success
        /// </returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status423Locked)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await _authService.Login(request);

            switch (response.Status)
            {
                case LoginStatus.Success:
                    return Ok(response);

                case LoginStatus.UserNotFound:
                    return Unauthorized(new ErrorResponse(401, response.ErrorMessage, "UserNotFound"));

                case LoginStatus.InvalidCredentials:
                    return Unauthorized(new ErrorResponse(401, response.ErrorMessage, "InvalidaCredentials"));

                case LoginStatus.LockedOut:
                    return StatusCode(StatusCodes.Status423Locked,
                        new ErrorResponse(423, response.ErrorMessage, "LockedOut")
                    );

                case LoginStatus.NotAllowed:
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new ErrorResponse(403, response.ErrorMessage, "UnverifiedEmail"));

                default:
                    return BadRequest(new ErrorResponse(400, response.ErrorMessage, "LoginFailed"));
                       
            }

        }

        /// <summary>
        /// Get all users (support pagination)
        /// </summary>
        /// <param name="pageNumber">Number of the page</param>
        /// <param name="pageSize">Numbers of users retrieved per page</param>
        /// <returns>
        /// User's information and total count of users on success
        /// </returns>
        [HttpGet("users")]
        [Authorize(Policy = "PhotographyCompanyPolicy")]
        [ProducesResponseType(typeof(GetUsersResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _authService.GetAllUsers(pageNumber, pageSize);

            if (result.Status == GetUsersStatus.Error)
            {
                return BadRequest(
                    new ErrorResponse(StatusCodes.Status400BadRequest,
                        result.ErrorMessage ?? "pageNumber and pageSize must be greater than 0.",
                        "InvalidPagination"));
            }
            else
            {
                return Ok(result);
            }
            
        }

        /// <summary>
        /// Retrieves information about the currently authenticated user.
        /// </summary>
        /// <remarks>This endpoint requires authentication. The response includes user details including user id, user role, 
        /// and assoiciated listing case ids (for photography company) or assigned listing case ids (for agent). 
        /// If the user cannot be determined from the authentication
        /// context, the request is rejected as unauthorized.</remarks>
        /// <returns>An <see cref="IActionResult"/> containing the current user's information with status code 200 (OK) if the
        /// user is found; otherwise, a 401 (Unauthorized) response with an error message if the user cannot be
        /// identified.</returns>
        [HttpGet("users/me")]
        [Authorize]
        [ProducesResponseType(typeof(GetCurrentUserInfoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetCurrentUserInfo()
        {
            var result = await _authService.GetCurrentUserInfo(User);

            if (result.Result == GetCurrentUserInfoResult.UserNotFound)
            {
                return Unauthorized(new ErrorResponse(401, result.ErrorMessage ?? "Unable to find the user id due to user id claim missing.", "UserNotFound"));
            }
            else
            {
                return Ok(result);
            }
        }
    }
}
