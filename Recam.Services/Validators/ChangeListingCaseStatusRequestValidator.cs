using FluentValidation;
using Recam.Models.Enums;
using Recam.Services.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.Validators
{
    public class ChangeListingCaseStatusRequestValidator: AbstractValidator<ChangeListingCaseStatusRequest>
    {
        public ChangeListingCaseStatusRequestValidator()
        {
            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Status must be one of: Created, Pending, Delivered.");

        }
    }
}
