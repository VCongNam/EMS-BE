using EMS.Application.Common.Exceptions;
using System.Net;
using System.Text.Json;

namespace EMS.API.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Đã xảy ra lỗi hệ thống: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            int statusCode = (int)HttpStatusCode.InternalServerError; // Mặc định 500
            string message = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.";
            object? errors = null;

            switch (exception)
            {
                case NotFoundException notFoundEx:
                    statusCode = (int)HttpStatusCode.NotFound; // 404
                    message = notFoundEx.Message;
                    break;

                case BadRequestException badRequestEx:
                    statusCode = (int)HttpStatusCode.BadRequest; // 400
                    message = badRequestEx.Message;
                    break;

                case UnauthorizedAccessException unAuthEx:
                    statusCode = (int)HttpStatusCode.Forbidden; // 403 (Hoặc 401 tùy logic)
                    message = unAuthEx.Message;
                    break;

                case FluentValidation.ValidationException validationEx:
                    statusCode = (int)HttpStatusCode.BadRequest; // 400
                    message = "Dữ liệu đầu vào không hợp lệ.";
                    // Gom các lỗi của FluentValidation lại
                    errors = validationEx.Errors.Select(e => new { e.PropertyName, e.ErrorMessage });
                    break;
                // THÊM MỚI: Xử lý Forbidden
                case ForbiddenAccessException forbiddenEx:
                    statusCode = (int)HttpStatusCode.Forbidden; // 403
                    message = forbiddenEx.Message;
                    break;
                // THÊM MỚI: Xử lý Conflict
                case ConflictException conflictEx:
                    statusCode = (int)HttpStatusCode.Conflict; // 409
                    message = conflictEx.Message;
                    break;
                default:
                    // Lỗi 500 (Lỗi code, sập DB...) thì giữ nguyên message mặc định để giấu lỗi thật
                    // Nếu đang ở môi trường Development, có thể in lỗi thật ra để dễ debug:
                    message = exception.Message; // Bật dòng này khi đang code, khi đem đi chấm đồ án thì tắt đi
                    break;
            }

            // 2. Format lại cục JSON trả về cho Frontend
            var result = JsonSerializer.Serialize(new
            {
                StatusCode = statusCode,
                Message = message,
                Errors = errors
            });

            context.Response.StatusCode = statusCode;
            return context.Response.WriteAsync(result);
        }
    }
}
