using ETLFramework.Core.Models;
using ETLFramework.Core.Interfaces;

namespace ETLFramework.Transformation.Helpers;

/// <summary>
/// Helper class for working with transformation results.
/// </summary>
public static class TransformationResultHelper
{
    /// <summary>
    /// Creates a successful transformation result.
    /// </summary>
    /// <param name="outputRecord">The output record</param>
    /// <returns>A successful transformation result</returns>
    public static TransformationResult Success(DataRecord outputRecord)
    {
        return new TransformationResult
        {
            IsSuccessful = true,
            OutputRecord = outputRecord,
            Errors = new List<ExecutionError>()
        };
    }

    /// <summary>
    /// Creates a successful transformation result with multiple output records.
    /// </summary>
    /// <param name="outputRecords">The output records</param>
    /// <returns>A successful transformation result with the first record</returns>
    public static TransformationResult Success(IEnumerable<DataRecord> outputRecords)
    {
        var recordList = outputRecords.ToList();
        return new TransformationResult
        {
            IsSuccessful = true,
            OutputRecord = recordList.FirstOrDefault(),
            Errors = new List<ExecutionError>()
        };
    }

    /// <summary>
    /// Creates a failed transformation result.
    /// </summary>
    /// <param name="errorMessage">The error message</param>
    /// <param name="exception">The optional exception</param>
    /// <returns>A failed transformation result</returns>
    public static TransformationResult Failure(string errorMessage, Exception? exception = null)
    {
        var error = exception != null
            ? new TransformationError(errorMessage, exception)
            : new TransformationError(errorMessage);

        return new TransformationResult
        {
            IsSuccessful = false,
            OutputRecord = null,
            Errors = new List<ExecutionError> { error }
        };
    }

    /// <summary>
    /// Creates a skipped transformation result.
    /// </summary>
    /// <param name="inputRecord">The input record to pass through</param>
    /// <param name="reason">The skip reason</param>
    /// <returns>A skipped transformation result</returns>
    public static TransformationResult Skipped(DataRecord inputRecord, string reason)
    {
        // For skipped transformations, pass through the input record
        return new TransformationResult
        {
            IsSuccessful = true,
            OutputRecord = inputRecord,
            Errors = new List<ExecutionError>()
        };
    }

    /// <summary>
    /// Creates multiple successful transformation results.
    /// </summary>
    /// <param name="outputRecords">The output records</param>
    /// <returns>Multiple successful transformation results</returns>
    public static IEnumerable<TransformationResult> SuccessMultiple(IEnumerable<DataRecord> outputRecords)
    {
        return outputRecords.Select(record => Success(record));
    }

    /// <summary>
    /// Creates multiple failed transformation results.
    /// </summary>
    /// <param name="count">The number of failed results</param>
    /// <param name="errorMessage">The error message</param>
    /// <param name="exception">The optional exception</param>
    /// <returns>Multiple failed transformation results</returns>
    public static IEnumerable<TransformationResult> FailureMultiple(int count, string errorMessage, Exception? exception = null)
    {
        return Enumerable.Range(0, count).Select(_ => Failure(errorMessage, exception));
    }

    /// <summary>
    /// Checks if a transformation result is successful.
    /// </summary>
    /// <param name="result">The transformation result</param>
    /// <returns>True if successful</returns>
    public static bool IsSuccess(TransformationResult result)
    {
        return result.IsSuccessful && result.Errors.Count == 0;
    }

    /// <summary>
    /// Checks if a transformation result has errors.
    /// </summary>
    /// <param name="result">The transformation result</param>
    /// <returns>True if has errors</returns>
    public static bool HasErrors(TransformationResult result)
    {
        return !result.IsSuccessful || result.Errors.Count > 0;
    }

    /// <summary>
    /// Gets the error messages from a transformation result.
    /// </summary>
    /// <param name="result">The transformation result</param>
    /// <returns>The error messages</returns>
    public static IEnumerable<string> GetErrorMessages(TransformationResult result)
    {
        return result.Errors.Select(e => e.Message);
    }

    /// <summary>
    /// Gets all output records from multiple transformation results.
    /// </summary>
    /// <param name="results">The transformation results</param>
    /// <returns>All output records</returns>
    public static IEnumerable<DataRecord> GetAllOutputRecords(IEnumerable<TransformationResult> results)
    {
        return results
            .Where(r => r.IsSuccessful && r.OutputRecord != null)
            .Select(r => r.OutputRecord!);
    }

    /// <summary>
    /// Gets all errors from multiple transformation results.
    /// </summary>
    /// <param name="results">The transformation results</param>
    /// <returns>All errors</returns>
    public static IEnumerable<ExecutionError> GetAllErrors(IEnumerable<TransformationResult> results)
    {
        return results.SelectMany(r => r.Errors);
    }

    /// <summary>
    /// Combines multiple transformation results into a summary.
    /// </summary>
    /// <param name="results">The transformation results</param>
    /// <returns>A summary transformation result</returns>
    public static TransformationResult Combine(IEnumerable<TransformationResult> results)
    {
        var resultList = results.ToList();
        var allErrors = GetAllErrors(resultList).ToList();
        var outputRecords = GetAllOutputRecords(resultList).ToList();

        return new TransformationResult
        {
            IsSuccessful = allErrors.Count == 0,
            OutputRecord = outputRecords.FirstOrDefault(),
            Errors = allErrors
        };
    }

    /// <summary>
    /// Creates a transformation result from an input record with no changes.
    /// </summary>
    /// <param name="inputRecord">The input record</param>
    /// <returns>A pass-through transformation result</returns>
    public static TransformationResult PassThrough(DataRecord inputRecord)
    {
        return Success(inputRecord);
    }

    /// <summary>
    /// Creates transformation results from input records with no changes.
    /// </summary>
    /// <param name="inputRecords">The input records</param>
    /// <returns>Pass-through transformation results</returns>
    public static IEnumerable<TransformationResult> PassThroughMultiple(IEnumerable<DataRecord> inputRecords)
    {
        return inputRecords.Select(PassThrough);
    }
}

/// <summary>
/// Extension methods for TransformationResult to provide additional functionality.
/// </summary>
public static class TransformationResultExtensions
{
    /// <summary>
    /// Gets all output records from transformation results (simulates OutputRecords property).
    /// </summary>
    /// <param name="results">The transformation results</param>
    /// <returns>All output records</returns>
    public static IEnumerable<DataRecord> GetOutputRecords(this IEnumerable<TransformationResult> results)
    {
        return results
            .Where(r => r.IsSuccessful && r.OutputRecord != null)
            .Select(r => r.OutputRecord!);
    }

    /// <summary>
    /// Gets the output record as a collection (simulates OutputRecords property).
    /// </summary>
    /// <param name="result">The transformation result</param>
    /// <returns>Output records collection</returns>
    public static IEnumerable<DataRecord> GetOutputRecords(this TransformationResult result)
    {
        if (result.IsSuccessful && result.OutputRecord != null)
        {
            return new[] { result.OutputRecord };
        }
        return Enumerable.Empty<DataRecord>();
    }
}
