using BusinessAnalytics.Application.Tasks.DTOs;
using FluentValidation;

namespace BusinessAnalytics.Application.Tasks.Validators;

public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Task title is required")
            .MaximumLength(200).WithMessage("Task title cannot exceed 200 characters");
        
        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters");
        
        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid task priority");
        
        RuleFor(x => x.AssignedToUserId)
            .Must(id => id == null || id != Guid.Empty)
            .WithMessage("Assigned user ID cannot be empty")
            .WithName("AssignedToUserId");
    }
}

public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Task title is required")
            .MaximumLength(200).WithMessage("Task title cannot exceed 200 characters");
        
        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters");
        
        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid task priority");
        
        RuleFor(x => x.AssignedToUserId)
            .Must(id => id == null || id != Guid.Empty)
            .WithMessage("Assigned user ID cannot be empty")
            .WithName("AssignedToUserId");
    }
}

public class UpdateTaskStatusRequestValidator : AbstractValidator<UpdateTaskStatusRequest>
{
    public UpdateTaskStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid task status");
    }
}