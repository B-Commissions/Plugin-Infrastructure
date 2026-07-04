using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BlueBeard.Core.Configs;
using Rocket.API;

namespace BlueBeard.Core.Validation;

/// <summary>
/// Attribute-driven validation for any plain object — including Rocket-native
/// <see cref="IRocketPluginConfiguration"/> classes and BlueBeard <see cref="IConfig"/>
/// classes. Pure reflection; no dependency on how the object was loaded.
///
/// <code>
/// // In a RocketMod plugin's Load():
/// var report = ConfigValidator.ValidateAndCorrect(Configuration.Instance);
/// foreach (var fix in report.Corrections)
///     Logger.LogWarning($"[Config] {fix}");
/// if (report.ChangedAnything) Configuration.Save();
/// </code>
/// </summary>
public static class ConfigValidator
{
    /// <summary>
    /// Validate without modifying. Recurses into [ValidateNested] objects and lists.
    /// </summary>
    public static ValidationResult Validate(object instance)
    {
        var result = new ValidationResult();
        if (instance != null)
            ValidateObject(instance, "", result);
        return result;
    }

    /// <summary>
    /// Validate and fix violations in place: range violations clamp to the nearest bound,
    /// everything else resets from a defaults instance built via <c>new T()</c> (plus
    /// <c>LoadDefaults()</c> when T is an IRocketPluginConfiguration or IConfig).
    /// </summary>
    public static CorrectionReport ValidateAndCorrect<T>(T instance) where T : new()
    {
        var defaults = new T();
        switch (defaults)
        {
            case IRocketPluginConfiguration rocket: rocket.LoadDefaults(); break;
            case IConfig config: config.LoadDefaults(); break;
        }
        return ValidateAndCorrect(instance, defaults);
    }

    /// <summary>
    /// Validate and fix violations in place against an explicitly supplied defaults instance.
    /// </summary>
    public static CorrectionReport ValidateAndCorrect(object instance, object defaults)
    {
        var report = new CorrectionReport();
        if (instance != null)
            CorrectObject(instance, defaults, "", report);
        return report;
    }

    // -----------------------------------------------------------------------
    // Validation walk
    // -----------------------------------------------------------------------

    private static void ValidateObject(object instance, string path, ValidationResult result)
    {
        foreach (var prop in ReadableProperties(instance))
        {
            var propPath = Append(path, prop.Name);
            var value = prop.GetValue(instance);

            foreach (var error in CheckProperty(prop, value))
            {
                error.PropertyPath = propPath;
                result.Errors.Add(error);
            }

            if (prop.GetCustomAttribute<ValidateNestedAttribute>() != null && value != null)
                ValidateNested(value, propPath, result);
        }
    }

    private static void ValidateNested(object value, string path, ValidationResult result)
    {
        if (value is string) return;
        if (value is IEnumerable enumerable)
        {
            var i = 0;
            foreach (var element in enumerable)
            {
                if (element != null) ValidateObject(element, $"{path}[{i}]", result);
                i++;
            }
        }
        else
        {
            ValidateObject(value, path, result);
        }
    }

    // -----------------------------------------------------------------------
    // Correction walk
    // -----------------------------------------------------------------------

    private static void CorrectObject(object instance, object defaults, string path, CorrectionReport report)
    {
        foreach (var prop in ReadableProperties(instance))
        {
            var propPath = Append(path, prop.Name);
            var value = prop.GetValue(instance);
            var errors = CheckProperty(prop, value).ToList();

            if (errors.Count > 0)
            {
                if (!prop.CanWrite)
                {
                    foreach (var e in errors) { e.PropertyPath = propPath; report.Uncorrectable.Add(e); }
                }
                else if (TryClamp(prop, value, out var clamped))
                {
                    prop.SetValue(instance, clamped);
                    report.Corrections.Add(new ValidationCorrection
                    {
                        PropertyPath = propPath,
                        OldValue = value,
                        NewValue = clamped,
                        Reason = "clamped to allowed range"
                    });
                }
                else if (defaults != null)
                {
                    var defaultValue = prop.GetValue(defaults);
                    prop.SetValue(instance, defaultValue);
                    report.Corrections.Add(new ValidationCorrection
                    {
                        PropertyPath = propPath,
                        OldValue = value,
                        NewValue = defaultValue,
                        Reason = errors[0].Message
                    });
                }
                else
                {
                    foreach (var e in errors) { e.PropertyPath = propPath; report.Uncorrectable.Add(e); }
                }

                continue; // don't recurse into a value that was just replaced
            }

            if (prop.GetCustomAttribute<ValidateNestedAttribute>() != null && value != null)
                CorrectNested(value, defaults != null && prop.CanRead ? prop.GetValue(defaults) : null, propPath, report);
        }
    }

