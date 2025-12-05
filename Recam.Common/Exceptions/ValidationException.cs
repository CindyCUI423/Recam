using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Common.Exceptions
{
    public class ValidationException: Exception
    {
        public IReadOnlyList<string> Errors { get; }
        public ValidationException(string message)
            : base(message)
        {
            Errors = Array.Empty<string>();
        }
        public ValidationException(IEnumerable<string> errors)
            : base(errors != null ? string.Join(";", errors) : "Validation error")
        {
            Errors = errors?.ToList() ?? new List<string>();
        }

    }
}
