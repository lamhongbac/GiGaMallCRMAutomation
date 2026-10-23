using CRMMall.BillReview.BLL.Policies;
using CRMMall.BillReview.BLL.Services;
using CRMMall.BillReview.DAL.Data;
using CRMMall.BillReview.DAL.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "CRMMall Bill Review & Approval API", 
        Version = "v1",
        Description = "Microservice đối soát và phê duyệt hóa đơn Loyalty Gigamall (Không bao gồm OCR, tiếp nhận kết quả từ Server OCR độc lập)." 
    });
});

// Đăng ký 3 tầng (DAL, BLL)
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddScoped<IBillRepository, BillRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ILoyaltyRepository, LoyaltyRepository>();
builder.Services.AddScoped<IAutoApprovalPolicy, AutoApprovalPolicy>();

// Đăng ký các thư viện logic độc lập có thể tái sử dụng (BLL Domain Services)
builder.Services.AddScoped<ILoyaltyPointsService, LoyaltyPointsService>();
builder.Services.AddScoped<IMembershipTierService, MembershipTierService>();
builder.Services.AddScoped<ILotteryPromotionService, LotteryPromotionService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Đăng ký Orchestrator duyệt hóa đơn
builder.Services.AddScoped<IBillReviewService, BillReviewService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CRMMall Bill Review API v1");
    });
}

app.UseCors("AllowAll");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
