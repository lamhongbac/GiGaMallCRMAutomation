using Microsoft.Extensions.Logging;
using CRMMall.BillReview.BLL.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRMMall.BillReview.BLL.DTOs;
using CRMMall.BillReview.BLL.Policies;
using CRMMall.BillReview.DAL.Entities;
using CRMMall.BillReview.DAL.Repositories;

namespace CRMMall.BillReview.BLL.Services
{
    /// <summary>
    /// Điều phối viên nghiệp vụ duyệt hóa đơn (Bill Review Orchestrator)
    /// Đóng vai trò tổng hợp và điều phối các thư viện logic độc lập:
    /// - ILoyaltyPointsService: Tính toán & ghi nhận sổ cái điểm
    /// - IMembershipTierService: Thẩm định & nâng hạng thành viên
    /// - ILotteryPromotionService: Đánh giá & phát vé quay số, quà khuyến mãi
    /// - INotificationService: Điều phối thông báo đẩy
    /// </summary>
    public class BillReviewService : IBillReviewService
    {
        private readonly IBillRepository _billRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IAutoApprovalPolicy _autoApprovalPolicy;
        private readonly ILoyaltyPointsService _loyaltyPointsService;
        private readonly IMembershipTierService _membershipTierService;
        private readonly ILotteryPromotionService _lotteryPromotionService;
        private readonly INotificationService _notificationService;
        private readonly IOcrApiClient _ocrApiClient;
        private readonly ApplicationSetting _appSetting;
        private readonly ILogger<BillReviewService> _logger;

        public BillReviewService(
            IBillRepository billRepository,
            ICustomerRepository customerRepository,
            IAutoApprovalPolicy autoApprovalPolicy,
            ILoyaltyPointsService loyaltyPointsService,
            IMembershipTierService membershipTierService,
            ILotteryPromotionService lotteryPromotionService,
            INotificationService notificationService,
            IOcrApiClient ocrApiClient,
            ApplicationSetting appSetting,
            ILogger<BillReviewService> logger)
        {
            _billRepository = billRepository;
            _customerRepository = customerRepository;
            _autoApprovalPolicy = autoApprovalPolicy;
            _loyaltyPointsService = loyaltyPointsService;
            _membershipTierService = membershipTierService;
            _lotteryPromotionService = lotteryPromotionService;
            _notificationService = notificationService;
            _ocrApiClient = ocrApiClient;
            _appSetting = appSetting;
            _logger = logger;
        }

