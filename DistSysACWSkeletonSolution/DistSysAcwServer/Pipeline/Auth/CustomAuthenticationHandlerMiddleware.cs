using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using DistSysAcwServer.Models;
using DistSysAcwServer.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DistSysAcwServer.Auth
{
    /// <summary>
    /// Custom authentication handler that validates clients by API Key.
    /// Runs on every HTTP request before the request reaches the controller.
    /// If an [Authorize] attribute is present on the action, authentication must succeed
    /// for the request to proceed.
    /// </summary>
    public class CustomAuthenticationHandlerMiddleware
        : AuthenticationHandler<AuthenticationSchemeOptions>, IAuthenticationHandler
    {
        private UserContext DbContext { get; set; }
        private IHttpContextAccessor HttpContextAccessor { get; set; }
        private SharedError Error { get; set; }

        public CustomAuthenticationHandlerMiddleware(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            UserContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            SharedError error)
            : base(options, logger, encoder)
        {
            DbContext = dbContext;
            HttpContextAccessor = httpContextAccessor;
            Error = error;
        }

        /// <summary>
        /// Authenticates the client by reading the 'ApiKey' header and validating it
        /// against the database. On success, creates a ClaimsPrincipal containing
        /// the user's Name and Role claims.
        /// </summary>
        /// <returns>
        /// AuthenticateResult.Success with a valid ticket if the API Key is found,
        /// or AuthenticateResult.Fail if it is not.
        /// </returns>
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Try to get the ApiKey header from the request
            if (!Request.Headers.TryGetValue("ApiKey", out var apiKeyValues))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            string? apiKey = apiKeyValues.FirstOrDefault();
            if (string.IsNullOrEmpty(apiKey))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            // Look up the user in the database using the loosely coupled data access class
            User? user = UserDatabaseAccess.GetUserByKey(apiKey, DbContext);
            if (user == null)
            {
                return Task.FromResult(AuthenticateResult.Fail("Unauthorized"));
            }

            // Build the claims for this authenticated user
            Claim[] claims = new[]
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.Role)
            };

            // Create a ClaimsIdentity with authentication type "ApiKey"
            ClaimsIdentity identity = new ClaimsIdentity(claims, "ApiKey");

            // Wrap the identity in a ClaimsPrincipal
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);

            // Create an AuthenticationTicket using the principal and scheme name
            AuthenticationTicket ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        /// <summary>
        /// Called when authentication fails and the request is challenged.
        /// Returns a 401 Unauthorized response with the spec-required message.
        /// </summary>
        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Error.StatusCode = StatusCodes.Status401Unauthorized;
            Error.Message = "Unauthorized. Check ApiKey in Header is correct.";
            return Task.CompletedTask;
        }
    }
}