using DistSysAcwServer.Middleware;
using DistSysAcwServer.Pipeline;
using DistSysAcwServer.Services;
using DistSysAcwServer.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddScoped<SharedError>();
builder.Services.AddTransient<DistSysAcwServer.Pipeline.ErrorHandlingMiddleware>();

builder.Services.AddControllers(options =>
{
    options.AllowEmptyInputInBodyModelBinding = true;
    options.Filters.Add<ActionErrorHandlingFilter>();
});
builder.Services.AddDbContext<DistSysAcwServer.Models.UserContext>();
builder.Services.AddHttpContextAccessor();

// Register the RSA key service as a singleton so the same key pair
// is shared across all threads and requests for the lifetime of the server.
builder.Services.AddSingleton<RsaKeyService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "CustomAuthentication";
}).AddScheme<AuthenticationSchemeOptions, DistSysAcwServer.Auth.CustomAuthenticationHandlerMiddleware>
    ("CustomAuthentication", options => { });

builder.Services.AddTransient<IAuthorizationHandler, DistSysAcwServer.Auth.CustomAuthorizationHandlerMiddleware>();
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

//app.UseHttpsRedirection(); // We aren't using HTTPS redirection for this project to avoid certificate issues on lab pcs

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();