                /// <summary>
        /// Lấy danh sách các hóa đơn bóc tách đang chờ kiểm toán viên đối soát và phê duyệt.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Cung cấp hàng đợi hóa đơn cần xử lý (Audit Worklist Provider).
        /// - MỤC ĐÍCH: Truy vấn các hóa đơn có Approved IS NULL từ mssOCRTrialTrans, kết hợp thông tin hội viên và tự động chạy IAutoApprovalPolicy để gắn nhãn gợi ý (IsAutoApprovable) cho kiểm toán viên.
        /// - CÁCH DÙNG: Được gọi từ API GET /api/bills/pending trên Web Portal hoặc Worker điều phối.
        /// </remarks>
        /// <param name="fromDate">Lọc từ ngày tải lên (tùy chọn).</param>
        /// <param name="toDate">Lọc đến ngày tải lên (tùy chọn).</param>
        /// <param name="limit">Số lượng bản ghi tối đa (mặc định 100).</param>
        /// <returns>Danh sách các hóa đơn chờ duyệt kèm nhãn đánh giá an toàn.</returns>
            public async Task<PagedResultDto<PendingUploadBillDto>> GetPendingUploadBillsPagedAsync(DateTime? fromDate = null, DateTime? toDate = null, int pageIndex = 1, int pageSize = 25)
        {
            var (rawBills, totalCount) = await _billRepository.GetPendingUploadBillsPagedAsync(fromDate, toDate, pageIndex, pageSize);
            var results = new List<PendingUploadBillDto>();

            foreach (var b in rawBills)
            {
                bool hasOcr = !string.IsNullOrWhiteSpace(b.BillNumber) || !string.IsNullOrWhiteSpace(b.BillTotal);
                results.Add(new PendingUploadBillDto
                {
                    ID = b.ID,
                    MemID = b.MemID,
                    UploadImageDateTime = b.UploadImageDateTime,
                    ImagePath = b.ImagePath,
                    ImageFileName = b.ImageFileName,
                    ImageUrl = $"/api/bills/{b.ID}/image",
                    DataStatus = b.DataStatus ?? "Pending",
                    OCRStatus = b.OCRStatus,
                    HasOcrResult = hasOcr,
                    ScannedOutletNumber = b.OutletNumber,
                    ScannedOutletName = b.OutletName,
                    ScannedBrandName = b.BrandName,
                    ScannedBillNumber = b.BillNumber,
                    ScannedBillDateTime = b.BillDateTime,
                    ScannedBillTotal = b.BillTotal
                });
            }

            return new PagedResultDto<PendingUploadBillDto>
            {
                Items = results,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<IEnumerable<PendingUploadBillDto>> GetPendingUploadBillsAsync(DateTime? fromDate = null, DateTime? toDate = null, int limit = 50)
        {
            var rawBills = await _billRepository.GetPendingUploadBillsAsync(fromDate, toDate, limit);
            var results = new List<PendingUploadBillDto>();

            foreach (var b in rawBills)
            {
                bool hasOcr = !string.IsNullOrWhiteSpace(b.BillNumber) || !string.IsNullOrWhiteSpace(b.BillTotal);
                results.Add(new PendingUploadBillDto
                {
                    ID = b.ID,
                    MemID = b.MemID,
                    UploadImageDateTime = b.UploadImageDateTime,
                    ImagePath = b.ImagePath,
                    ImageFileName = b.ImageFileName,
                    ImageUrl = $"/api/bills/{b.ID}/image",
                    DataStatus = b.DataStatus ?? "Pending",
                    OCRStatus = b.OCRStatus,
                    HasOcrResult = hasOcr,
                    ScannedBillNumber = b.BillNumber,
                    ScannedBillDateTime = b.BillDateTime,
                    ScannedBillTotal = b.BillTotal,
                    ScannedOutletNumber = b.OutletNumber,
                    ScannedOutletName = b.OutletName,
                    ScannedBrandName = b.BrandName
                });
            }

            return results;
        }

        public async Task<byte[]?> GetBillImageBytesAsync(Guid billId)
        {
            var bill = await _billRepository.GetByIdAsync(billId);
            if (bill == null) return null;

            // Quy tắc đường dẫn: BaseImagePath + dbimagePath (ImagePath) + ImageFileName
            // Đây là nơi lưu file ảnh gốc upload lần đầu từ mobile (chưa duyệt)
            // DEV : D:\GGDrive\TEST_IMAGES\ + Transaction\010926\ + 35734_010926222308.png
            // PROD: D:\Project\CRMAPI\Images\Bill\ + Transaction\010926\ + 35734_010926222308.png
            string baseImagePath = _appSetting.BaseImagePath;
            string dbimagePath = (bill.ImagePath ?? "").Trim().Replace("/", "\\").TrimStart('\\', '/');
            string imageFileName = (bill.ImageFileName ?? "").Trim();

            if (string.IsNullOrEmpty(imageFileName))
            {
                _logger.LogWarning("Bill {BillId} không có ImageFileName trong CSDL.", billId);
                return null;
            }

            // Chuẩn hóa đường dẫn tránh lặp nếu baseImagePath đã kết thúc bằng Transaction
            string relClean = dbimagePath;
            if (baseImagePath.TrimEnd('\\', '/').EndsWith("Transaction", StringComparison.OrdinalIgnoreCase) &&
                dbimagePath.StartsWith("Transaction", StringComparison.OrdinalIgnoreCase))
            {
                relClean = dbimagePath.Length > "Transaction".Length ? dbimagePath.Substring("Transaction".Length).TrimStart('\\', '/') : "";
            }

            // Đường dẫn ảnh gốc upload từ mobile: BaseImagePath + dbimagePath + ImageFileName
            string fullImagePath = Path.Combine(baseImagePath, dbimagePath, imageFileName);
            if (!File.Exists(fullImagePath) && !string.IsNullOrEmpty(relClean) && !string.Equals(relClean, dbimagePath, StringComparison.OrdinalIgnoreCase))
            {
                fullImagePath = Path.Combine(baseImagePath, relClean, imageFileName);
            }

            if (File.Exists(fullImagePath))
            {
                return await File.ReadAllBytesAsync(fullImagePath);
            }

            _logger.LogWarning("Không tìm thấy file ảnh gốc tại: {FullImagePath}", fullImagePath);
            return null;
        }

        public async Task<OcrExtractApiResponse> PerformOcrAsync(Guid billId)
        {
            var bill = await _billRepository.GetByIdAsync(billId);
            if (bill == null)
            {
                return new OcrExtractApiResponse
                {
                    Success = false,
                    Message = "Không tìm thấy hóa đơn trong hệ thống."
                };
            }

            byte[]? imageBytes = await GetBillImageBytesAsync(billId);
            if (imageBytes == null || imageBytes.Length == 0)
            {
                return new OcrExtractApiResponse
                {
                    Success = false,
                    Message = $"Không tìm thấy tệp ảnh của hóa đơn ({bill.ImageFileName}). Vui lòng kiểm tra thư mục lưu trữ."
                };
            }

            string fileName = string.IsNullOrWhiteSpace(bill.ImageFileName) ? $"{bill.MemID}_bill.png" : bill.ImageFileName;
            var ocrResponse = await _ocrApiClient.ExtractBillAsync(imageBytes, fileName, bill.MemID, bill.ID.ToString(), duplicateAction: "allow");
            return ocrResponse;
        }

        public async Task<SaveOcrResultResponse> SaveOcrResultAsync(Guid billId, SaveOcrResultRequest request)
        {
            var bill = await _billRepository.GetByIdAsync(billId);
            if (bill == null)
            {
                return new SaveOcrResultResponse
                {
                    Success = false,
                    BillId = billId,
                    Message = "Không tìm thấy hóa đơn."
                };
            }

            bool saved = await _billRepository.UpdateOcrScanResultAsync(
                billId,
                request.OutletNumber,
                request.BrandNumber,
                request.OutletName,
                request.BrandName,
                request.BillNumber,
                request.BillDateTime,
                request.BillTotal,
                request.CashierName,
                request.RawOcrResponse);

            return new SaveOcrResultResponse
            {
                Success = saved,
                BillId = billId,
                DataStatus = "OCR",
                ScanDate = DateTime.Now,
                Message = saved ? "Đã lưu kết quả OCR vào CSDL thành công!" : "Lưu CSDL thất bại."
            };
        }

        public async Task<IEnumerable<PendingBillDto>> GetBillsForReviewAsync(DateTime? fromDate = null, DateTime? toDate = null, int limit = 100)
        {
            var rawBills = await _billRepository.GetBillsForReviewAsync(fromDate, toDate, limit);
            var results = new List<PendingBillDto>();

            foreach (var b in rawBills)
            {
                var customer = await _customerRepository.GetCustomerByIdAsync(b.MemID);
                var decision = _autoApprovalPolicy.Evaluate(b, customer ?? new CustomerInfo { Status = "Unknown" });

                results.Add(new PendingBillDto
                {
                    ID = b.ID,
                    MemID = b.MemID,
                    CustomerName = customer?.FullName,
                    CustomerMobile = customer?.Mobile,
                    CustomerStatus = customer?.Status,
                    CurrentTier = "MEMBER",
                    UploadImageDateTime = b.UploadImageDateTime,
                    ImagePath = b.ImagePath,
                    ImageFileName = b.ImageFileName,
                    ScannedOutletNumber = b.OutletNumber,
                    ScannedOutletName = b.OutletName,
                    ScannedBrandNumber = b.BrandNumber,
                    ScannedBillNumber = b.BillNumber,
                    ScannedBillDateTime = b.BillDateTime,
                    ScannedBillTotal = b.BillTotal,
                    CashierName = b.CashierName,
                    IsAutoApprovable = decision.CanAutoApprove,
                    AutoApproveReason = decision.Reason
                });
            }

            return results;
        }

        public async Task<IEnumerable<PendingBillDto>> GetPendingBillsAsync(DateTime? fromDate = null, DateTime? toDate = null, int limit = 100)
        {
            var rawBills = await _billRepository.GetPendingBillsAsync(fromDate, toDate, limit);
            var results = new List<PendingBillDto>();

            foreach (var b in rawBills)
            {
                var customer = await _customerRepository.GetCustomerByIdAsync(b.MemID);
                var decision = _autoApprovalPolicy.Evaluate(b, customer ?? new CustomerInfo { Status = "Unknown" });

                results.Add(new PendingBillDto
                {
                    ID = b.ID,
                    MemID = b.MemID,
                    CustomerName = customer?.FullName,
                    CustomerMobile = customer?.Mobile,
                    CustomerStatus = customer?.Status,
                    CurrentTier = "MEMBER",
                    UploadImageDateTime = b.UploadImageDateTime,
                    ImagePath = b.ImagePath,
                    ImageFileName = b.ImageFileName,
                    ScannedOutletNumber = b.OutletNumber,
                    ScannedOutletName = b.OutletName,
                    ScannedBrandNumber = b.BrandNumber,
                    ScannedBillNumber = b.BillNumber,
                    ScannedBillDateTime = b.BillDateTime,
                    ScannedBillTotal = b.BillTotal,
                    CashierName = b.CashierName,
                    IsAutoApprovable = decision.CanAutoApprove,
                    AutoApproveReason = decision.Reason
                });
            }

            return results;
        }

                /// <summary>
        /// Lấy chi tiết toàn bộ dữ liệu bóc tách và thông tin hội viên của một hóa đơn cụ thể.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Cung cấp dữ liệu chi tiết cho màn hình đối soát hóa đơn (Bill Detail Inspector).
        /// - MỤC ĐÍCH: Đọc hóa đơn theo ID, nạp thông tin khách hàng, thẻ thành viên và lý do gợi ý tự duyệt.
        /// - CÁCH DÙNG: Được gọi từ API GET /api/bills/{id} khi kiểm toán viên chọn 1 dòng hóa đơn để soi ảnh.
        /// </remarks>
        /// <param name="id">Mã định danh duy nhất của hóa đơn (mssOCRTrialTrans.ID).</param>
        /// <returns>Đối tượng PendingBillDto chi tiết hoặc null nếu không tồn tại.</returns>
        public async Task<PendingBillDto?> GetBillByIdAsync(Guid id)
        {
            var b = await _billRepository.GetByIdAsync(id);
            if (b == null) return null;

            var customer = await _customerRepository.GetCustomerByIdAsync(b.MemID);
            var decision = _autoApprovalPolicy.Evaluate(b, customer ?? new CustomerInfo { Status = "Unknown" });

            return new PendingBillDto
            {
                ID = b.ID,
                MemID = b.MemID,
                CustomerName = customer?.FullName,
                CustomerMobile = customer?.Mobile,
                CustomerStatus = customer?.Status,
                CurrentTier = "MEMBER",
                UploadImageDateTime = b.UploadImageDateTime,
                ImagePath = b.ImagePath,
                ImageFileName = b.ImageFileName,
                ScannedOutletNumber = b.OutletNumber,
                ScannedOutletName = b.OutletName,
                ScannedBrandNumber = b.BrandNumber,
                ScannedBillNumber = b.BillNumber,
                ScannedBillDateTime = b.BillDateTime,
                ScannedBillTotal = b.BillTotal,
                CashierName = b.CashierName,
                IsAutoApprovable = decision.CanAutoApprove,
                AutoApproveReason = decision.Reason
            };
        }

                /// <summary>
        /// Thực thi trọn gói quy trình phê duyệt hóa đơn qua chuỗi 6 bước chuẩn hóa.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Quy trình nghiệp vụ cốt lõi (Core Approval Pipeline Orchestrator).
        /// - MỤC ĐÍCH:
        ///   1. Kiểm tra tính hợp lệ &amp; chống tích điểm trùng lặp gian lận.
        ///   2. Gọi ILoyaltyPointsService tính điểm và ghi đồng bộ 3 sổ cái (CardTrans, CLMLedger, MemClassLedger).
        ///   3. Gọi IMembershipTierService thẩm định và tự động nâng hạng thẻ.
        ///   4. Gọi ILotteryPromotionService thẩm định hạn ngạch và phát vé quay số / voucher khuyến mãi.
        ///   5. Gọi INotificationService điều phối gửi thông báo đẩy đa kênh tức thì.
        ///   6. Cập nhật trạng thái hóa đơn mssOCRTrialTrans sang 'Approved'.
        /// - CÁCH DÙNG: Được gọi khi kiểm toán viên bấm nút [Phê duyệt] trên Web Portal hoặc khi Worker AI tự động duyệt hóa đơn hợp lệ.
        /// </remarks>
        /// <param name="request">Yêu cầu phê duyệt chứa thông tin đã đối soát (số tiền, số bill, gian hàng, ngày giờ).</param>
        /// <returns>Đối tượng BillReviewResult chứa kết quả chi tiết: điểm thưởng, điểm xếp hạng, thăng hạng, vé số, quà tặng.</returns>
        public async Task<BillReviewResult> ApproveBillAsync(ApproveBillRequest request)
        {
            #region KIỂM TRA HỢP LỆ (VALIDATION & AUDIT)
            // -----------------------------------------------------------------------------------------
            var bill = await _billRepository.GetByIdAsync(request.BillId);
            if (bill == null)
            {
                return new BillReviewResult 
                { 
                    Success = false, 
                    Message = "[LỖI] Không tìm thấy bản ghi hóa đơn cần phê duyệt trong hệ thống." 
                };
            }

            if (bill.Approved == true)
            {
                return new BillReviewResult 
                { 
                    Success = false, 
                    Message = "[CẢNH BÁO] Hóa đơn này đã được phê duyệt trước đó." 
                };
            }

            if (string.IsNullOrWhiteSpace(request.ApprovedOutletNumber))
            {
                return new BillReviewResult
                {
                    Success = false,
                    Message = "Mã gian hàng (Outlet Number) không được để trống."
                };
            }

            if (string.IsNullOrWhiteSpace(request.ApprovedBillNumber))
            {
                return new BillReviewResult
                {
                    Success = false,
                    Message = "Số hóa đơn (Bill Number) không được để trống."
                };
            }

            if (request.ApprovedTotalAmount <= 0)
            {
                return new BillReviewResult 
                { 
                    Success = false, 
                    Message = "Số tiền phê duyệt phải lớn hơn 0 VNĐ." 
                };
            }

            var isDuplicate = await _billRepository.IsBillNumberExistsAsync(request.ApprovedOutletNumber, request.ApprovedBillNumber, bill.ID);
            if (isDuplicate)
            {
                return new BillReviewResult 
                { 
                    Success = false, 
                    Message = $"[GIAN LẬN] Số hóa đơn '{request.ApprovedBillNumber}' đã được phê duyệt cho gian hàng '{request.ApprovedOutletNumber}' trước đó!" 
                };
            }
            #endregion

            #region LƯU THÔNG TIN ADMIN & CẬP NHẬT TRẠNG THÁI (APPROVED = 1, REVIEWED = 1)
            // -----------------------------------------------------------------------------------------
            // NGUYÊN TẮC BẤT BIẾN: CHỈ lưu đúng những gì Admin gõ vào các trường _A!
            // Tuyệt đối KHÔNG tự ý lấy số OCR đè vào các trường _A.
            // Nếu Admin không gõ trường nào, trường _A đó trong CSDL PHẢI LÀ NULL!
            string? saveBillNumberA = !string.IsNullOrWhiteSpace(request.BillNumberA) ? request.BillNumberA.Trim() : null;
            decimal? saveBillTotalA = request.BillTotalA;
            DateTime? saveBillDateTimeA = request.BillDateTimeA;
            string? saveOutletNumberA = !string.IsNullOrWhiteSpace(request.OutletNumberA) ? request.OutletNumberA.Trim() : null;
            string? saveBrandNumberA = !string.IsNullOrWhiteSpace(request.BrandNumberA) ? request.BrandNumberA.Trim() : null;

            bool updated = await _billRepository.UpdateApprovalStatusAsync(
                bill.ID,
                approved: true,
                reviewBy: request.ReviewBy ?? "OperatorWeb",
                reviewNotes: request.ReviewNotes,
                approvedAmount: saveBillTotalA,
                approvedBillNumber: saveBillNumberA,
                approvedBillDateTime: saveBillDateTimeA,
                approvedOutletNumber: saveOutletNumberA,
                approvedBrandNumber: saveBrandNumberA,
                pointGain: 0);

            if (!updated)
            {
                return new BillReviewResult
                {
                    Success = false,
                    Message = "Cập nhật dữ liệu vào CSDL thất bại."
                };
            }

            return new BillReviewResult
            {
                Success = true,
                Message = $"Đã phê duyệt hóa đơn {request.ApprovedBillNumber} thành công! Đã lưu số liệu Admin và cập nhật trạng thái (Chờ chạy CRM Process).",
                PointsAwarded = 0,
                RankPointsAwarded = 0
            };
            #endregion
        }

        /// <summary>
        /// [DÀNH CHO TASK CRM-PROC / BƯỚC 4] Xử lý nghiệp vụ Loyalty (Tích điểm 3 sổ cái, thăng hạng, vé số, quà tặng, push noti)
        /// Chỉ thực thi khi bấm nút [CRMProcess] hoặc qua Worker tự động cho các bill đã duyệt (Approved = 1 & CRMProcess = 0).
        /// </summary>
        public async Task<BillReviewResult> ProcessCrmLoyaltyAsync(Guid billId, string executedBy)
        {
            var bill = await _billRepository.GetByIdAsync(billId);
            if (bill == null)
            {
                return new BillReviewResult { Success = false, Message = "Không tìm thấy hóa đơn cần chạy CRM Process." };
            }

            if (bill.Approved != true)
            {
                return new BillReviewResult { Success = false, Message = "Hóa đơn chưa được phê duyệt, không thể chạy CRM Process." };
            }

            var customer = await _customerRepository.GetCustomerByIdAsync(bill.MemID);
            if (customer == null)
            {
                return new BillReviewResult { Success = false, Message = $"Không tìm thấy hồ sơ hội viên MemID: {bill.MemID}." };
            }

            string billNo = bill.BillNumberA ?? bill.BillNumber ?? string.Empty;
            string outletNo = bill.OutletNumberA ?? bill.OutletNumber ?? string.Empty;
            decimal billTotal = bill.BillTotalA ?? (decimal.TryParse(bill.BillTotal, out var bt) ? bt : 0);
            DateTime billDate = bill.BillDateTimeA ?? (DateTime.TryParse(bill.BillDateTime, out var bd) ? bd : DateTime.Now);

            // 1. Tích điểm 3 sổ cái
            var pointsResult = await _loyaltyPointsService.ProcessSalePointsAsync(
                bill.MemID,
                customer.CardNo,
                bill.ID,
                billNo,
                outletNo,
                billDate,
                billTotal,
                bill.CashierName,
                executedBy,
                bill.ReviewNotes);

            if (!pointsResult.Success)
            {
                return new BillReviewResult { Success = false, Message = $"Lỗi tích điểm: {pointsResult.Message}" };
            }

            // 2. Thăng hạng
            var tierResult = await _membershipTierService.EvaluateAndUpgradeTierAsync(bill.MemID, billNo);

            // 3. Vé số & Quà
            var lotteryAwarded = await _lotteryPromotionService.EvaluateAndIssueLotteryAsync(
                bill.MemID, customer.DateJoined, billNo, billDate, billTotal, outletNo);

            var promotionAwarded = await _lotteryPromotionService.EvaluateAndIssuePromotionAsync(
                bill.MemID, billNo, billDate, billTotal, outletNo);

            // 4. Thông báo đẩy Mobile
            await _notificationService.SendSalePointsNotificationAsync(
                bill.MemID, billNo, outletNo, pointsResult.PointsAwarded, pointsResult.RewardRate);

            if (tierResult.IsUpgraded && !string.IsNullOrWhiteSpace(tierResult.NewTier))
            {
                await _notificationService.SendTierUpgradedNotificationAsync(bill.MemID, tierResult.NewTier);
            }

            if (lotteryAwarded != null)
            {
                await _notificationService.SendLotteryTicketNotificationAsync(bill.MemID, lotteryAwarded);
            }

            if (promotionAwarded != null)
            {
                await _notificationService.SendPromotionGiftNotificationAsync(bill.MemID, promotionAwarded);
            }

            return new BillReviewResult
            {
                Success = true,
                Message = $"Chạy CRM Process thành công! Tích lũy +{pointsResult.PointsAwarded:N0} điểm cho hội viên {bill.MemID}.",
                PointsAwarded = pointsResult.PointsAwarded,
                RankPointsAwarded = pointsResult.RankPointsAwarded,
                RewardRate = pointsResult.RewardRate,
                ReviewRate = pointsResult.ReviewRate,
                IsUpgraded = tierResult.IsUpgraded,
                NewTier = tierResult.NewTier,
                LotteryCode = lotteryAwarded?.LotteryCode,
                LotteryAwarded = lotteryAwarded,
                PromotionCode = promotionAwarded?.PromotionCode,
                PromotionAwarded = promotionAwarded
            };
        }

                /// <summary>
        /// Từ chối hóa đơn không hợp lệ và gửi thông báo phản hồi lý do đến khách hàng.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Quy trình từ chối hóa đơn (Audit Rejection Handler).
        /// - MỤC ĐÍCH: Cập nhật Approved = 0, ghi nhận lý do từ chối (ReviewNotes) và gửi Push Notification phản hồi để khách hàng biết lý do.
        /// - CÁCH DÙNG: Được gọi khi kiểm toán viên bấm nút [Từ chối] trên giao diện Web Portal kèm lý do phản hồi.
        /// </remarks>
        /// <param name="request">Yêu cầu từ chối chứa mã hóa đơn và lý do từ chối.</param>
        /// <returns>Kết quả xử lý từ chối.</returns>
        public async Task<BillReviewResult> RejectBillAsync(RejectBillRequest request)
        {
            var bill = await _billRepository.GetByIdAsync(request.BillId);
            if (bill == null)
            {
                return new BillReviewResult { Success = false, Message = "Không tìm thấy hóa đơn cần từ chối." };
            }

            if (bill.Approved.HasValue)
            {
                return new BillReviewResult { Success = false, Message = "Hóa đơn này đã được xử lý trước đó." };
            }

            // NGUYÊN TẮC: Khi từ chối, Admin không nhập các trường _A, nên tất cả trường _A phải là null!
            // Tuyệt đối không copy số OCR vào các cột _A!
            await _billRepository.UpdateApprovalStatusAsync(
                bill.ID,
                approved: false,
                reviewBy: request.ReviewBy ?? "OperatorWeb",
                reviewNotes: request.Reason,
                approvedAmount: null,
                approvedBillNumber: null,
                approvedBillDateTime: null,
                approvedOutletNumber: null,
                approvedBrandNumber: null,
                pointGain: 0);

            // Gửi thông báo từ chối qua INotificationService (Tạm REM trong quá trình test vì chưa có DB thiết bị hội viên)
            /*
            try
            {
                await _notificationService.SendBillRejectedNotificationAsync(bill.MemID, request.Reason);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Chưa gửi được push notification từ chối cho MemID {MemId}", bill.MemID);
            }
            */

            return new BillReviewResult
            {
                Success = true,
                Message = "Đã từ chối hóa đơn và lưu lý do thành công."
            };
        }

                /// <summary>
        /// Webhook tiếp nhận kết quả bóc tách hóa đơn từ Server OCR chuyên dụng độc lập.
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Cổng kết nối tích hợp (Integration Ingestion Webhook).
        /// - MỤC ĐÍCH: Tiếp nhận kết quả trích xuất ban đầu (Số bill, ngày giờ, gian hàng, tổng tiền, ảnh) từ máy chủ OCR AI và lưu vào mssOCRTrialTrans ở trạng thái 'Pending'.
        /// - CÁCH DÙNG: Được gọi qua HTTP POST từ Server AI chuyên dụng sau khi bóc tách xong ảnh hóa đơn.
        /// </remarks>
        /// <param name="request">Dữ liệu hóa đơn OCR trích xuất từ máy chủ AI.</param>
        /// <returns>Mã định danh hóa đơn mới tạo (ID).</returns>
        public async Task<Guid> ReceiveOcrScanResultAsync(OcrWebhookRequest request)
        {
            var bill = new OcrTrialTrans
            {
                ID = Guid.NewGuid(),
                MemID = request.MemID,
                UploadImageDateTime = DateTime.Now,
                ImagePath = request.ImagePath,
                ImageFileName = request.ImageFileName,
                OutletNumber = request.OutletNumber,
                BrandNumber = request.BrandNumber,
                OutletName = request.OutletName,
                BrandName = request.BrandName,
                BillNumber = request.BillNumber,
                BillDateTime = request.BillDateTime,
                BillTotal = request.BillTotal,
                CashierName = request.CashierName,
                ScanDate = DateTime.Now,
                Approved = null,
                Reviewed = false,
                DataStatus = "Pending"
            };

            await _billRepository.InsertAsync(bill);
            return bill.ID;
        }

                /// <summary>
        /// Kích hoạt tiến trình quét và tự động phê duyệt hóa đơn theo lô (Batch Auto-Approval Runner).
        /// </summary>
        /// <remarks>
        /// - VAI TRÒ: Động cơ tự động hóa theo lô (Batch Automation Engine).
        /// - MỤC ĐÍCH: Lấy danh sách hóa đơn 'Pending', đánh giá qua IAutoApprovalPolicy. Nếu đạt 5 điều kiện an toàn thì tự động gọi ApproveBillAsync với tên người duyệt là 'AI_AUTO_APPROVE'.
        /// - CÁCH DÙNG: Được gọi từ CheckPoint Framework theo lịch định kỳ (cron job) hoặc từ API POST /api/bills/auto-review.
        /// </remarks>
        /// <param name="maxBatchSize">Số lượng hóa đơn xử lý tối đa mỗi lượt quét (mặc định 50).</param>
        /// <returns>Báo cáo tổng kết số lượng duyệt tự động và số lượng chuyển duyệt tay.</returns>
        public async Task<AutoReviewSummary> RunAutoApprovalBatchAsync(int maxBatchSize = 50)
        {
            var pendingBills = await _billRepository.GetPendingBillsAsync(null, null, limit: maxBatchSize);
            int evaluated = 0;
            int approved = 0;
            int manual = 0;

            foreach (var bill in pendingBills)
            {
                evaluated++;
                var customer = await _customerRepository.GetCustomerByIdAsync(bill.MemID);
                var decision = _autoApprovalPolicy.Evaluate(bill, customer ?? new CustomerInfo { Status = "Unknown" });

                if (decision.CanAutoApprove)
                {
                    decimal.TryParse(bill.BillTotal, out var totalAmount);
                    DateTime.TryParse(bill.BillDateTime, out var billDate);

                    var approveReq = new ApproveBillRequest
                    {
                        BillId = bill.ID,
                        ApprovedBillNumber = bill.BillNumber ?? "UNKNOWN",
                        ApprovedTotalAmount = totalAmount,
                        ApprovedBillDateTime = billDate,
                        ApprovedOutletNumber = bill.OutletNumber ?? "UNKNOWN",
                        ApprovedBrandNumber = bill.BrandNumber,
                        ReviewBy = "AI_AUTO_APPROVE",
                        ReviewNotes = "Tự động duyệt qua chính sách Auto-Approval."
                    };

                    var result = await ApproveBillAsync(approveReq);
                    if (result.Success)
                    {
                        approved++;
                    }
                    else
                    {
                        manual++;
                    }
                }
                else
                {
                    manual++;
                }
            }

            return new AutoReviewSummary
            {
                TotalEvaluated = evaluated,
                TotalAutoApproved = approved,
                TotalManualRequired = manual,
                Details = $"Đã xử lý {evaluated} hóa đơn: Tự duyệt {approved} bill, Chuyển duyệt tay {manual} bill."
            };
        }
    
        private Task CopyImageToOcrFolderAsync(OcrTrialTrans bill)
        {
            try
            {
                string baseImagePath = _appSetting.BaseImagePath;
                string dbimagePath = (bill.ImagePath ?? "").Trim().Replace("/", "\\").TrimStart('\\', '/');
                string imageFileName = (bill.ImageFileName ?? "").Trim();

                // Chuẩn hóa đường dẫn
                string relClean = dbimagePath;
                if (baseImagePath.TrimEnd('\\', '/').EndsWith("Transaction", StringComparison.OrdinalIgnoreCase) &&
                    dbimagePath.StartsWith("Transaction", StringComparison.OrdinalIgnoreCase))
                {
                    relClean = dbimagePath.Length > "Transaction".Length ? dbimagePath.Substring("Transaction".Length).TrimStart('\\', '/') : "";
                }

                // File gốc: BaseImagePath + dbimagePath + ImageFileName
                string sourceFile = Path.Combine(baseImagePath, dbimagePath, imageFileName);
                if (!File.Exists(sourceFile))
                {
                    sourceFile = Path.Combine(baseImagePath, relClean, imageFileName);
                }

                // Thư mục đích khi duyệt: BaseImagePath + dbimagePath + OCR\ (ví dụ: ...\Transaction\241026\OCR\)
                string targetDir = Path.Combine(baseImagePath, dbimagePath, _appSetting.CommonSettings.OCRImageFolder);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                string targetFile = Path.Combine(targetDir, imageFileName);
                if (File.Exists(sourceFile))
                {
                    File.Move(sourceFile, targetFile, overwrite: true);
                    _logger.LogInformation("[PROD MODE] Đã chuyển (CUT) file ảnh từ {Source} sang {Target}", sourceFile, targetFile);
                }
                else
                {
                    _logger.LogWarning("[PROD MODE] Không tìm thấy file ảnh gốc: {SourceFile}", sourceFile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chép file ảnh sang thư mục OCR cho bill {BillId}", bill.ID);
            }
            return Task.CompletedTask;
        }

        public async Task<PagedResultDto<PendingUploadBillDto>> GetBillsPagedAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? brandNumber = null,
            string? outletNumber = null,
            string? keyword = null,
            bool? ocrStatus = null,
            bool? reviewed = null,
            bool? approved = null,
            int pageIndex = 1,
            int pageSize = 25)
        {
            var (rawBills, totalCount) = await _billRepository.GetBillsPagedAsync(
                fromDate, toDate, brandNumber, outletNumber, keyword, ocrStatus, reviewed, approved, pageIndex, pageSize);

            var results = new List<PendingUploadBillDto>();
            foreach (var b in rawBills)
            {
                bool hasOcr = !string.IsNullOrWhiteSpace(b.BillNumber) || !string.IsNullOrWhiteSpace(b.BillTotal);
                results.Add(new PendingUploadBillDto
                {
                    ID = b.ID,
                    MemID = b.MemID,
                    UploadImageDateTime = b.UploadImageDateTime,
                    ImagePath = b.ImagePath,
                    ImageFileName = b.ImageFileName,
                    ImageUrl = $"/api/bills/{b.ID}/image",
                    DataStatus = b.DataStatus ?? "Pending",
                    OCRStatus = b.OCRStatus,
                    Reviewed = b.Reviewed,
                    Approved = b.Approved,
                    HasOcrResult = hasOcr,
                    ScannedOutletNumber = b.OutletNumber,
                    ScannedOutletName = b.OutletName,
                    ScannedBrandName = b.BrandName,
                    ScannedBillNumber = b.BillNumber,
                    ScannedBillDateTime = b.BillDateTime,
                    ScannedBillTotal = b.BillTotal,
                    ScannedCashierName = b.CashierName,
                    BillNumberA = b.BillNumberA,
                    BillDateTimeA = b.BillDateTimeA,
                    BillTotalA = b.BillTotalA,
                    OutletNumberA = b.OutletNumberA,
                    BrandNumberA = b.BrandNumberA,
                    ReviewNotes = b.ReviewNotes
                });
            }

            return new PagedResultDto<PendingUploadBillDto>
            {
                Items = results,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<FilterOptionsDto> GetFilterOptionsAsync()
        {
            var brands = (await _billRepository.GetDistinctBrandsAsync())
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .Distinct()
                .OrderBy(b => b)
                .ToList();

            var outletsRaw = await _billRepository.GetDistinctOutletsAsync();
            var outlets = outletsRaw
                .Where(o => !string.IsNullOrWhiteSpace(o.OutletNumber))
                .Select(o => new OutletOptionDto
                {
                    OutletNumber = o.OutletNumber,
                    OutletName = string.IsNullOrWhiteSpace(o.OutletName) ? o.OutletNumber : o.OutletName,
                    BrandNumber = o.BrandNumber
                })
                .OrderBy(o => o.OutletName)
                .ToList();

            return new FilterOptionsDto
            {
                Brands = brands,
                Outlets = outlets
            };
        }

        /// <summary>
        /// Ghi đè trực tiếp tệp ảnh hóa đơn trên đĩa cứng (hỗ trợ lưu ảnh xoay đúng hướng từ Web UI).
        /// </summary>
        public async Task<bool> OverwriteBillImageAsync(Guid billId, byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0) return false;

            var bill = await _billRepository.GetByIdAsync(billId);
            if (bill == null)
            {
                _logger.LogWarning("Không tìm thấy bill {BillId} để ghi đè ảnh.", billId);
                return false;
            }

            string baseImagePath = _appSetting.BaseImagePath;
            string dbimagePath = (bill.ImagePath ?? "").Trim().Replace("/", "\\").TrimStart('\\', '/');
            string imageFileName = (bill.ImageFileName ?? "").Trim();

            if (string.IsNullOrEmpty(imageFileName))
            {
                _logger.LogWarning("Bill {BillId} không có ImageFileName để ghi đè.", billId);
                return false;
            }

            // Chuẩn hóa đường dẫn tránh lặp nếu baseImagePath đã kết thúc bằng Transaction
            string relClean = dbimagePath;
            if (baseImagePath.TrimEnd('\\', '/').EndsWith("Transaction", StringComparison.OrdinalIgnoreCase) &&
                dbimagePath.StartsWith("Transaction", StringComparison.OrdinalIgnoreCase))
            {
                relClean = dbimagePath.Length > "Transaction".Length ? dbimagePath.Substring("Transaction".Length).TrimStart('\\', '/') : "";
            }

            string fullImagePath = Path.Combine(baseImagePath, dbimagePath, imageFileName);
            if (!File.Exists(fullImagePath) && !string.IsNullOrEmpty(relClean) && !string.Equals(relClean, dbimagePath, StringComparison.OrdinalIgnoreCase))
            {
                string altPath = Path.Combine(baseImagePath, relClean, imageFileName);
                if (File.Exists(altPath)) fullImagePath = altPath;
            }

            string? dir = Path.GetDirectoryName(fullImagePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await File.WriteAllBytesAsync(fullImagePath, imageBytes);
            _logger.LogInformation("Đã ghi đè file ảnh thành công cho bill {BillId} tại: {FullImagePath} (size: {Size} bytes)", 
                billId, fullImagePath, imageBytes.Length);
            return true;
        }
    }
}
