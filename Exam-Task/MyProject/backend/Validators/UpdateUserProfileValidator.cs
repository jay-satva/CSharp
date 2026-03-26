using FluentValidation;
using MyProject.Application.DTOs.User;

namespace MyProject.Application.Validators
{
    public class UpdateUserProfileValidator : AbstractValidator<UpdateUserProfileDto>
    {
        public UpdateUserProfileValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MinimumLength(2).WithMessage("First name must be at least 2 characters.")
                .MaximumLength(50).WithMessage("First name cannot exceed 50 characters.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MinimumLength(2).WithMessage("Last name must be at least 2 characters.")
                .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email address.");

            RuleFor(x => x.PhoneNumber)
                .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

            RuleFor(x => x.ProfilePhotoUrl)
                .Must(value => string.IsNullOrWhiteSpace(value) || Uri.IsWellFormedUriString(value, UriKind.Absolute))
                .WithMessage("Profile photo URL must be a valid absolute URL.");

            RuleFor(x => x.NewPassword)
                .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.")
                .When(x => !string.IsNullOrWhiteSpace(x.NewPassword));

            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("Current password is required to change password.")
                .When(x => !string.IsNullOrWhiteSpace(x.NewPassword));
        }
    }
}
