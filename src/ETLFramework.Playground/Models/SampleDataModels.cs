namespace ETLFramework.Playground.Models;

/// <summary>
/// Sample customer data model.
/// </summary>
public class CustomerData
{
    public int CustomerId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public bool IsActive { get; set; }
    public string CustomerType { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
}

/// <summary>
/// Sample product data model.
/// </summary>
public class ProductData
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Subcategory { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public int StockQuantity { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public double Weight { get; set; }
    public string Dimensions { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime? DiscontinuedDate { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Sample order data model.
/// </summary>
public class OrderData
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ShippedDate { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingState { get; set; } = string.Empty;
    public string ShippingZipCode { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Sample employee data model.
/// </summary>
public class EmployeeData
{
    public int EmployeeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public string Department { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public int? ManagerId { get; set; }
    public string OfficeLocation { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? TerminationDate { get; set; }
    public string EmployeeType { get; set; } = string.Empty;
}

/// <summary>
/// Sample data with intentional quality issues for validation testing.
/// </summary>
public class ProblematicData
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Age { get; set; }
    public string? Salary { get; set; }
    public string? Date { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Configuration for data generation.
/// </summary>
public class DataGenerationOptions
{
    public int RecordCount { get; set; } = 100;
    public bool IncludeNulls { get; set; } = false;
    public bool IncludeInvalidData { get; set; } = false;
    public double InvalidDataPercentage { get; set; } = 0.1;
    public string[] Categories { get; set; } = Array.Empty<string>();
    public DateTimeOffset StartDate { get; set; } = DateTimeOffset.Now.AddYears(-5);
    public DateTimeOffset EndDate { get; set; } = DateTimeOffset.Now;
}

/// <summary>
/// Playground menu option.
/// </summary>
public class PlaygroundOption
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Func<Task> Action { get; set; } = () => Task.CompletedTask;
}

/// <summary>
/// Playground configuration settings.
/// </summary>
public class PlaygroundSettings
{
    public string DefaultDataSize { get; set; } = "Medium";
    public bool EnableColorOutput { get; set; } = true;
    public bool ShowProgressBars { get; set; } = true;
    public bool AutoCleanup { get; set; } = true;
    public string ExportDirectory { get; set; } = "./exports";
    public string SampleDataDirectory { get; set; } = "./Data";
    public string TempDirectory { get; set; } = "./temp";
}
