using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Common.Exceptions
{
    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public string ErrorType { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
        public ErrorResponse(int statusCode, string message, string errorType)
        {
            StatusCode = statusCode;
            Message = message;
            ErrorType = errorType;
        }
    }
}
