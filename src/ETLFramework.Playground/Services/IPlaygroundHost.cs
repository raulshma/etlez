using ETLFramework.Playground.Models;

namespace ETLFramework.Playground.Services;

/// <summary>
/// Interface for the main playground host that manages the interactive menu system.
/// </summary>
public interface IPlaygroundHost
{
    /// <summary>
    /// Runs the interactive playground application.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task RunAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for playground utilities and helper functions.
/// </summary>
public interface IPlaygroundUtilities
{
    /// <summary>
    /// Displays a formatted header for a playground section.
    /// </summary>
    /// <param name="title">Section title</param>
    /// <param name="description">Optional description</param>
    void DisplayHeader(string title, string? description = null);

    /// <summary>
    /// Displays results in a formatted table.
    /// </summary>
    /// <param name="data">Data to display</param>
    /// <param name="title">Table title</param>
    void DisplayResults<T>(IEnumerable<T> data, string title);

    /// <summary>
    /// Prompts user for input with validation.
    /// </summary>
    /// <param name="prompt">Prompt message</param>
    /// <param name="defaultValue">Default value if user presses enter</param>
    /// <returns>User input</returns>
    string PromptForInput(string prompt, string? defaultValue = null);

    /// <summary>
    /// Prompts user to select from a list of options.
    /// </summary>
    /// <param name="prompt">Prompt message</param>
    /// <param name="options">Available options</param>
    /// <returns>Selected option</returns>
    T PromptForSelection<T>(string prompt, IEnumerable<T> options) where T : notnull;

    /// <summary>
    /// Displays a progress bar for long-running operations.
    /// </summary>
    /// <param name="operation">Operation to execute</param>
    /// <param name="description">Operation description</param>
    /// <returns>Task representing the operation</returns>
    Task WithProgressAsync(Func<IProgress<string>, Task> operation, string description);

    /// <summary>
    /// Displays an error message with formatting.
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="exception">Optional exception</param>
    void DisplayError(string message, Exception? exception = null);

    /// <summary>
    /// Displays a success message with formatting.
    /// </summary>
    /// <param name="message">Success message</param>
    void DisplaySuccess(string message);

    /// <summary>
    /// Waits for user to press any key to continue.
    /// </summary>
    /// <param name="message">Optional message to display</param>
    void WaitForKeyPress(string? message = null);
}

/// <summary>
/// Interface for sample data generation service.
/// </summary>
public interface ISampleDataService
{
    /// <summary>
    /// Generates sample customer data.
    /// </summary>
    /// <param name="count">Number of records to generate</param>
    /// <returns>Sample customer data</returns>
    IEnumerable<CustomerData> GenerateCustomerData(int count = 100);

    /// <summary>
    /// Generates sample product data.
    /// </summary>
    /// <param name="count">Number of records to generate</param>
    /// <returns>Sample product data</returns>
    IEnumerable<ProductData> GenerateProductData(int count = 50);

    /// <summary>
    /// Generates sample order data.
    /// </summary>
    /// <param name="count">Number of records to generate</param>
    /// <returns>Sample order data</returns>
    IEnumerable<OrderData> GenerateOrderData(int count = 200);

    /// <summary>
    /// Generates sample employee data.
    /// </summary>
    /// <param name="count">Number of records to generate</param>
    /// <returns>Sample employee data</returns>
    IEnumerable<EmployeeData> GenerateEmployeeData(int count = 75);

    /// <summary>
    /// Generates data with quality issues for validation testing.
    /// </summary>
    /// <param name="count">Number of records to generate</param>
    /// <returns>Sample data with quality issues</returns>
    IEnumerable<ProblematicData> GenerateProblematicData(int count = 50);

    /// <summary>
    /// Creates a temporary CSV file with sample data.
    /// </summary>
    /// <param name="data">Data to write</param>
    /// <param name="fileName">Optional file name</param>
    /// <returns>Path to the created file</returns>
    Task<string> CreateTempCsvFileAsync<T>(IEnumerable<T> data, string? fileName = null);

    /// <summary>
    /// Creates a temporary JSON file with sample data.
    /// </summary>
    /// <param name="data">Data to write</param>
    /// <param name="fileName">Optional file name</param>
    /// <returns>Path to the created file</returns>
    Task<string> CreateTempJsonFileAsync<T>(IEnumerable<T> data, string? fileName = null);

    /// <summary>
    /// Cleans up temporary files.
    /// </summary>
    void CleanupTempFiles();
}
