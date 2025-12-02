using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Recam.Services.DTOs;
using Recam.Models.Entities;
using Recam.Services.Interfaces;
using AutoMapper;
using Recam.Common.Exceptions;

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
        

    }
}
