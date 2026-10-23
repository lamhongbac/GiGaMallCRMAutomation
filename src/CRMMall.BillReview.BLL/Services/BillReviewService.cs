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

        public BillReviewService(
            IBillRepository billRepository,
            ICustomerRepository customerRepository,
            IAutoApprovalPolicy autoApprovalPolicy,
            ILoyaltyPointsService loyaltyPointsService,
            IMembershipTierService membershipTierService,
            ILotteryPromotionService lotteryPromotionService,
            INotificationService notificationService)
        {
            _billRepository = billRepository;
            _customerRepository = customerRepository;
            _autoApprovalPolicy = autoApprovalPolicy;
            _loyaltyPointsService = loyaltyPointsService;
            _membershipTierService = membershipTierService;
            _lotteryPromotionService = lotteryPromotionService;
            _notificationService = notificationService;
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
            #region BƯỚC 1: KIỂM TRA HỢP LỆ (VALIDATION & AUDIT)
            // -----------------------------------------------------------------------------------------
            var bill = await _billRepository.GetByIdAsync(request.BillId);
            if (bill == null)
            {
                return new BillReviewResult 
                { 
                    Success = false, 
                    Message = "[BƯỚC 1 - LỖI] Không tìm thấy bản ghi hóa đơn cần phê duyệt trong hệ thống." 
                };
            }

            if (bill.Approved == true)
            {
                return new BillReviewResult 
                { 
                    Success = false, 
                    Message = "[BƯỚC 1 - CẢNH BÁO] Hóa đơn này đã được phê duyệt tích điểm trước đó." 
                };
            }

            var customer = await _customerRepository.GetCustomerByIdAsync(bill.MemID);
            if (customer == null)
            {
                return new BillReviewResult 
                { 
                    Success = false, 
                    Message = $"[BƯỚC 1 - LỖI] Không tìm thấy hồ sơ hội viên với mã MemID: {bill.MemID}." 
                };
            }

            if (customer.Status != "L")
            {
                return new BillReviewResult 
                { 
                    Success = false, 
                    Message = $"[BƯỚC 1 - LỖI] Tài khoản hội viên {bill.MemID} đang ở trạng thái '{customer.Status}' (Yêu cầu 'L' - Life / Đang hoạt động, không bị 'S' - Suspend)." 
                };
            }

            if (request.ApprovedTotalAmount <= 0)
            {
                return new BillReviewResult 
                { 
                    Success = false, 
                    Message = "[BƯỚC 1 - LỖI] Số tiền phê duyệt phải lớn hơn 0 VNĐ." 
                };
            }

            var isDuplicate = await _billRepository.IsBillNumberExistsAsync(request.ApprovedOutletNumber, request.ApprovedBillNumber, bill.ID);
            if (isDuplicate)
            {
                return new BillReviewResult 
                { 
                    Success = false, 
                    Message = $"[BƯỚC 1 - GIAN LẬN] Số hóa đơn '{request.ApprovedBillNumber}' đã được tích điểm cho gian hàng '{request.ApprovedOutletNumber}' trước đó!" 
                };
            }
            #endregion

            #region BƯỚC 2: TÍNH TOÁN & GHI SỔ ĐIỂM LOYALTY (TÁI SỬ DỤNG QUA ILoyaltyPointsService)
            // -----------------------------------------------------------------------------------------
            var pointsResult = await _loyaltyPointsService.ProcessSalePointsAsync(
                bill.MemID,
                customer.CardNo,
                bill.ID,
                request.ApprovedBillNumber,
                request.ApprovedOutletNumber,
                request.ApprovedBillDateTime,
                request.ApprovedTotalAmount,
                bill.CashierName,
                request.ReviewBy,
                request.ReviewNotes);

            if (!pointsResult.Success)
            {
                return new BillReviewResult
                {
                    Success = false,
                    Message = $"[BƯỚC 2 - LỖI] {pointsResult.Message}"
                };
            }
            #endregion

            #region BƯỚC 3: THẨM ĐỊNH & THĂNG HẠNG THÀNH VIÊN (TÁI SỬ DỤNG QUA IMembershipTierService)
            // -----------------------------------------------------------------------------------------
            var tierResult = await _membershipTierService.EvaluateAndUpgradeTierAsync(bill.MemID, request.ApprovedBillNumber);
            #endregion

            #region BƯỚC 4: ĐÁNH GIÁ & PHÁT VÉ QUAY SỐ / QUÀ TẶNG (TÁI SỬ DỤNG QUA ILotteryPromotionService)
            // -----------------------------------------------------------------------------------------
            var lotteryAwarded = await _lotteryPromotionService.EvaluateAndIssueLotteryAsync(
                bill.MemID,
                customer.DateJoined,
                request.ApprovedBillNumber,
                request.ApprovedBillDateTime,
                request.ApprovedTotalAmount,
                request.ApprovedOutletNumber);

            var promotionAwarded = await _lotteryPromotionService.EvaluateAndIssuePromotionAsync(
                bill.MemID,
                request.ApprovedBillNumber,
                request.ApprovedBillDateTime,
                request.ApprovedTotalAmount,
                request.ApprovedOutletNumber);
            #endregion

            #region BƯỚC 5: ĐIỀU PHỐI THÔNG BÁO ĐẨY TỨC THÌ (TÁI SỬ DỤNG QUA INotificationService)
            // -----------------------------------------------------------------------------------------
            // 5.1 Thông báo cộng điểm bán hàng
            await _notificationService.SendSalePointsNotificationAsync(
                bill.MemID,
                request.ApprovedBillNumber,
                request.ApprovedOutletNumber,
                pointsResult.PointsAwarded,
                pointsResult.RewardRate);

            // 5.2 Thông báo chúc mừng thăng hạng
            if (tierResult.IsUpgraded && !string.IsNullOrWhiteSpace(tierResult.NewTier))
            {
                await _notificationService.SendTierUpgradedNotificationAsync(bill.MemID, tierResult.NewTier);
            }

            // 5.3 Thông báo phát vé quay số may mắn
            if (lotteryAwarded != null)
            {
                await _notificationService.SendLotteryTicketNotificationAsync(bill.MemID, lotteryAwarded);
            }

            // 5.4 Thông báo phát quà tặng / voucher khuyến mãi
            if (promotionAwarded != null)
            {
                await _notificationService.SendPromotionGiftNotificationAsync(bill.MemID, promotionAwarded);
            }
            #endregion

            #region BƯỚC 6: CẬP NHẬT TRẠNG THÁI HÓA ĐƠN & LƯU TRỮ (FINAL PERSISTENCE)
            // -----------------------------------------------------------------------------------------
            await _billRepository.UpdateApprovalStatusAsync(
                bill.ID,
                approved: true,
                reviewBy: request.ReviewBy,
                reviewNotes: request.ReviewNotes,
                approvedAmount: request.ApprovedTotalAmount,
                approvedBillNumber: request.ApprovedBillNumber,
                approvedBillDateTime: request.ApprovedBillDateTime,
                approvedOutletNumber: request.ApprovedOutletNumber,
                approvedBrandNumber: request.ApprovedBrandNumber,
                pointGain: pointsResult.PointsAwarded);

            var extras = new List<string>();
            if (tierResult.IsUpgraded) extras.Add($"Thăng hạng lên {tierResult.NewTier}");
            if (lotteryAwarded != null) extras.Add($"Vé quay số #{lotteryAwarded.LotteryCode}");
            if (promotionAwarded != null) extras.Add($"Quà khuyến mãi #{promotionAwarded.PromotionCode}");

            string extraNotes = extras.Any() ? $" (Đặc biệt: {string.Join(", ", extras)})" : string.Empty;

            return new BillReviewResult
            {
                Success = true,
                Message = $"[HOÀN TẤT 6 BƯỚC] Đã phê duyệt hóa đơn {request.ApprovedBillNumber} thành công! Tích lũy +{pointsResult.PointsAwarded:N0} điểm thưởng (+{pointsResult.RankPointsAwarded:N0} điểm xếp hạng) cho hội viên {bill.MemID}.{extraNotes}",
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
            #endregion
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

            await _billRepository.UpdateApprovalStatusAsync(
                bill.ID,
                approved: false,
                reviewBy: request.ReviewBy,
                reviewNotes: request.Reason,
                approvedAmount: 0,
                approvedBillNumber: bill.BillNumber,
                approvedBillDateTime: null,
                approvedOutletNumber: bill.OutletNumber,
                approvedBrandNumber: bill.BrandNumber,
                pointGain: 0);

            // Gửi thông báo từ chối qua INotificationService
            await _notificationService.SendBillRejectedNotificationAsync(bill.MemID, request.Reason);

            return new BillReviewResult
            {
                Success = true,
                Message = "Đã từ chối hóa đơn và gửi thông báo phản hồi tới hội viên."
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
    }
}
