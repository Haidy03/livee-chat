namespace CtiBackend.Services.HubSpot;

public sealed record NormalizedPhoneNumber(
    string Original,
    string? E164,
    string NationalSignificantNumber,
    string? NationalNumber,
    IReadOnlyList<string> SearchVariants);

public interface IPhoneNumberNormalizer
{
    /// <summary>Returns a normalized phone result, or null when input is invalid.</summary>
    NormalizedPhoneNumber? TryNormalize(string? input, string? defaultCountry = null);

    /// <summary>Mask all but the last 4 digits for safe logging.</summary>
    string Mask(string? input);
}
