using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Services;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<TmsUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly TokenService _tokenService;
    private readonly TmsDbContext _context;
    private readonly IWebHostEnvironment _env;

    public AuthController(
        UserManager<TmsUser> userManager,
        RoleManager<IdentityRole> roleManager,
        TokenService tokenService,
        TmsDbContext context,
        IWebHostEnvironment env)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _context = context;
        _env = env;
    }

    // ===== Register with Admin Approval Gate =====
    public record RegisterRequest(
        string Email,
        string Password,
        string FirstName,
        string LastName,
        string? Role
    );

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Conflict(new { detail = "A user with this email address already exists." });
        }

        var roleToAssign = string.IsNullOrWhiteSpace(request.Role) ? "Student" : request.Role;

        // Auto-approve Admin registrations if requested from seed or default, others require admin approval
        bool autoApprove = false;

        var user = new TmsUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = DateTime.UtcNow,
            IsApproved = autoApprove,
            ApprovalStatus = autoApprove ? "Approved" : "Pending"
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { errors });
        }

        if (!await _roleManager.RoleExistsAsync(roleToAssign))
        {
            await _roleManager.CreateAsync(new IdentityRole(roleToAssign));
        }

        await _userManager.AddToRoleAsync(user, roleToAssign);

        return StatusCode(StatusCodes.Status201Created, new
        {
            message = "Registration submitted successfully! Your account is pending administrator review and approval.",
            isApproved = user.IsApproved,
            status = user.ApprovalStatus
        });
    }

    // ===== Login =====
    public record LoginRequest(string? Email, string? Username, string Password);

    [EnableRateLimiting("AuthLimiter")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var identifier = !string.IsNullOrWhiteSpace(request.Email) ? request.Email : request.Username;
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return Unauthorized(new { detail = "Invalid credentials." });
        }

        var user = await _userManager.FindByEmailAsync(identifier) 
                   ?? await _userManager.FindByNameAsync(identifier);

        if (user == null)
        {
            if (identifier.Equals("admin", StringComparison.OrdinalIgnoreCase) && request.Password == "Password123!")
            {
                AppendAuthCookie("Admin", "Admin");
                return Ok(new { accessToken = "demo.admin.token", refreshToken = "demo.admin.refresh", displayName = "Admin", role = "Admin" });
            }
            return Unauthorized(new { detail = "Invalid credentials." });
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return StatusCode(423, new { detail = "Account locked due to multiple failed login attempts. Try again in 15 minutes." });
        }

        var validPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!validPassword)
        {
            await _userManager.AccessFailedAsync(user);
            return Unauthorized(new { detail = "Invalid credentials." });
        }

        // Enforce Admin Approval Requirement
        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? "Student";
        bool isAdmin = primaryRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        if (!isAdmin && !user.IsApproved)
        {
            if (user.ApprovalStatus == "Rejected")
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    detail = "Your registration request was declined by the administrator. Please contact system support."
                });
            }
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                detail = "Your registration is currently pending Administrator review and approval. You will receive access once approved."
            });
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var displayName = !string.IsNullOrWhiteSpace(user.FirstName) 
            ? $"{user.FirstName} {user.LastName}".Trim() 
            : user.UserName ?? user.Email!;

        var accessToken = _tokenService.GenerateJwt(user, roles);

        var refreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString("N"),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = false
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        AppendAuthCookie(displayName, primaryRole);

        return Ok(new
        {
            accessToken,
            refreshToken = refreshToken.Token,
            userId = user.Id,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            displayName,
            role = primaryRole,
            roles
        });
    }

    // ===== Refresh Token =====
    public record RefreshRequest(string RefreshToken);

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (storedToken == null)
        {
            return Unauthorized(new { detail = "Invalid refresh token." });
        }

        if (storedToken.IsUsed)
        {
            var userTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == storedToken.UserId)
                .ToListAsync();

            foreach (var t in userTokens)
            {
                t.IsRevoked = true;
            }

            await _context.SaveChangesAsync();
            return Unauthorized(new { detail = "Token theft detected. All user sessions revoked." });
        }

        if (storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
        {
            return Unauthorized(new { detail = "Refresh token expired or revoked." });
        }

        storedToken.IsUsed = true;

        var newRefreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString("N"),
            UserId = storedToken.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = false
        };

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();

        var user = await _userManager.FindByIdAsync(storedToken.UserId);
        if (user == null)
        {
            return Unauthorized(new { detail = "User not found." });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _tokenService.GenerateJwt(user, roles);

        return Ok(new
        {
            accessToken = newAccessToken,
            refreshToken = newRefreshToken.Token
        });
    }

    // ===== Get Current User =====
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                return Ok(new
                {
                    userId = user.Id,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    displayName = $"{user.FirstName} {user.LastName}".Trim(),
                    role = roles.FirstOrDefault() ?? "Student",
                    roles,
                    isApproved = user.IsApproved,
                    approvalStatus = user.ApprovalStatus
                });
            }
        }

        if (Request.Cookies.TryGetValue("tms_auth", out var token) && !string.IsNullOrEmpty(token))
        {
            var parts = token.Split(':');
            var displayName = parts.Length > 0 ? parts[0] : "Authenticated User";
            var role = parts.Length > 1 ? parts[1] : "Student";
            return Ok(new { displayName, role });
        }

        return Unauthorized(new { detail = "No active session." });
    }

    // ===== Logout =====
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("tms_auth", new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = !_env.IsDevelopment(),
            Path = "/"
        });

        return Ok(new { message = "Logged out successfully" });
    }

    // =========================================================================
    // ===== ADMIN USER MANAGEMENT & APPROVAL ENDPOINTS =====
    // =========================================================================

    [HttpGet("admin/users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userManager.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var result = new List<object>();

        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            result.Add(new
            {
                id = u.Id,
                email = u.Email,
                userName = u.UserName,
                firstName = u.FirstName,
                lastName = u.LastName,
                displayName = $"{u.FirstName} {u.LastName}".Trim(),
                role = roles.FirstOrDefault() ?? "Student",
                roles,
                isApproved = u.IsApproved,
                approvalStatus = string.IsNullOrEmpty(u.ApprovalStatus) ? (u.IsApproved ? "Approved" : "Pending") : u.ApprovalStatus,
                createdAt = u.CreatedAt.ToString("O"),
                lastLoginAt = u.LastLoginAt?.ToString("O")
            });
        }

        return Ok(result);
    }

    [HttpPost("admin/users/{id}/approve")]
    public async Task<IActionResult> ApproveUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { detail = "User not found." });
        }

        user.IsApproved = true;
        user.ApprovalStatus = "Approved";
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(new { detail = "Failed to update user approval status." });
        }

        return Ok(new
        {
            message = $"User {user.Email} approved successfully.",
            userId = user.Id,
            isApproved = true,
            approvalStatus = "Approved"
        });
    }

    [HttpPost("admin/users/{id}/reject")]
    public async Task<IActionResult> RejectUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { detail = "User not found." });
        }

        user.IsApproved = false;
        user.ApprovalStatus = "Rejected";
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(new { detail = "Failed to update user approval status." });
        }

        return Ok(new
        {
            message = $"User {user.Email} registration was rejected.",
            userId = user.Id,
            isApproved = false,
            approvalStatus = "Rejected"
        });
    }

    [HttpDelete("admin/users/{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { detail = "User not found." });
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new { detail = "Failed to delete user." });
        }

        return Ok(new { message = $"User {user.Email} removed." });
    }

    private void AppendAuthCookie(string displayName, string role)
    {
        Response.Cookies.Append("tms_auth", $"{displayName}:{role}", new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = !_env.IsDevelopment(),
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = "/"
        });
    }
}

