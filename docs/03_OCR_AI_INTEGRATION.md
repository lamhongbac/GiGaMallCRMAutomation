# ĐẶC TẢ TÍCH HỢP MÁY CHỦ BÓC TÁCH AI OCR (OCR AI INTEGRATION)
## BƯỚC 2: MÁY CHỦ AI QWEN2-VL, OCRAPICLIENT VÀ ĐẶC TẢ HỢP ĐỒNG API

> 🏢 **Hệ sinh thái**: CRM Loyalty Gigamall - Sao Mai Solution  
> 🎯 **Mục tiêu**: Bóc tách tự động hóa đơn mua sắm bằng mô hình VLM tự lưu trữ (Qwen2-VL), loại bỏ chi phí Google Cloud Vision và loại bỏ việc cấu hình tọa độ template thủ công.  
> 🏛️ **Dự án cũ tham khảo**: `D:\GIT PROJECT\CRM SAOMAI\CRMPROFILELogic\SourceCode\CRMProfileManagement`  
> 📁 **Tệp nguồn tham khảo**: `OCRBillProcess/OCRHelper.cs` & `CRMProfileManagement/OCRBillPROCESS/OCR_Helper.cs`

---

## 🔸 PHẦN 1: HIỆN TRẠNG HỆ THỐNG CŨ (AS-IS / LEGACY ANALYSIS)

### 1.1. Công nghệ cũ: Google Cloud Vision API & Template tọa độ cứng
1. **Google Cloud Vision API (`Google.Cloud.Vision.V1`)**:
   - Gửi ảnh lên Google Cloud thông qua thư viện grpc `AnnotateImageRequest`.
   - Google chỉ trả về chuỗi văn bản nhận dạng quang học thô (`fullTextAnnotation.text`).
   - Tốn chi phí bản quyền API ($1.50 cho mỗi 1,000 hóa đơn) và phụ thuộc hoàn toàn vào đường truyền quốc tế.
2. **Cơ chế bóc tách tọa độ cứng (`InvoiceConfig` & `SignalConfig`)**:
   - Nhân viên phải vào form `frmOCRBillConfigV2` vẽ từng dòng, từng cột tọa độ `Top`, `Left`, `Width`, `Height` cho từng máy in của từng cửa hàng.
   - Khi hóa đơn bị nhăn, chụp nghiêng, thiếu sáng hoặc gian hàng thay đổi định dạng mẫu in, thuật toán bóc tách bị sai lệch 100%.

---

## 🔹 PHẦN 2: THIẾT KẾ HỆ THỐNG MỚI (TO-BE / MODERN SPECIFICATION)

### 2.1. Kiến trúc Máy chủ AI OCR Độc lập (Self-hosted VLM Server)
* **Mô hình AI**: Vision-Language Model (`Qwen2-VL-7B-Instruct`) tối ưu trên framework `mlx-vlm` / `vLLM`.
* **Máy chủ dịch vụ**: FastAPI REST API chạy trên hạ tầng riêng:
  - Base URL: `http://14.161.34.37:8080` (hoặc `http://127.0.0.1:8000` nội bộ).
  - Swagger Docs: `http://14.161.34.37:8080/docs`.
  - Endpoint bóc tách hóa đơn: `POST /api/v1/extract-bill` (Content-Type: `multipart/form-data`).

### 2.2. Đặc tả Hợp đồng API (`/api/v1/extract-bill`)

#### Request:
Gửi tập tin ảnh nhị phân qua form field `file`:
```http
POST /api/v1/extract-bill HTTP/1.1
Host: 14.161.34.37:8080
Content-Type: multipart/form-data; boundary=----WebKitFormBoundary

------WebKitFormBoundary
Content-Disposition: form-data; name="file"; filename="bill_sample.png"
Content-Type: image/png

<binary data>
------WebKitFormBoundary--
```

#### Response JSON:
```json
{
  "success": true,
  "bill_id": 1024,
  "filename": "0901234567_241026101530.png",
  "is_duplicate": false,
  "duplicate_action": "none",
  "latency_seconds": 1.42,
  "data": {
    "brand_name": "HIGHLANDS COFFEE",
    "outlet_name": "Highlands Gigamall Tầng 1",
    "outlet_number": "HL-GIGA-01",
    "bill_number": "HD-982341",
    "bill_datetime": "2026-10-24 10:15:00",
    "bill_total": 125000.0,
    "cashier_name": "Nguyen Van A",
    "outlet_address": "Tầng 1, TTTM Gigamall, 240-242 Phạm Văn Đồng",
    "is_valid_bill": true,
    "match_score": 0.98,
    "confidence": 0.95
  },
  "raw_output": "{...}"
}
```

### 2.3. Triển khai `OcrApiClient` trong `CRMMall.BillReview.BLL`
Sử dụng `HttpClient` quản lý qua `IHttpClientFactory` kết hợp chính sách chịu lỗi Polly (Timeout 30s, Retry 2 lần với Exponential Backoff):

```csharp
public class OcrApiClient : IOcrApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OcrApiClient> _logger;

    public OcrApiClient(HttpClient httpClient, ILogger<OcrApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<OcrExtractApiResponse> ExtractBillAsync(byte[] imageBytes, string fileName)
    {
        using var content = new MultipartFormDataContent();
        using var byteContent = new ByteArrayContent(imageBytes);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(byteContent, "file", fileName);

        var response = await _httpClient.PostAsync("/api/v1/extract-bill", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<OcrExtractApiResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}
```

### 2.4. Lưu kết quả bóc tách vào `mssOCRTrans`
Khi bấm nút **[Save OCR]** trên Tab 1:
- Cập nhật các trường: `BrandNumber`, `OutletNumber`, `BillNumber`, `BillDateTime`, `BillTotal`, `CashierName`, `OutletAddress`, `OCRText` (JSON thô), `ScanDate = DateTime.Now`.
- Đánh dấu **`OCRStatus = 1`** và chuyển trạng thái `DataStatus = 'OCR'`.
- Hóa đơn tự động biến mất khỏi Tab 1 và xuất hiện tại Tab 2 (Chờ Admin thẩm định).
