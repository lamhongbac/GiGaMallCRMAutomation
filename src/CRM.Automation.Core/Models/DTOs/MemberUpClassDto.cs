namespace CRM.Automation.Core.Models.DTOs;

/// <summary>
/// DTO ánh xạ trực tiếp từ Stored Procedure [GetMembersToUpClass]
/// </summary>
public class MemberUpClassCandidateDto
{
    public string MemID { get; set; } = string.Empty;
    public string ClassNo { get; set; } = string.Empty;
    public decimal? CloseAmt { get; set; }
    public string? FullName { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? CardNo { get; set; }
    public string? NameOnCard { get; set; }
}

/// <summary>
/// Thông tin hạng thẻ từ bảng [dbo].[G_MemberClass]
/// </summary>
public class MemberClassInfoDto
{
    public string ClassNo { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string? NextClassNo { get; set; }
    public string? PreClassNo { get; set; }
    public decimal? RankingPoint { get; set; }
    public decimal? RewardPoint { get; set; }
}

/// <summary>
/// Kết quả sau khi nâng hạng thành viên
/// </summary>
public class UpClassResultDto
{
    public string MemID { get; set; } = string.Empty;
    public string CardNo { get; set; } = string.Empty;
    public string OldClassNo { get; set; } = string.Empty;
    public string NewClassNo { get; set; } = string.Empty;
    public string NewClassName { get; set; } = string.Empty;
    public decimal CloseAmt { get; set; }
    public decimal BonusRewardPoints { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// DTO Báo cáo lịch sử nâng hạng thẻ hội viên
/// </summary>
public class UpClassHistoryReportDto
{
    public long OrderNo { get; set; }
    public string MemID { get; set; } = string.Empty;
    public string? CardNo { get; set; }
    public string? FullName { get; set; }
    public string? Mobile { get; set; }
    public string ClassNo { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public decimal? OpenAmt { get; set; }
    public decimal? CloseAmt { get; set; }
    public DateTime EventDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}