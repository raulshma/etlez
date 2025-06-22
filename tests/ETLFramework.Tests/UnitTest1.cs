using ETLFramework.Core.Models;
using FluentAssertions;

namespace ETLFramework.Tests;

public class CoreModelTests
{
    [Fact]
    public void DataRecord_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var record = new DataRecord();

        // Assert
        record.Should().NotBeNull();
        record.Fields.Should().NotBeNull();
        record.Fields.Should().BeEmpty();
    }

    [Fact]
    public void DataRecord_WithFields_ShouldStoreData()
    {
        // Arrange
        var record = new DataRecord();

        // Act
        record.Fields["Name"] = "John Doe";
        record.Fields["Age"] = 30;
        record.Fields["Email"] = "john@example.com";

        // Assert
        record.Fields.Should().HaveCount(3);
        record.Fields["Name"].Should().Be("John Doe");
        record.Fields["Age"].Should().Be(30);
        record.Fields["Email"].Should().Be("john@example.com");
    }

    [Fact]
    public void PipelineExecutionResult_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var result = new PipelineExecutionResult();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.RecordsProcessed.Should().Be(0);
        result.Errors.Should().NotBeNull();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void PipelineExecutionResult_WithSuccess_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var endTime = DateTimeOffset.UtcNow;
        var result = new PipelineExecutionResult
        {
            IsSuccess = true,
            RecordsProcessed = 100,
            StartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
            EndTime = endTime
        };

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.RecordsProcessed.Should().Be(100);
        result.StartTime.Should().BeBefore(endTime);
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        result.Errors.Should().NotBeNull();
        result.Warnings.Should().NotBeNull();
        result.Statistics.Should().NotBeNull();
    }
}
