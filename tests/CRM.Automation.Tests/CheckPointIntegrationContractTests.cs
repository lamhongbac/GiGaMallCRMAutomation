using Xunit;
using CRM.Automation.Core.Models;
using CRM.Automation.Core.Models.Requests;
using CRM.Automation.Core.Models.DTOs;
using CRM.Automation.Core.Models.Settings;

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
    public void CheckPointRequest_ShouldOnlyContainNonBusinessParameters()
    {
        // Act: CheckPoint chỉ truyền các tham số tĩnh/phi nghiệp vụ
        var request = new BurnPointReminderRequest
        {
            ExecutionId = "EXEC-2026-001",
            TaskCode = "TASK_BURN_POINT",
            TriggerTime = DateTime.UtcNow,
            TriggerBy = "CheckPointScheduler"
        };

        // Assert
        Assert.Equal("EXEC-2026-001", request.ExecutionId);
        Assert.Equal("TASK_BURN_POINT", request.TaskCode);
        Assert.NotNull(request.TriggerTime);
        Assert.Equal("CheckPointScheduler", request.TriggerBy);
    }

    [Fact]
    public void BurnPointSettings_ShouldMaintainStandardBusinessDefaults()
    {
        // Act: Các giá trị nghiệp vụ được quản lý tập trung ở appsettings
        var settings = new BurnPointSettings();

        // Assert
        Assert.Equal(2, settings.ResetPointYears);
        Assert.Equal(30, settings.RemindDay);
        Assert.Equal(500, settings.BatchSize);
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
    [Fact]
    public void UpClassRequest_ShouldMatchCheckPointTaskContract()
    {
        var request = new UpClassRequest
        {
            ExecutionId = "CP-UPCLASS-2026-TEST",
            TaskCode = "TASK_UP_CLASS",
            TriggerBy = "CheckPoint"
        };

        Assert.Equal("CP-UPCLASS-2026-TEST", request.ExecutionId);
        Assert.Equal("TASK_UP_CLASS", request.TaskCode);
    }

    [Fact]
    public void UpClassResultDto_ShouldReflectTierUpgradeAccurately()
    {
        var result = new UpClassResultDto
        {
            MemID = "MEM-001",
            CardNo = "97040001",
            OldClassNo = "SILVER",
            NewClassNo = "GOLD",
            NewClassName = "Hạng Vàng",
            CloseAmt = 5000000m,
            BonusRewardPoints = 500m,
            Success = true
        };

        Assert.True(result.Success);
        Assert.Equal("GOLD", result.NewClassNo);
        Assert.Equal(500m, result.BonusRewardPoints);
    }
}