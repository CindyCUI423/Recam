using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.DTOs
{
    public class UpdatePasswordResponse
    {
        public UpdatePasswordResult Result { get; set; }
        public string? ErrorMessage { get; set; }

        public enum UpdatePasswordResult
        {
            Success,
            UserNotFound,
            InvalidCurrentPassword,
            InvalidNewPassword,
            Error
        }
    }
}
