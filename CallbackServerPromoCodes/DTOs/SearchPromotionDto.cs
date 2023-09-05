namespace CallbackServerPromoCodes.DTOs;

public record SearchPromotionDto(string product, string? code, string? url, string video, string channel)
{
}