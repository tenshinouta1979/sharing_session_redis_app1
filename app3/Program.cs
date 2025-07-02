// App1/Program.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Configure Redis Distributed Cache
// This uses StackExchange.Redis client for connecting to Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    // Get Redis connection string from appsettings.json
    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
    // Optional: Prefix for keys in Redis to avoid conflicts if multiple apps use the same Redis instance
    options.InstanceName = "App1_Session_";
});

// Configure Session Middleware
builder.Services.AddSession(options =>
{
    // Set session timeout (e.g., 30 minutes of inactivity)
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    // Make the session cookie inaccessible to client-side script for security
    options.Cookie.HttpOnly = true;
    // Mark the session cookie as essential for GDPR compliance
    options.Cookie.IsEssential = true;
    // Set SameSite policy for the session cookie.
    // SameSiteMode.Lax is generally good for default, but for cross-domain iframes
    // where cookies might be needed, SameSiteMode.None might be considered,
    // but it *requires* SecurePolicy.Always (HTTPS).
    options.Cookie.SameSite = SameSiteMode.Lax;
    // Always enforce secure cookies (HTTPS) in production for security
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    // Set a distinct name for App1's session cookie
    options.Cookie.Name = ".App1.Session";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Redirect HTTP requests to HTTPS
app.UseHttpsRedirection();
// Serve static files (HTML, CSS, JS) from wwwroot
app.UseStaticFiles();

// Enable routing for MVC/Razor Pages
app.UseRouting();

// Enable session middleware. This must be placed before UseAuthorization/UseEndpoints
// so that session data is available for controllers/pages.
app.UseSession();

// Enable authorization middleware
app.UseAuthorization();

// Map Razor Pages to routes
app.MapRazorPages();

app.Run();