using System.Collections.Generic;
using System.Linq;

namespace BlueBeard.Core.Validation;

public class ValidationError
{
    /// <summary>Dotted path to the offending property, e.g. "Rewards[2].Chance".</summary>
    public string PropertyPath { get; set; }

    public string Message { get; set; }

    public object AttemptedValue { get; set; }

    public override string ToString() => $"{PropertyPath}: {Message} (was: {AttemptedValue ?? "null"})";
}

public class ValidationResult
{
    public List<ValidationError> Errors { get; } = [];

    public bool IsValid => Errors.Count == 0;

    public override string ToString() =>
        IsValid ? "Valid" : string.Join("; ", Errors.Select(e => e.ToString()));
}

/// <summary>A fix applied by <see cref="ConfigValidator.ValidateAndCorrect{T}(T)"/>.</summary>
public class ValidationCorrection
{
    public string PropertyPath { get; set; }
    public object OldValue { get; set; }
    public object NewValue { get; set; }
    public string Reason { get; set; }

    public override string ToString() =>
        $"{PropertyPath}: {OldValue ?? "null"} -> {NewValue ?? "null"} ({Reason})";
}

/// <summary>
/// Outcome of a validate-and-correct pass: what was fixed, and what could not be
/// (e.g. a nested object missing from the defaults instance).
/// </summary>
public class CorrectionReport
{
    public List<ValidationCorrection> Corrections { get; } = [];
    public List<ValidationError> Uncorrectable { get; } = [];

    public bool ChangedAnything => Corrections.Count > 0;
}
