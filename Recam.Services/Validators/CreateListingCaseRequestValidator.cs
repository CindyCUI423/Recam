using FluentValidation;
using Recam.Services.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.Services.Validators
{
    public class CreateListingCaseRequestValidator: AbstractValidator<CreateListingCaseRequest>
    {
        private static readonly string[] States =
        {
            "NSW", "ACT", "VIC", "QLD", "SA", "WA", "TAS", "NT"
        };
        
        public CreateListingCaseRequestValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(255).WithMessage("Title length must be less than 255.");

            RuleFor(x => x.Description)
                .MaximumLength(255)
                .WithMessage("Description length must be less than 255.")
                .When(x => !string.IsNullOrWhiteSpace(x.Description)); // Validate only if Description is provided

            RuleFor(x => x.Street)
                .NotEmpty().WithMessage("Street is required")
                .MaximumLength(50).WithMessage("Street length must be less than 50.");

            RuleFor(x => x.City)
                .NotEmpty().WithMessage("City is required")
                .MaximumLength(50).WithMessage("City length must be less than 50.");

            RuleFor(x => x.State)
                .NotEmpty().WithMessage("State is required")
                .Must(s => States.Contains(s.ToUpperInvariant()))
                .WithMessage("Must provide valid Australian state short forms like 'NSW'.");

            RuleFor(x => x.Postcode)
                .InclusiveBetween(1000, 9999)
                .WithMessage("Postcode must be a 4-digit number.");

            RuleFor(x => x.Bedrooms)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Bedrooms cannot be negative.");

            RuleFor(x => x.Bathrooms)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Bathrooms cannot be negative.");

            RuleFor(x => x.Garages)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Garages cannot be negative.");

            RuleFor(x => x.FloorArea)
                .GreaterThan(0)
                .WithMessage("FloorArea must be greater than 0.");

            RuleFor(x => x.PropertyType)
                .IsInEnum()
                .WithMessage("PropertyType must be one of: House, Unit, Townhouse, Villa, Others.");

        }
    }
}
