using System.Text;
using CtiBackend.Options;
using CtiBackend.Services.HubSpot;
using Microsoft.Extensions.Options;
using PN = PhoneNumbers;

namespace CtiBackend.Integrations.HubSpot.Services;

public sealed class PhoneNumberNormalizer : IPhoneNumberNormalizer
{
    private const int MaxInputLength = 20;
    private const int MaxVariants = 4;

    private readonly HubSpotOptions _opt;
    private readonly PN.PhoneNumberUtil _util = PN.PhoneNumberUtil.GetInstance();

    public PhoneNumberNormalizer(IOptions<HubSpotOptions> opt)
    {
        _opt = opt.Value;
    }

    public NormalizedPhoneNumber? TryNormalize(string? input, string? defaultCountry = null)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        var cleaned = input.Trim();
        if (cleaned.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
            cleaned = cleaned[4..];

        // Keep only digits and a leading '+'
        var sb = new StringBuilder();
        var first = true;
        foreach (var ch in cleaned)
        {
            if (first && ch == '+') { sb.Append('+'); first = false; continue; }
            if (char.IsDigit(ch)) sb.Append(ch);
            first = false;
        }
        var stripped = sb.ToString();
        var digitCount = 0;
        foreach (var ch in stripped) if (char.IsDigit(ch)) digitCount++;
        if (digitCount == 0) return null;
        if (stripped.Length > MaxInputLength) return null;

        var region = (defaultCountry ?? _opt.DefaultCountryCode ?? "SA").ToUpperInvariant();

        PN.PhoneNumber? parsed = null;
        try
        {
            parsed = _util.Parse(stripped, region);
        }
        catch
        {
            // Fall through — we can still build last-N variants
        }

        string? e164 = null;
        string nsn;
        string? nationalNumber = null;

        if (parsed != null && _util.IsValidNumber(parsed))
        {
            e164 = _util.Format(parsed, PN.PhoneNumberFormat.E164);
            nsn = _util.GetNationalSignificantNumber(parsed);
            nationalNumber = parsed.NationalNumber.ToString();
        }
        else
        {
            // Best effort: derive an NSN by stripping leading 00 / + / 0
            var bare = new string(stripped.Where(char.IsDigit).ToArray());
            if (bare.Length >= 9)
                nsn = bare.Length > 9 ? bare[^9..] : bare;
            else
                nsn = bare;
        }

        var variants = new List<string>(MaxVariants);
        void Add(string? v)
        {
            if (string.IsNullOrWhiteSpace(v)) return;
            if (variants.Contains(v)) return;
            if (variants.Count >= MaxVariants) return;
            variants.Add(v);
        }

        Add(nsn);
        Add("0" + nsn);
        Add(e164);
        if (nsn.Length > 9) Add(nsn[^9..]);

        return new NormalizedPhoneNumber(input, e164, nsn, nationalNumber, variants);
    }

    public string Mask(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "***";
        var digits = new string(input.Where(char.IsDigit).ToArray());
        if (digits.Length <= 4) return new string('*', Math.Max(3, digits.Length));
        return new string('*', digits.Length - 4) + digits[^4..];
    }
}
