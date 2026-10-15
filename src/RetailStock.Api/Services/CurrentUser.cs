using Microsoft.IdentityModel.JsonWebTokens;
using RetailStock.Application.Common.Interfaces;
using RetailStock.Infrastructure.Authentication;

namespace RetailStock.Api.Services;

/// <summary>
/// Implementación de ICurrentUser que lee los claims del JWT ya validado (HttpContext.User).
/// Vive en Api porque depende de HttpContext; Application solo conoce la interfaz.
/// Equivalente en Spring: leer SecurityContextHolder / @AuthenticationPrincipal Jwt jwt.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    private System.Security.Claims.ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public int? UserId => int.TryParse(Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : null;

    public string? Email => Principal?.FindFirst(JwtRegisteredClaimNames.Email)?.Value;

    public string? Role => Principal?.FindFirst(JwtTokenService.RoleClaim)?.Value;
}