    private static void CorrectNested(object value, object defaultsValue, string path, CorrectionReport report)
    {
        if (value is string) return;
        if (value is IEnumerable enumerable)
        {
            // Element defaults have no meaningful pairing (counts differ), so nested list
            // elements are corrected only where clamping applies; other violations are
            // reported as uncorrectable.
            var i = 0;
            foreach (var element in enumerable)
            {
                if (element != null) CorrectObject(element, null, $"{path}[{i}]", report);
                i++;
            }
        }
        else
        {
            CorrectObject(value, defaultsValue, path, report);
        }
    }

    // -----------------------------------------------------------------------
    // Per-property checks
    // -----------------------------------------------------------------------

    private static IEnumerable<ValidationError> CheckProperty(PropertyInfo prop, object value)
    {
        var range = prop.GetCustomAttribute<RangeAttribute>();
        if (range != null && TryToDouble(value, out var d1) && (d1 < range.Min || d1 > range.Max))
            yield return new ValidationError
            {
                Message = $"must be between {range.Min} and {range.Max}",
                AttemptedValue = value
            };

        var min = prop.GetCustomAttribute<MinValueAttribute>();
        if (min != null && TryToDouble(value, out var d2) && d2 < min.Min)
            yield return new ValidationError
            {
                Message = $"must be at least {min.Min}",
                AttemptedValue = value
            };

        var max = prop.GetCustomAttribute<MaxValueAttribute>();
        if (max != null && TryToDouble(value, out var d3) && d3 > max.Max)
            yield return new ValidationError
            {
                Message = $"must be at most {max.Max}",
                AttemptedValue = value
            };

        if (prop.GetCustomAttribute<NotEmptyAttribute>() != null && IsEmpty(value))
            yield return new ValidationError
            {
                Message = "must not be empty",
                AttemptedValue = value
            };

        var regex = prop.GetCustomAttribute<RegexMatchAttribute>();
        if (regex != null && value is string s && !Regex.IsMatch(s, regex.Pattern))
            yield return new ValidationError
            {
                Message = $"must match pattern '{regex.Pattern}'",
                AttemptedValue = value
            };

        var oneOf = prop.GetCustomAttribute<OneOfAttribute>();
        if (oneOf != null && value != null && !oneOf.Allowed.Any(a => LooseEquals(a, value)))
            yield return new ValidationError
            {
                Message = $"must be one of: {string.Join(", ", oneOf.Allowed.Select(a => a?.ToString()))}",
                AttemptedValue = value
            };
    }

    private static bool TryClamp(PropertyInfo prop, object value, out object clamped)
    {
        clamped = null;
        if (!TryToDouble(value, out var d)) return false;

        double? lower = null, upper = null;
        var range = prop.GetCustomAttribute<RangeAttribute>();
        if (range != null) { lower = range.Min; upper = range.Max; }
        var min = prop.GetCustomAttribute<MinValueAttribute>();
        if (min != null) lower = lower.HasValue ? Math.Max(lower.Value, min.Min) : min.Min;
        var max = prop.GetCustomAttribute<MaxValueAttribute>();
        if (max != null) upper = upper.HasValue ? Math.Min(upper.Value, max.Max) : max.Max;

        if (lower == null && upper == null) return false;
        if ((lower == null || d >= lower) && (upper == null || d <= upper)) return false;

        var target = d < lower ? lower.Value : upper.Value;
        var underlying = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
        try
        {
            clamped = Convert.ChangeType(target, underlying, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static IEnumerable<PropertyInfo> ReadableProperties(object instance) =>
        instance.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

    private static string Append(string path, string name) =>
        path.Length == 0 ? name : $"{path}.{name}";

    private static bool TryToDouble(object value, out double result)
    {
        result = 0;
        if (value == null || value is bool || value is string) return false;
        try
        {
            result = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsEmpty(object value) => value switch
    {
        null => true,
        string s => string.IsNullOrWhiteSpace(s),
        ICollection c => c.Count == 0,
        IEnumerable e => !e.Cast<object>().Any(),
        _ => false
    };

    private static bool LooseEquals(object allowed, object value)
    {
        if (Equals(allowed, value)) return true;
        return string.Equals(allowed?.ToString(), value.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
