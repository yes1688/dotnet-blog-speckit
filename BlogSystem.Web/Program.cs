using BlogSystem.Core.Entities;
using BlogSystem.Core.Interfaces;
using BlogSystem.Core.Services;
using BlogSystem.Infrastructure.Data;
using BlogSystem.Infrastructure.Repositories;
using Markdig;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);

// 配置 PostgreSQL 資料庫連線
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<BlogDbContext>(options =>
    options.UseNpgsql(connectionString));

// 註冊 Repository 服務
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IRepository<BlogPost>, BlogPostRepository>();

// T045: 註冊 BlogPostService
builder.Services.AddScoped<IBlogPostService, BlogPostService>();

// T061: 註冊 AuthService
builder.Services.AddScoped<IAuthService>(sp =>
{
    var repository = sp.GetRequiredService<IRepository<AdminLog>>();
    var adminEmails = builder.Configuration["AdminEmails"] ?? string.Empty;
    return new AuthService(repository, adminEmails);
});

// T079: 註冊 ImageService
builder.Services.AddScoped<IImageService, ImageService>();

// T098: 註冊 Category/Tag Services
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ITagService, TagService>();

// T108: 註冊 SearchService
builder.Services.AddScoped<ISearchService, SearchService>();

// T052: 配置 Cookie Authentication (有效期 7 天)
var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Cookies";  // 使用 Cookies 作為預設 Challenge
})
.AddCookie("Cookies", options =>
{
    options.LoginPath = "/Admin/Auth/Login";
    options.AccessDeniedPath = "/Admin/Auth/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

// T051: 配置 Google OAuth 2.0 (可選)
var googleClientId = builder.Configuration["Google:ClientId"];
var googleClientSecret = builder.Configuration["Google:ClientSecret"];
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    authBuilder.AddGoogle("Google", options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/Admin/Auth/GoogleCallback";
    });
}

// T053: 建立自訂 Authorization Policy "AdminOnly"
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            var email = context.User.Identity?.Name;
            if (string.IsNullOrEmpty(email))
                return false;

            // 檢查是否為管理員 Email
            var adminEmails = builder.Configuration["AdminEmails"];
            if (string.IsNullOrWhiteSpace(adminEmails))
                return false;

            var adminEmailList = adminEmails.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrWhiteSpace(e));

            return adminEmailList.Any(adminEmail =>
                adminEmail.Equals(email, StringComparison.OrdinalIgnoreCase));
        });
    });
});

// T042: 配置 Markdig Markdown 解析器
var markdownPipeline = new MarkdownPipelineBuilder()
    .UseAdvancedExtensions() // 啟用進階功能 (表格、任務列表等)
    .UseSoftlineBreakAsHardlineBreak() // 軟換行視為硬換行
    .Build();
builder.Services.AddSingleton(markdownPipeline);

// T043: 配置 URL 編碼支援中文字元
builder.Services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.All));

// T077: 配置檔案上傳限制 (5MB)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 5 * 1024 * 1024; // 5 MB
});

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 啟用身份驗證 (必須在 UseAuthorization 之前)
app.UseAuthentication();
app.UseAuthorization();

// T043: 配置支援中文 URL slug 的路由

// Admin Area 路由 (必須在其他路由之前)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// 文章詳細頁路由: /{year}/{month}/{slug}
app.MapControllerRoute(
    name: "post",
    pattern: "{year:int}/{month:int}/{slug}",
    defaults: new { controller = "Post", action = "Details" });

// 分類頁路由: /Category/{slug}
app.MapControllerRoute(
    name: "category",
    pattern: "Category/{id}",
    defaults: new { controller = "Category", action = "Index" });

// 標籤頁路由: /Tag/{slug}
app.MapControllerRoute(
    name: "tag",
    pattern: "Tag/{id}",
    defaults: new { controller = "Tag", action = "Index" });

// 預設路由
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// 公開 Program 類別以便整合測試使用
public partial class Program { }
