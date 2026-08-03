using System.Text.RegularExpressions;

namespace ExItS.Platform.Admin.Models;

/// <summary>
/// Shared MVP person fields for Platform Staff and Organization Staff create/edit/invite forms.
/// </summary>
public sealed class StaffPersonFieldsModel
{
    private static readonly Regex EmailPattern = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string EmployeeCode { get; set; } = "";
    public bool RequireEmailVerification { get; set; }

    public string ResolvedDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(DisplayName))
        {
            return DisplayName.Trim();
        }

        return string.Join(
            ' ',
            new[] { FirstName.Trim(), LastName.Trim() }.Where(static p => !string.IsNullOrWhiteSpace(p)));
    }

    public void ApplyResolvedDisplayName()
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            DisplayName = ResolvedDisplayName();
        }
    }

    public StaffPersonValidationResult Validate(bool requireEmail = true)
    {
        var errors = new StaffPersonFieldErrors();
        if (string.IsNullOrWhiteSpace(FirstName))
        {
            errors.FirstName = "First name is required.";
        }

        if (string.IsNullOrWhiteSpace(LastName))
        {
            errors.LastName = "Last name is required.";
        }

        ApplyResolvedDisplayName();
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            errors.DisplayName = "Display name is required.";
        }

        if (requireEmail)
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                errors.Email = "Email is required.";
            }
            else if (!EmailPattern.IsMatch(Email.Trim()))
            {
                errors.Email = "Enter a valid email address.";
            }
        }

        if (!string.IsNullOrWhiteSpace(Phone) && Phone.Trim().Length > 32)
        {
            errors.Phone = "Phone must be 32 characters or fewer.";
        }

        if (!string.IsNullOrWhiteSpace(EmployeeCode) && EmployeeCode.Trim().Length > 64)
        {
            errors.EmployeeCode = "Employee code must be 64 characters or fewer.";
        }

        return new StaffPersonValidationResult(errors);
    }

    public void Clear()
    {
        FirstName = "";
        LastName = "";
        DisplayName = "";
        Email = "";
        Phone = "";
        EmployeeCode = "";
        RequireEmailVerification = false;
    }
}

public sealed class StaffPersonFieldErrors
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? EmployeeCode { get; set; }

    public bool HasErrors =>
        FirstName is not null
        || LastName is not null
        || DisplayName is not null
        || Email is not null
        || Phone is not null
        || EmployeeCode is not null;
}

public sealed class StaffPersonValidationResult(StaffPersonFieldErrors errors)
{
    public StaffPersonFieldErrors Errors { get; } = errors;
    public bool IsValid => !Errors.HasErrors;
}
