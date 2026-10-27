using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CRMMall.BillReview.BLL.Configurations;
using CRMMall.BillReview.BLL.DTOs;

namespace CRMMall.BillReview.BLL.Services
{
    /// <summary>
    /// Triển khai kết nối HTTP Client tới Máy chủ OCR Chuyên dụng Gigamall.
    /// Đọc cấu hình tập trung từ OcrSettings (appsettings.json).
    /// </summary>
    public class OcrApiClient : IOcrApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OcrApiClient> _logger;
        private readonly ApplicationSetting _appSetting;

        public OcrApiClient(
            HttpClient httpClient, 
            ApplicationSetting appSetting, 
            ILogger<OcrApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _appSetting = appSetting ?? new ApplicationSetting();

            string baseUrl = _appSetting.OcrBaseUrl;
            if (!baseUrl.EndsWith("/")) baseUrl += "/";
            _httpClient.BaseAddress = new Uri(baseUrl);
            int timeout = _appSetting.CommonSettings.TimeoutSeconds;
            _httpClient.Timeout = TimeSpan.FromSeconds(timeout > 0 ? timeout : 30);
        }

        public async Task<bool> CheckHealthAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/health");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể kết nối tới OCR Server tại {BaseUrl}", _appSetting.OcrBaseUrl);
                return false;
            }
        }

        /// <summary>
        /// [API 3: POST api/v1/extract-bill] Bóc tách hóa đơn trực tiếp (Core AI bóc tách đồng bộ).
        /// Gửi tệp nhị phân của ảnh qua Multipart/Form-data cho hiệu năng tối ưu nhất.
        /// </summary>
        public async Task<OcrExtractApiResponse> ExtractBillAsync(
            byte[] imageBytes, 
            string fileName, 
            string memId = "", 
            string imageNumber = "", 
            string duplicateAction = "allow")
        {
            var result = new OcrExtractApiResponse
            {
                Filename = fileName,
                Success = false
            };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            string endpoint = _appSetting.CommonSettings.Endpoints.ExtractBill; // Đọc từ cấu hình OcrSettings

            try
            {
                using var form = new MultipartFormDataContent();

                // 1. Tệp nhị phân ảnh hóa đơn
                var fileContent = new ByteArrayContent(imageBytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(fileName));
                form.Add(fileContent, "file", fileName);

                // 2. Các tham số nghiệp vụ kèm theo
                if (!string.IsNullOrWhiteSpace(memId))
                {
                    form.Add(new StringContent(memId), "mem_id");
                }
                if (!string.IsNullOrWhiteSpace(imageNumber))
                {
                    form.Add(new StringContent(imageNumber), "image_number");
                }
                
                string action = string.IsNullOrWhiteSpace(duplicateAction) ? _appSetting.CommonSettings.DuplicateAction : duplicateAction;
                form.Add(new StringContent(action), "duplicate_action");

                _logger.LogInformation("Gửi ảnh {FileName} (size: {Size} bytes) tới OCR endpoint '{Endpoint}'...", 
                    fileName, imageBytes.Length, endpoint);

                var response = await _httpClient.PostAsync(endpoint, form);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OCR Server trả lỗi HTTP {StatusCode}: {Body}", response.StatusCode, responseContent);
                    result.Message = $"OCR Server trả lỗi HTTP {response.StatusCode}: {responseContent}";
                    stopwatch.Stop();
                    result.LatencySeconds = stopwatch.Elapsed.TotalSeconds;
                    return result;
                }

                var rootJson = JsonNode.Parse(responseContent);
                if (rootJson == null)
                {
                    result.Message = "Không phân tích được phản hồi JSON từ OCR Server.";
                    stopwatch.Stop();
                    result.LatencySeconds = stopwatch.Elapsed.TotalSeconds;
                    return result;
                }

                bool isSuccess = rootJson["success"]?.GetValue<bool>() ?? false;
                result.Success = isSuccess;
                result.BillId = rootJson["bill_id"]?.GetValue<int>() ?? 0;
                result.Filename = rootJson["filename"]?.ToString() ?? fileName;
                result.IsDuplicate = rootJson["is_duplicate"]?.GetValue<bool>() ?? false;
                result.DuplicateAction = rootJson["duplicate_action"]?.ToString();
                result.RawOutput = rootJson["raw_output"]?.ToString() ?? responseContent;

                double latency = rootJson["latency_seconds"]?.GetValue<double>() ?? stopwatch.Elapsed.TotalSeconds;
                result.LatencySeconds = latency;

                // Phân tích dữ liệu bóc tách bên trong object 'data' (hỗ trợ cả snake_case và PascalCase)
                var dataNode = rootJson["data"];
                if (dataNode != null)
                {
                    var ext = new OcrExtractedData();
                    ext.BrandName = dataNode["brand_name"]?.ToString() ?? dataNode["BrandName"]?.ToString() ?? dataNode["brand"]?.ToString() ?? string.Empty;
                    ext.OutletName = dataNode["outlet_name"]?.ToString() ?? dataNode["OutletName"]?.ToString() ?? string.Empty;
                    ext.OutletNumber = dataNode["outlet_number"]?.ToString() ?? dataNode["OutletNumber"]?.ToString() ?? string.Empty;
                    ext.BillNumber = dataNode["bill_number"]?.ToString() ?? dataNode["BillNumber"]?.ToString() ?? string.Empty;
                    ext.BillDatetime = dataNode["bill_datetime"]?.ToString() ?? dataNode["BillDateTime"]?.ToString() ?? dataNode["datetime"]?.ToString();
                    ext.CashierName = dataNode["cashier_name"]?.ToString() ?? dataNode["CashierName"]?.ToString();

                    // Parse Tổng tiền
                    if (dataNode["bill_total"] != null)
                    {
                        try { ext.BillTotal = Convert.ToDecimal(dataNode["bill_total"]!.ToString()); } catch { ext.BillTotal = 0m; }
                    }
                    else if (dataNode["BillTotal"] != null)
                    {
                        try { ext.BillTotal = Convert.ToDecimal(dataNode["BillTotal"]!.ToString()); } catch { ext.BillTotal = 0m; }
                    }

                    ext.MatchScore = dataNode["match_score"]?.GetValue<float>() ?? dataNode["MatchScore"]?.GetValue<float>() ?? 0.85f;
                    ext.Confidence = dataNode["confidence"]?.GetValue<float>() ?? dataNode["Confidence"]?.GetValue<float>() ?? 0.85f;
                    ext.IsValidBill = dataNode["is_valid_bill"]?.GetValue<bool>() ?? dataNode["IsValidBill"]?.GetValue<bool>() ?? true;

                    result.Data = ext;
                }

                stopwatch.Stop();
                result.Message = isSuccess ? $"Bóc tách AI hoàn tất thành công trong {result.LatencySeconds:F2}s" : (rootJson["message"]?.ToString() ?? "Bóc tách không thành công");
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Ngoại lệ khi gọi OCR Server qua endpoint '{Endpoint}' cho file {FileName}", endpoint, fileName);
                result.Message = $"Lỗi kết nối OCR: {ex.Message}";
                result.LatencySeconds = stopwatch.Elapsed.TotalSeconds;
                return result;
            }
        }

        /// <summary>
        /// [API 2: POST api/v1/bills/upload-base64]
        /// </summary>
        public async Task<OcrExtractApiResponse> ExtractBillBase64Async(
            string imageBase64, 
            string fileName, 
            string memId = "", 
            string duplicateAction = "allow")
        {
            var result = new OcrExtractApiResponse
            {
                Filename = fileName,
                Success = false
            };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            string endpoint = _appSetting.CommonSettings.Endpoints.UploadBase64;

            try
            {
                var payload = new
                {
                    image_base64 = imageBase64,
                    filename = fileName,
                    actor = string.IsNullOrWhiteSpace(memId) ? "CRM_UI" : memId,
                    note = "Gửi từ CRM Base64 API",
                    async_ocr = false,
                    auto_ocr = true,
                    duplicate_action = duplicateAction
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(endpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    result.Message = $"OCR Server Base64 trả lỗi HTTP {response.StatusCode}: {responseContent}";
                    stopwatch.Stop();
                    result.LatencySeconds = stopwatch.Elapsed.TotalSeconds;
                    return result;
                }

                var rootJson = JsonNode.Parse(responseContent);
                stopwatch.Stop();

                result.Success = rootJson?["success"]?.GetValue<bool>() ?? false;
                result.BillId = rootJson?["bill_id"]?.GetValue<int>() ?? 0;
                result.Filename = rootJson?["filename"]?.ToString() ?? fileName;
                result.RawOutput = responseContent;
                result.LatencySeconds = stopwatch.Elapsed.TotalSeconds;
                result.Message = "Bóc tách Base64 hoàn tất";
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Message = $"Lỗi Base64 OCR: {ex.Message}";
                result.LatencySeconds = stopwatch.Elapsed.TotalSeconds;
                return result;
            }
        }

        private static string GetContentType(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".bmp" => "image/bmp",
                _ => "application/octet-stream"
            };
        }
    }
}
