using FluentValidation;
using RealEstateTax.Application.DTOs.Taxpayers;

namespace RealEstateTax.Application.Validators;

public class CreateTaxpayerRequestValidator : AbstractValidator<CreateTaxpayerRequest>
{
    public CreateTaxpayerRequestValidator()
    {
        RuleFor(x => x.NationalId)
            .NotEmpty()
            .Length(14)
            .Matches(@"^\d{14}$").WithMessage("National ID must be exactly 14 digits.")
            .Must(EgyptianNidValidator.IsStructurallyValid)
            .WithMessage("National ID has an invalid structure (century digit, date, or governorate code).");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100)
            .When(x => !x.IsCorporate);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100)
            .When(x => !x.IsCorporate);

        RuleFor(x => x.CompanyName)
            .NotEmpty()
            .MaximumLength(200)
            .When(x => x.IsCorporate);

        RuleFor(x => x.Email)
            .EmailAddress()
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^(\+20|0)?1[0-2,5]\d{8}$")
            .WithMessage("Phone number must be a valid Egyptian mobile number.")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}

public class UpdateTaxpayerRequestValidator : AbstractValidator<UpdateTaxpayerRequest>
{
    public UpdateTaxpayerRequestValidator()
    {
        RuleFor(x => x.Email)
            .EmailAddress()
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^(\+20|0)?1[0-2,5]\d{8}$")
            .WithMessage("Phone number must be a valid Egyptian mobile number.")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}

/// <summary>
/// Validates the structural rules of Egyptian National ID numbers.
/// Format: B YY MM DD GGGG S C
///   B    – century digit (2 = 1900s, 3 = 2000s)
///   YYMMDD – birth date
///   GGGG – governorate code (01–35) + sequential digits
///   S    – sequence/gender digit
///   C    – check digit (TODO: verify exact algorithm with NIDAP)
/// </summary>
public static class EgyptianNidValidator
{
    // Valid Egyptian governorate codes (01–35 as of 2024)
    private static readonly HashSet<int> ValidGovernorateCodes = [
        01, 02, 03, 04, 05, 06, 07, 08, 09, 10,
        11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
        21, 22, 23, 24, 25, 26, 27, 28, 29, 30,
        31, 32, 33, 34, 35
    ];

    public static bool IsStructurallyValid(string? nid)
    {
        if (string.IsNullOrEmpty(nid) || nid.Length != 14 || !nid.All(char.IsDigit))
            return false;

        // Century digit: 2 (1900s) or 3 (2000s)
        var centuryDigit = nid[0] - '0';
        if (centuryDigit != 2 && centuryDigit != 3)
            return false;

        // Birth date: YYMMDD
        if (!int.TryParse(nid[1..3], out var yy)) return false;
        if (!int.TryParse(nid[3..5], out var mm)) return false;
        if (!int.TryParse(nid[5..7], out var dd)) return false;

        if (mm < 1 || mm > 12) return false;
        if (dd < 1 || dd > 31) return false;

        // Validate date by constructing it
        var fullYear = (centuryDigit == 2 ? 1900 : 2000) + yy;
        try { _ = new DateTime(fullYear, mm, dd); }
        catch { return false; }

        // Governorate code: first 2 of the 4 GGGG digits (positions 7–8)
        if (!int.TryParse(nid[7..9], out var govCode)) return false;
        if (!ValidGovernorateCodes.Contains(govCode)) return false;

        return true;
    }
}
