namespace TmsApi.Application.DTOs;

public record LoginRequest(string Username, string Password);

public record UserProfileDto(string DisplayName, string Role);
