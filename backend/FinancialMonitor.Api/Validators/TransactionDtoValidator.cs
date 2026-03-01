using FinancialMonitor.Api.Models;
using FluentValidation;

namespace FinancialMonitor.Api.Validators;

public class TransactionDtoValidator : AbstractValidator<TransactionDto>
{
    private static readonly HashSet<string> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "USD", "EUR", "ILS", "GBP", "JPY", "CHF", "CAD", "AUD"
    };

    public TransactionDtoValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required.")
            .Length(3)
            .WithMessage("Currency must be a 3-letter ISO code.")
            .Must(currency => SupportedCurrencies.Contains(currency))
            .WithMessage(x => $"Currency '{x.Currency}' is not supported.");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Status must be Pending, Completed, or Failed.");
    }
}
