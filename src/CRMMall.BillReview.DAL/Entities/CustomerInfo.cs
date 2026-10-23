using System;

namespace CRMMall.BillReview.DAL.Entities
{
    public class CustomerInfo
    {
        public string MemID { get; set; } = string.Empty;
        public string? CardNo { get; set; }
        public string? FullName { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string Status { get; set; } = "L"; // L: Life (Đang hoạt động), S: Suspend (Tạm khóa)
        public DateTime? DateJoined { get; set; }
    }
}
