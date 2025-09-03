using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UndercutF1.Data;

public sealed class Formula1Account(
    IOptions<LiveTimingOptions> options,
    ILogger<Formula1Account> logger
)
{
    public string? AccessToken =>
        IsAuthenticated(out _) == AuthenticationResult.Success
            ? options.Value.Formula1AccessToken
            : null;

    public TokenPayload? Payload =>
        IsAuthenticated(out var payload) == AuthenticationResult.Success ? payload : null;

    public AuthenticationResult IsAuthenticated(out TokenPayload? payload) =>
        IsAuthenticated(options.Value.Formula1AccessToken, out payload);

    public AuthenticationResult IsAuthenticated(string? token, out TokenPayload? payload)
    {
        payload = null;
        if (string.IsNullOrWhiteSpace(token))
            return AuthenticationResult.NoToken;

        try
        {
            payload = GetTokenPayloadFromToken(token);
            if (payload is null)
                return AuthenticationResult.InvalidToken;

            if (payload.SubscriptionStatus != "active")
            {
                logger.LogError(
                    "Formula 1 Access Token does not have an active subscription status (Value: {Status}). If you've recently subscribed, you may want to try logging in again. This token cannot be used for authenticated with the live timing feed. An F1 TV subscription is required for the additional live timing feeds, but no login is required for normal functionality.",
                    payload.SubscriptionStatus
                );
                return AuthenticationResult.InvalidSubscriptionStatus;
            }

            if (payload.Expiry < DateTimeOffset.UtcNow)
            {
                logger.LogError(
                    "Formula 1 Access Token expired on {Date}, please login again.",
                    payload.Expiry
                );
                return AuthenticationResult.ExpiredToken;
            }

            return AuthenticationResult.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read data in access token");
            return AuthenticationResult.InvalidToken;
        }
    }

    private TokenPayload? GetTokenPayloadFromToken(string token)
    {
        var tokenPart = token.Split('.')[1];

        // For some reason, the base64 encoded string sometimes doesn't have enough padding chars at the end
        // Base64 strings should be a multiple of 4
        var missingPaddingChars = tokenPart.Length % 4;
        if (missingPaddingChars > 0)
        {
            logger.LogDebug("Adding {Count} extra padding chars", missingPaddingChars);
            tokenPart += new string('=', 4 - missingPaddingChars);
        }

        var tokenPayload = JsonSerializer.Deserialize<TokenPayload>(
            Convert.FromBase64String(tokenPart),
            JsonSerializerOptions.Web
        );
        logger.LogDebug("F1 TV Token Details: {Token}", tokenPayload);
        return tokenPayload;
    }

    public sealed record TokenPayload(
        string SubscriptionStatus,
        string? SubscribedProduct,
        int Exp,
        int Iat
    )
    {
        public DateTimeOffset Expiry => DateTimeOffset.FromUnixTimeSeconds(Exp);

        public DateTimeOffset IssuedAt => DateTimeOffset.FromUnixTimeSeconds(Iat);
    }

    public enum AuthenticationResult
    {
        Success,
        NoToken,
        InvalidToken,
        InvalidSubscriptionStatus,
        ExpiredToken,
    }
}
