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
        /// <returns>Returns user Id if successfully created</returns>
        [HttpPost("signup")]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
        {
            var userId = await _authService.SignUp(request);

            //TODO: change to CreatedAtAction when Get user API is implemented
            return Created(string.Empty, userId);
        }
        

    }
}
