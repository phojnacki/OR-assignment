using FluentValidation;
using OR.InventoryService.Application.Commands;

namespace OR.InventoryService.Application.Validators;

public class AddInventoryCommandValidator : AbstractValidator<AddInventoryCommand>
{
    public AddInventoryCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0.");
        
        RuleFor(x => x.AddedBy)
            .NotEmpty().WithMessage("AddedBy is required.");
    }
}
