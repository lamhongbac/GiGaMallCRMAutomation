using Microsoft.AspNetCore.Mvc;
using CRM.Automation.Api.Filters;
using CRM.Automation.Core.Models;
using CRM.Automation.Core.Models.DTOs;
using CRM.Automation.Infrastructure.Data;

namespace CRM.Automation.Api.Controllers;

[ApiController]
[Route("api/reports")]
[TypeFilter(typeof(ApiKeyAuthFilter))]
public class ReportsController : ControllerBase
{
    private readonly BurnPointRepository _repository;
    private readonly MemberTierRepository _tierRepository;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        BurnPointRepository repository, 
        MemberTierRepository tierRepository,
        ILogger<ReportsController> logger)
    {
        _repository = repository;
        _tierRepository = tierRepository;
        _logger = logger;
    }

    /// <summary>
    /// Báo cáo chi tiết danh sách nhắc nhở & chốt hủy điểm theo khoảng thời gian
    /// (Tương đương Stored Procedure [GetBurnPointReport] và trang CMSWeb cũ)
    /// </summary>
    /// <param name="fromDate">Từ ngày (tùy chọn, vd: 2026-01-01)</param>
    /// <param name="toDate">Đến ngày (tùy chọn, vd: 2026-12-31)</param>
    /// <param name="status">Lọc theo trạng thái: Pending, Cancel, Burned (tùy chọn)</param>
    [HttpGet("burn-point")]
    [ProducesResponseType(typeof(BOProcessResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBurnPointReport(
        [FromQuery] DateTime? fromDate, 
        [FromQuery] DateTime? toDate, 
        [FromQuery] string? status,
        CancellationToken ct)
    {
        _logger.LogInformation("Truy vấn báo cáo chi tiết BurnPoint từ {FromDate} đến {ToDate}, Status={Status}", fromDate, toDate, status);
        var data = await _repository.GetBurnPointReportAsync(fromDate, toDate, ct);
        
        // Kế thừa cơ chế lọc trạng thái theo trang BurnPointReport.aspx.cs cũ
        if (!string.IsNullOrWhiteSpace(status))
        {
            data = data.Where(x => string.Equals(x.BurnStatus, status, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return Ok(BOProcessResult.Success(data, $"Tìm thấy {data.Count} bản ghi báo cáo chi tiết."));
    }

    /// <summary>
    /// Báo cáo thống kê tổng hợp hiệu quả chiến dịch (KPI): Tổng điểm đã nhắc, Tổng điểm đã cứu, Tổng điểm đã hủy, Tỷ lệ cứu điểm %
    /// </summary>
    /// <param name="fromDate">Từ ngày (tùy chọn)</param>
    /// <param name="toDate">Đến ngày (tùy chọn)</param>
    [HttpGet("burn-point/summary")]
    [ProducesResponseType(typeof(BOProcessResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBurnPointSummary(
        [FromQuery] DateTime? fromDate, 
        [FromQuery] DateTime? toDate, 
        CancellationToken ct)
    {
        _logger.LogInformation("Truy vấn báo cáo tổng hợp KPI BurnPoint từ {FromDate} đến {ToDate}", fromDate, toDate);
        var summary = await _repository.GetBurnPointSummaryAsync(fromDate, toDate, ct);
        return Ok(BOProcessResult.Success(summary, "Đã tổng hợp số liệu báo cáo thành công."));
    }

    /// <summary>
    /// BÁO CÁO TRỪ ĐIỂM CHI TIẾT: Danh sách các trường hợp thực tế đã bị trừ điểm hết hạn
    /// </summary>
    /// <param name="fromDate">Từ ngày thực hiện trừ điểm (tùy chọn, vd: 2026-01-01)</param>
    /// <param name="toDate">Đến ngày thực hiện trừ điểm (tùy chọn, vd: 2026-12-31)</param>
    /// <param name="memId">Lọc theo Mã hội viên (tùy chọn)</param>
    /// <param name="mobile">Lọc theo Số điện thoại (tùy chọn)</param>
    [HttpGet("reset-point")]
    [ProducesResponseType(typeof(BOProcessResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetResetPointReport(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? memId,
        [FromQuery] string? mobile,
        CancellationToken ct)
    {
        _logger.LogInformation("Truy vấn báo cáo chi tiết Reset Point (Trừ điểm) từ {FromDate} đến {ToDate}, MemId: {MemId}", 
            fromDate, toDate, memId);

        var data = await _repository.GetResetPointReportAsync(fromDate, toDate, memId, mobile, ct);
        return Ok(BOProcessResult.Success(data, $"Tìm thấy {data.Count} bản ghi trừ điểm thực tế."));
    }

    /// <summary>
    /// BÁO CÁO TRỪ ĐIỂM TỔNG HỢP (KPI): Tổng số hội viên đã trừ, Tổng điểm thực tế đã hủy, Điểm trung bình/cao nhất/thấp nhất
    /// </summary>
    /// <param name="fromDate">Từ ngày thực hiện trừ điểm (tùy chọn)</param>
    /// <param name="toDate">Đến ngày thực hiện trừ điểm (tùy chọn)</param>
    [HttpGet("reset-point/summary")]
    [ProducesResponseType(typeof(BOProcessResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetResetPointSummary(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken ct)
    {
        _logger.LogInformation("Truy vấn báo cáo tổng hợp Reset Point (Trừ điểm) từ {FromDate} đến {ToDate}", fromDate, toDate);

        var summary = await _repository.GetResetPointSummaryAsync(fromDate, toDate, ct);
        return Ok(BOProcessResult.Success(summary, "Đã tổng hợp số liệu báo cáo trừ điểm thành công."));
    }
    /// <summary>
    /// BÁO CÁO NÂNG HẠNG THẺ HỘI VIÊN: Chi tiết lịch sử các hội viên đã được thăng hạng
    /// </summary>
    /// <param name="fromDate">Từ ngày nâng hạng (tùy chọn, vd: 2026-01-01)</param>
    /// <param name="toDate">Đến ngày nâng hạng (tùy chọn, vd: 2026-12-31)</param>
    /// <param name="memId">Lọc theo Mã hội viên (tùy chọn)</param>
    /// <param name="mobile">Lọc theo Số điện thoại (tùy chọn)</param>
    [HttpGet("membership/up-class")]
    [ProducesResponseType(typeof(BOProcessResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUpClassHistory(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? memId,
        [FromQuery] string? mobile,
        CancellationToken ct)
    {
        _logger.LogInformation("Truy vấn báo cáo lịch sử nâng hạng thẻ từ {FromDate} đến {ToDate}, MemId: {MemId}", 
            fromDate, toDate, memId);

        var data = await _tierRepository.GetUpClassHistoryReportAsync(fromDate, toDate, memId, mobile, ct);
        return Ok(BOProcessResult.Success(data, $"Tìm thấy {data.Count} bản ghi lịch sử thăng hạng thẻ."));
    }
}