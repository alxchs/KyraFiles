// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace KyraFiles.App.Services.Git
{
	public enum DeviceCodeStatus
	{
		Success,
		MissingClientId,
		NetworkError,
		ServerError
	}

	public enum TokenPollStatus
	{
		Success,
		Pending,
		SlowDown,
		Expired,
		AccessDenied,
		Cancelled,
		Error
	}

	public sealed class DeviceCodeResult
	{
		public DeviceCodeStatus Status { get; init; }
		public string UserCode { get; init; } = string.Empty;
		public string DeviceCode { get; init; } = string.Empty;
		public string VerificationUri { get; init; } = "https://github.com/login/device";
		public int IntervalSeconds { get; init; } = 5;
		public int ExpiresInSeconds { get; init; } = 900;
		public string? ErrorMessage { get; init; }

		public static DeviceCodeResult MissingConfiguration(string? message = null) => new()
		{
			Status = DeviceCodeStatus.MissingClientId,
			ErrorMessage = message ?? "GitHub OAuth Client ID is not configured."
		};
	}

	/// <summary>
	/// Service contract for GitHub OAuth device flow authentication and credential management.
	/// </summary>
	public interface IGitHubAuthService
	{
		/// <summary>
		/// Checks whether a valid OAuth Client ID is configured for the application.
		/// </summary>
		bool IsClientIdConfigured();

		/// <summary>
		/// Gets the currently configured OAuth Client ID (from environment variables, settings or constants).
		/// </summary>
		string? GetConfiguredClientId();

		/// <summary>
		/// Requests a new device authorization code from GitHub.
		/// </summary>
		Task<DeviceCodeResult> RequestDeviceCodeAsync(HttpClient? httpClient = null, CancellationToken cancellationToken = default);

		/// <summary>
		/// Polls GitHub for the user's access token after device authorization.
		/// </summary>
		Task<(TokenPollStatus Status, string? AccessToken)> PollForAccessTokenAsync(
			string deviceCode,
			int intervalSeconds,
			int expiresInSeconds,
			HttpClient? httpClient = null,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Saves the access token securely to the system credential vault.
		/// </summary>
		bool SaveAccessToken(string token);

		/// <summary>
		/// Removes the saved access token from the system credential vault.
		/// </summary>
		bool RemoveAccessToken();

		/// <summary>
		/// Retrieves the saved access token if present.
		/// </summary>
		string? GetSavedAccessToken();

		/// <summary>
		/// Checks if a valid saved access token exists.
		/// </summary>
		bool IsAuthenticated();
	}
}

