
using EMS.API.BackgroundServices;
using EMS.Application.Common.Interfaces;
using EMS.Application.Features.Accounts.Services;
using EMS.Application.Features.Assignments.Services;
using EMS.Application.Features.Auth.Services;
using EMS.Application.Features.Classes.Services;
using EMS.Application.Features.LearningMaterials.Services;
using EMS.Application.Features.Notifications.Services;
using EMS.Application.Features.Posts.Services;
using EMS.Application.Features.Feedbacks.Services;
using EMS.Application.Features.ProgressReports.Validators;
using EMS.Application.Features.Sessions.Services;
using EMS.Application.Features.Students.Services;
using EMS.Application.Features.SystemAdmin.Services;
using EMS.Application.Features.TuitionFees.Services;
using EMS.Application.Features.TuitionFees.Validators;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Configuration;
using EMS.Infrastructure.Data;
using EMS.Infrastructure.Repositories;
using EMS.Infrastructure.Services; 
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));



// 2. ĐĂNG KÝ EMAIL SERVICE (Dùng HttpClient cho Brevo API)
// Dòng này cực kỳ quan trọng: Nó vừa đăng ký IEmailService, vừa nạp HttpClient vào EmailService
builder.Services.AddHttpClient<IEmailService, EmailService>();


// 3. ĐĂNG KÝ REPOSITORY (Infrastructure)
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IClassRepository, ClassRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<IAssignmentRepository, AssignmentRepository>();
builder.Services.AddScoped<ISubmissionRepository, SubmissionRepository>();
builder.Services.AddScoped<ITARepository, TARepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<ILearningMaterialRepository, LearningMaterialRepository>();
builder.Services.AddScoped<IProgressReportRepository, ProgressReportRepository>();
builder.Services.AddScoped<ITuitionFeeRepository, TuitionFeeRepository>();
builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();
// Progress report service and repository
builder.Services.AddScoped<EMS.Application.Features.ProgressReports.Services.IProgressReportService, EMS.Application.Features.ProgressReports.Services.ProgressReportService>();
builder.Services.AddScoped<IGradeCategoryRepository, GradeCategoryRepository>();
builder.Services.AddScoped<ISystemAdminRepository, SystemAdminRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();

builder.Services.Configure<SupabaseSettings>(builder.Configuration.GetSection("SupabaseSettings"));

// Supabase Client Setup
var supabaseUrl = builder.Configuration["SupabaseSettings:Url"] ?? throw new ArgumentNullException("Supabase Url is missing");
var supabaseKey = builder.Configuration["SupabaseSettings:Key"] ?? throw new ArgumentNullException("Supabase Key is missing");
var options = new Supabase.SupabaseOptions
{
    AutoConnectRealtime = true
};
builder.Services.AddSingleton(provider => new Supabase.Client(supabaseUrl, supabaseKey, options));

// 3. Đăng ký Service (Application/Infrastructure)
builder.Services.AddScoped<ISupabaseStorageService, EMS.Infrastructure.Services.Supabase.SupabaseStorageService>();
builder.Services.AddScoped<IClassService, ClassService>();
//Student
builder.Services.AddScoped<IStudentAccountService, StudentAccountService>();
builder.Services.AddScoped<IStudentClassService, StudentClassService>();
builder.Services.AddScoped<IStudentAssignmentService, StudentAssignmentService>();
builder.Services.AddScoped<IStudentMaterialService, StudentMaterialService>();
builder.Services.AddScoped<IStudentScheduleService, StudentScheduleService>();
builder.Services.AddScoped<IStudentTuitionService, StudentTuitionService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<IClassTAService, ClassTAService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ILearningMaterialService, LearningMaterialService>();
//builder.Services.AddScoped<IProgressReportService, ProgressReportService>();
builder.Services.AddHttpClient<IVietQRService, VietQRService>();
builder.Services.AddScoped<ITuitionFeeService, TuitionFeeService>();
// Gradebook feature
builder.Services.AddScoped<EMS.Application.Features.Gradebook.Services.IGradebookService, EMS.Application.Features.Gradebook.Services.GradebookService>();

// Đăng ký Interface và Class triển khai thực tế của nó
builder.Services.AddScoped<IStudentMaterialService, StudentMaterialService>();
builder.Services.AddScoped<ISystemAdminService,SystemAdminService>();
builder.Services.AddScoped<INotificationService, NotificationService>();


builder.Services.AddFluentValidationAutoValidation(); // Tự động chặn Request nếu dữ liệu sai và trả về lỗi 400
builder.Services.AddFluentValidationClientsideAdapters();
// Lệnh này sẽ tự động tìm tất cả các class Validator trong cùng một thư mục/Assembly với CreateProgressReportValidator
builder.Services.AddValidatorsFromAssemblyContaining<CreateProgressReportValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateTuitionFeeValidator>();

// Đăng ký Worker tự động hóa
builder.Services.AddHostedService<InvoiceAutomationWorker>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins(
                "http://localhost:5173",
                "https://ems-fe-six.vercel.app",
                "https://ems-be-2-s2nk.onrender.com",
                "https://localhost:7049"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        });
});


// 4. CẤU HÌNH JWT AUTHENTICATION
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"] ?? throw new ArgumentNullException("Jwt Secret Key is missing in appsettings.json");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        // Reduce default clock skew to avoid tokens being accepted slightly after expiry
        ClockSkew = TimeSpan.Zero
    };
    // In development, allow HTTP metadata endpoints; in production ensure HTTPS
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
});


//builder.Services.AddControllers();
//// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 5. CẤU HÌNH SWAGGER (CÓ NÚT Ổ KHÓA BẢO MẬT)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "EMS API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header sử dụng scheme Bearer. \r\n\r\n Nhập 'Bearer' [khoảng trắng] và sau đó dán token của bạn vào.\r\n\r\nVí dụ: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});



var app = builder.Build();

// Diagnostic check: verify DI registrations for progress report service/repository
using (var startupScope = app.Services.CreateScope())
{
    var sp = startupScope.ServiceProvider;
    var prSvc = sp.GetService<EMS.Application.Features.ProgressReports.Services.IProgressReportService>();
    var prRepo = sp.GetService<EMS.Domain.Interfaces.IProgressReportRepository>();
    if (prSvc == null)
        System.Console.WriteLine("DI CHECK: IProgressReportService NOT registered");
    else
        System.Console.WriteLine("DI CHECK: IProgressReportService registered");

    if (prRepo == null)
        System.Console.WriteLine("DI CHECK: IProgressReportRepository NOT registered");
    else
        System.Console.WriteLine("DI CHECK: IProgressReportRepository registered");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
// QUAN TRỌNG: Thứ tự phải là Authentication trước, Authorization sau
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
