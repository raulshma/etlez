using ETLFramework.Core.Interfaces;

namespace ETLFramework.Transformation.Helpers;

/// <summary>
/// Extension methods for transformation context to provide additional functionality.
/// </summary>
public static class TransformationContextExtensions
{
    /// <summary>
    /// Updates transformation statistics (extension method for compatibility).
    /// </summary>
    /// <param name="context">The transformation context</param>
    /// <param name="recordsTransformed">Number of records transformed</param>
    /// <param name="fieldsTransformed">Number of fields transformed</param>
    /// <param name="processingTime">Processing time</param>
    public static void UpdateStatistics(this ITransformationContext context,
        long recordsTransformed = 0,
        long fieldsTransformed = 0,
        TimeSpan? processingTime = null)
    {
        // If the context is our implementation, call the method directly
        if (context is Models.TransformationContext transformationContext)
        {
            transformationContext.UpdateStatistics(recordsTransformed, fieldsTransformed, processingTime);
        }
        else
        {
            // For other implementations, update what we can
            context.Statistics.RecordsTransformed += recordsTransformed;
            context.Statistics.FieldsTransformed += fieldsTransformed;
            
            if (processingTime.HasValue)
            {
                context.Statistics.TotalProcessingTime = context.Statistics.TotalProcessingTime.Add(processingTime.Value);
            }

            context.Statistics.CalculateDerivedStatistics();
        }
    }

    /// <summary>
    /// Marks a record as skipped (extension method for compatibility).
    /// </summary>
    /// <param name="context">The transformation context</param>
    public static void SkipRecord(this ITransformationContext context)
    {
        // If the context is our implementation, call the method directly
        if (context is Models.TransformationContext transformationContext)
        {
            transformationContext.SkipRecord();
        }
        else
        {
            // For other implementations, update what we can
            context.Statistics.RecordsSkipped++;
        }
    }

    /// <summary>
    /// Gets the current progress percentage (extension method for compatibility).
    /// </summary>
    /// <param name="context">The transformation context</param>
    /// <returns>Progress percentage (0-100)</returns>
    public static double GetProgressPercentage(this ITransformationContext context)
    {
        // If the context is our implementation, call the method directly
        if (context is Models.TransformationContext transformationContext)
        {
            return transformationContext.GetProgressPercentage();
        }
        else
        {
            // For other implementations, calculate manually
            if (context.TotalRecords.HasValue && context.TotalRecords.Value > 0)
            {
                return (double)context.CurrentRecordIndex / context.TotalRecords.Value * 100;
            }
            return 0;
        }
    }

    /// <summary>
    /// Gets the estimated time remaining (extension method for compatibility).
    /// </summary>
    /// <param name="context">The transformation context</param>
    /// <returns>Estimated time remaining</returns>
    public static TimeSpan? GetEstimatedTimeRemaining(this ITransformationContext context)
    {
        // If the context is our implementation, call the method directly
        if (context is Models.TransformationContext transformationContext)
        {
            return transformationContext.GetEstimatedTimeRemaining();
        }
        else
        {
            // For other implementations, calculate manually
            if (context.TotalRecords.HasValue && context.TotalRecords.Value > 0 && context.CurrentRecordIndex > 0)
            {
                var averageTimePerRecord = context.ElapsedTime.TotalMilliseconds / context.CurrentRecordIndex;
                var remainingRecords = context.TotalRecords.Value - context.CurrentRecordIndex;
                var estimatedRemainingMs = remainingRecords * averageTimePerRecord;
                return TimeSpan.FromMilliseconds(estimatedRemainingMs);
            }
            return null;
        }
    }

    /// <summary>
    /// Sets the total records count (extension method for compatibility).
    /// </summary>
    /// <param name="context">The transformation context</param>
    /// <param name="totalRecords">The total records count</param>
    public static void SetTotalRecords(this ITransformationContext context, long totalRecords)
    {
        // If the context is our implementation, set the property directly
        if (context is Models.TransformationContext transformationContext)
        {
            transformationContext.TotalRecords = totalRecords;
        }
        else
        {
            // For other implementations, store in metadata
            context.SetMetadata("TotalRecords", totalRecords);
        }
    }

    /// <summary>
    /// Gets the total records count (extension method for compatibility).
    /// </summary>
    /// <param name="context">The transformation context</param>
    /// <returns>The total records count</returns>
    public static long? GetTotalRecords(this ITransformationContext context)
    {
        // If the context is our implementation, get the property directly
        if (context is Models.TransformationContext transformationContext)
        {
            return transformationContext.TotalRecords;
        }
        else
        {
            // For other implementations, get from metadata
            return context.GetMetadata<long?>("TotalRecords");
        }
    }
}
