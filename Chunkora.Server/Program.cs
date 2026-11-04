using Chunkora.Server;
using Chunkora.Server.Agents.Authentication;
using Chunkora.Server.Agents.Managers;
using Chunkora.Server.Authentication;
using Chunkora.Server.Models;
using Chunkora.Server.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

DotNetEnv.Env.NoClobber().TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContextFactory<GlobalDatabaseContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL"));

    options.UseSeeding((context, _) =>
    {
        var roles = context.Set<Role>();
        var users = context.Set<User>();

        var targetRole = roles.FirstOrDefault(x => x.Name == "admin");
        if (!users.Any())
        {
            Guid roleId;
            if (targetRole is null)
            {
                var role = roles.Add(new Role
                {
                    Name = "admin",
                    Description = "Chunkora Administrator Role",
                    Permissions = ["*"],
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                context.SaveChanges();

                roleId = role.Entity.Id;
            }
            else
            {
                roleId = targetRole.Id;
            }
            
            var initialPassword = builder.Configuration["BootstrapAdmin:Password"];

            if (string.IsNullOrWhiteSpace(initialPassword))
            {
                throw new InvalidOperationException(
                    "BootstrapAdmin__Password is required when configuring the admin account for the first time");
            }

            users.Add(new User
            {
                RoleId = roleId,
                Name = "admin",
                Description = "Chunkora Administrator",
                HashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(initialPassword),
                AccessTokenExpiresAt = DateTime.UtcNow.AddHours(2),
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            context.SaveChanges();
        }
    });
});
builder.Services
    .AddAuthorizationBuilder()
    .SetFallbackPolicy(
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build()
    );
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(AccessTokenAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, AccessTokenAuthenticationHandler>(AccessTokenAuthenticationHandler.SchemeName, _ => {})
    .AddScheme<AuthenticationSchemeOptions, AgentTokenAuthenticationHandler>(AgentTokenAuthenticationHandler.SchemeName, _ => {});
builder.Services
    .AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder(AccessTokenAuthenticationHandler.SchemeName).RequireAuthenticatedUser().Build())
    .AddPolicy("AgentOnly", policy =>
    {
        policy.AddAuthenticationSchemes(AgentTokenAuthenticationHandler.SchemeName);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("principal_type", "agent");
    });
builder.Services.AddSingleton<InstanceConnectionManager>();
builder.Services.AddSingleton<InstanceOperationConnectionManager>();
builder.Services.AddSingleton<InstanceIoConnectionManager>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<AccessTokenValidator>();
builder.Services.AddScoped<AgentTokenValidator>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<NodeService>();
builder.Services.AddScoped<NodeMetricsService>();
builder.Services.AddScoped<InstanceService>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.UseExceptionHandler();

app.MapControllers();

app.MapGrpcService<Chunkora.Server.Agents.Services.StatusService>();
app.MapGrpcService<Chunkora.Server.Agents.Services.InstanceService>();
app.MapGrpcService<Chunkora.Server.Agents.Services.InstanceOperationService>();
app.MapGrpcService<Chunkora.Server.Agents.Services.InstanceIoService>();

app.Run();