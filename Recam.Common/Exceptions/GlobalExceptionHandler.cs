using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Recam.Common.Exceptions
{
    public class GlobalExceptionHandler
    {
        public async Task HandleException(HttpContext httpContext)
        {
            var exceptionHandlerFeature = httpContext.Features.Get<IExceptionHandlerFeature>();

            if (exceptionHandlerFeature == null) return;

            var exception = exceptionHandlerFeature.Error;

            var statusCode = GetStatusCode(exception);

            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = statusCode;

            var errorResponse = new ErrorResponse(statusCode, exception.Message, exception.GetType().Name);

            var json = JsonSerializer.Serialize(errorResponse);

            await httpContext.Response.WriteAsync(json);
            
        }

        private int GetStatusCode(Exception exception)
        {
            switch (exception)
            {
                case ArgumentException:
                    return StatusCodes.Status400BadRequest;

                case UnauthorizedAccessException:
                    return StatusCodes.Status401Unauthorized;

                case KeyNotFoundException:
                    return StatusCodes.Status404NotFound;

                case ValidationException:
                    return StatusCodes.Status400BadRequest;

                case ConflictException:
                    return StatusCodes.Status409Conflict;

                default:
                    return StatusCodes.Status500InternalServerError;

            }
        }
    }
}
