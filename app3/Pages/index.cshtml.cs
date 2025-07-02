// App1/Pages/Index.cshtml.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http; // Required for HttpContext.Session
using Microsoft.Extensions.Caching.Distributed; // Required for IDistributedCache
using System;
using System.Text.Json; // For JSON serialization
using System.Collections.Generic; // For Dictionary
using System.Threading.Tasks; // For Task
using Microsoft.Extensions.Logging; // ADDED: For logging

namespace App1.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<IndexModel> _logger; // ADDED: Logger instance

        // Inject IDistributedCache and ILogger
        public IndexModel(IDistributedCache cache, ILogger<IndexModel> logger) // MODIFIED: Added ILogger
        {
            _cache = cache;
            _logger = logger; // ADDED: Assign logger
        }

        // Bind properties from the form
        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        // Message to display on the page (e.g., login status)
        public string Message { get; set; } = string.Empty;

        // Property to hold the session ID that will be passed to the frontend
        public string CurrentSessionId { get; set; } = string.Empty;

        public void OnGet()
        {
            // On initial GET request, ensure a session exists and get its ID.
            // HttpContext.Session.Id will create a new session if one doesn't exist.
            CurrentSessionId = HttpContext.Session.Id;
            Message = "Please log in to simulate authentication and send session data to App2.";
            _logger.LogInformation($"App1 OnGet: Current Session ID: {CurrentSessionId}"); // ADDED: Log session ID on GET
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync()
        {
            // Hardcoded authentication for demonstration purposes
            if (Username == "testuser" && Password == "password123")
            {
                // Set internal session variables upon successful login
                HttpContext.Session.SetString("UserLoggedIn", "true");
                HttpContext.Session.SetString("Username", Username);
                HttpContext.Session.SetString("LoginTime", DateTime.UtcNow.ToString());

                // Retrieve the current session ID. This is the ID that ASP.NET Core
                // uses internally and stores in Redis.
                CurrentSessionId = HttpContext.Session.Id;
                _logger.LogInformation($"App1 OnPost: Login successful. Session ID: {CurrentSessionId}"); // ADDED: Log session ID on POST

                // --- Store shared data explicitly in Redis as JSON for App2 to read ---
                var sharedData = new Dictionary<string, string>
                {
                    { "Username", Username },
                    { "LoginTime", DateTime.UtcNow.ToString("o") }, // ISO 8601 format
                    { "SourceApp", "App1" }
                };
                string jsonSharedData = JsonSerializer.Serialize(sharedData);

                // Define a distinct Redis key for the shared data
                string sharedRedisKey = $"App1_SharedData_{CurrentSessionId}";

                // Set the shared data in Redis with an expiration that matches or exceeds session timeout
                await _cache.SetStringAsync(
                    sharedRedisKey,
                    jsonSharedData,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) // Match session timeout
                    });
                _logger.LogInformation($"App1 OnPost: Stored shared data in Redis. Key: '{sharedRedisKey}', Data: '{jsonSharedData}'"); // ADDED: Log Redis key and data
                // ----------------------------------------------------------------------

                Message = "Login successful! Session created and shared data stored in Redis. App2 will receive session ID.";
            }
            else
            {
                Message = "Login failed. Invalid username or password.";
                // If login fails, ensure CurrentSessionId is still available for the page
                CurrentSessionId = HttpContext.Session.Id;
                _logger.LogWarning($"App1 OnPost: Login failed for user '{Username}'. Current Session ID: {CurrentSessionId}"); // ADDED: Log failed login
            }

            // Return the current page to re-render with updated messages and potentially the iframe
            return Page();
        }
    }
}