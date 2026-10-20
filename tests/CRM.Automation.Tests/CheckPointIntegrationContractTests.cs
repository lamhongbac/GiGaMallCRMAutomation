using Xunit;
using CRM.Automation.Core.Models;
using CRM.Automation.Core.Models.Requests;
using CRM.Automation.Core.Models.DTOs;

namespace CRM.Automation.Tests;

public class CheckPointIntegrationContractTests
{
    [Fact]
    public void BOProcessResult_Success_ShouldMatchCheckPointFormat()
    {
        // Arrange & Act
        var result = BOProcessResult.Success(new { ProcessedCount = 100 }, "Task completed successfully");

        // Assert
        Assert.True(result.Ok);
        Assert.Equal("Task completed successfully", result.Message);
        Assert.NotNull(result.Data);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void BOProcessResult_Fail_ShouldMatchCheckPointFormat()
    {
        // Arrange & Act
        var result = BOProcessResult.Fail("Database query timed out", "DB_TIMEOUT");

        // Assert
        Assert.False(result.Ok);
        Assert.Equal("Database query timed out", result.Message);
        Assert.Equal("DB_TIMEOUT", result.ErrorCode);
    }

    [Fact]
    public void BurnPointReminderRequest_ShouldHaveStandardDefaults()
    {
        // Act
        var request = new BurnPointReminderRequest();

        // Assert
        Assert.Equal(2, request.ResetPointYears);
        Assert.Equal(30, request.RemindDay);
        Assert.Equal(500, request.BatchSize);
    }

    [Fact]
    public void MemberBurnPointDto_Calculations_ShouldBeAccurate()
    {
        // Arrange
        var dto = new MemberBurnPointDto
        {
            MemID = "MEM-12345",
            RemainingPoints = 150.5m,
            ExpiredDate = DateTime.UtcNow.AddDays(30)
        };

        // Assert
        Assert.Equal("MEM-12345", dto.MemID);
        Assert.Equal(150.5m, dto.RemainingPoints);
        Assert.True(dto.RemainingPoints > 0);
    }
}
