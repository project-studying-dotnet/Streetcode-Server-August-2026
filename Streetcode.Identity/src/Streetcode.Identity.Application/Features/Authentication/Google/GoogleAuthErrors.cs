using FluentResults;

namespace Streetcode.Identity.Application.Features.Authentication.Google;

public static class GoogleAuthErrors
{
    public static Error InvalidToken() => Error("Google.InvalidToken", "The Google ID token is invalid.");
    public static Error Unavailable() => Error("Google.Unavailable", "Google authentication is temporarily unavailable.");
    public static Error LinkRequired() => Error("Google.LinkRequired", "Sign in to your existing account and explicitly link Google.");
    public static Error Conflict() => Error("Google.LinkConflict", "The Google account cannot be linked to this account.");
    public static Error Unauthorized() => Error("Google.Unauthorized", "The account or credentials are invalid.");
    public static Error Error(string code, string message) => new Error(message).WithMetadata("Code", code);
}
