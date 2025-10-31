using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Diagnostics;

namespace BlogSystem.Web.Middleware
{
    /// <summary>
    /// 全域錯誤處理中介軟體
    /// 捕獲並記錄未處理的例外，並回傳適當的錯誤頁面
    /// </summary>
    public class GlobalErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalErrorHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public GlobalErrorHandlingMiddleware(
            RequestDelegate next,
            ILogger<GlobalErrorHandlingMiddleware> logger,
            IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
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
                    "未處理的例外發生。路徑: {Path}, 方法: {Method}, IP: {IP}",
                    context.Request.Path,
                    context.Request.Method,
                    context.Connection.RemoteIpAddress);

                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            // 取得或建立 Activity ID 作為 Request ID
            var requestId = Activity.Current?.Id ?? context.TraceIdentifier;

            // 記錄錯誤詳情
            _logger.LogError(exception,
                "Request ID: {RequestId}, Error: {ErrorMessage}",
                requestId,
                exception.Message);

            // 清除已設定的 Response
            context.Response.Clear();

            // 在開發環境顯示詳細錯誤
            if (_environment.IsDevelopment())
            {
                // 使用 ASP.NET Core 內建的開發者例外頁面
                var exceptionHandlerFeature = new ExceptionHandlerFeature
                {
                    Error = exception,
                    Path = context.Request.Path.Value
                };
                context.Features.Set<IExceptionHandlerFeature>(exceptionHandlerFeature);
                context.Features.Set<IExceptionHandlerPathFeature>(exceptionHandlerFeature);
            }

            // 重新導向到錯誤頁面
            context.Request.Path = "/Home/Error";
            context.Items["RequestId"] = requestId;

            await _next(context);
        }
    }

    /// <summary>
    /// 中介軟體擴充方法
    /// </summary>
    public static class GlobalErrorHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalErrorHandling(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalErrorHandlingMiddleware>();
        }
    }
}
