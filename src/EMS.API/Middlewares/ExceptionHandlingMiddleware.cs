using EMS.Application.Common.Exceptions;
using System.Net;
using System.Text.Json;

namespace EMS.API.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
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
                _logger.LogError(ex,
                    "Đã xảy ra lỗi hệ thống: {Message}",
                    ex.Message);

                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(
            HttpContext context,
            Exception exception)
        {
            context.Response.ContentType = "application/json";

            int statusCode = (int)HttpStatusCode.InternalServerError;

            string message = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.";

            switch (exception)
            {
                case NotFoundException notFoundEx:
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = notFoundEx.Message;
                    break;

                case BadRequestException badRequestEx:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    message = badRequestEx.Message;
                    break;

                case UnauthorizedAccessException unAuthEx:
                    statusCode = (int)HttpStatusCode.Forbidden;
                    message = unAuthEx.Message;
                    break;

                case ForbiddenAccessException forbiddenEx:
                    statusCode = (int)HttpStatusCode.Forbidden;
                    message = forbiddenEx.Message;
                    break;

                case ConflictException conflictEx:
                    statusCode = (int)HttpStatusCode.Conflict;
                    message = conflictEx.Message;
                    break;

                case FluentValidation.ValidationException validationEx:
                    statusCode = (int)HttpStatusCode.BadRequest;

                    message = validationEx.Errors
                        .FirstOrDefault()?.ErrorMessage
                        ?? "Dữ liệu đầu vào không hợp lệ.";

                    break;

                default:
                    message = exception.Message;
                    break;
            }

            var result = JsonSerializer.Serialize(new
            {
                statusCode,
                message
            });

            context.Response.StatusCode = statusCode;

            return context.Response.WriteAsync(result);
        }
    }
}