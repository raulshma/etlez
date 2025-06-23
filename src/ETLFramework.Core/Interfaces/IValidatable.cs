using ETLFramework.Core.Models;

namespace ETLFramework.Core.Interfaces;

/// <summary>
/// Interface for objects that can be validated.
/// </summary>
public interface IValidatable
{
    /// <summary>
    /// Validates the object and returns a validation result.
    /// </summary>
    /// <returns>A validation result indicating whether the object is valid</returns>
    ValidationResult Validate();
}
