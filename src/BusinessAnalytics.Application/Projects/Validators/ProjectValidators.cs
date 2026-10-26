using BusinessAnalytics.Application.Projects.DTOs;
using FluentValidation;

namespace BusinessAnalytics.Application.Projects.Validators;

public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("Department ID is required");
        
        RuleFor(x => x.OwnerUserId)
            .NotEmpty().WithMessage("Owner user ID is required");
        
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required")
            .MaximumLength(200).WithMessage("Project name cannot exceed 200 characters");
        
        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters");
        
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid project status");
        
        RuleFor(x => x)
            .Must(x => !x.StartAt.HasValue || !x.DueAt.HasValue || x.DueAt >= x.StartAt)
            .WithMessage("Due date cannot be before start date")
            .WithName("DateRange");
    }
}

public class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required")
            .MaximumLength(200).WithMessage("Project name cannot exceed 200 characters");
        
        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters");
        
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid project status");
        
        RuleFor(x => x)
            .Must(x => !x.StartAt.HasValue || !x.DueAt.HasValue || x.DueAt >= x.StartAt)
            .WithMessage("Due date cannot be before start date")
            .WithName("DateRange");
    }
}