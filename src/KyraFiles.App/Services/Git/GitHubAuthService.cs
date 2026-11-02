// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Files.App.Helpers;

namespace Files.App.Services.Git
{
	/// <summary>
	/// Implementation of GitHub OAuth Device Authorization Flow (RFC 8628).
	/// </summary>
	public sealed class GitHubAuthService : IGitHubAuthService
	{
		public const string GitResourceName = "Files:https://github.com";
		public const string GitResourceUsername = "Personal Access Token";
		public const string DefaultDeviceCodeEndpoint = "https://github.com/login/device/code";
		public const string DefaultAccessTokenEndpoint = "https://github.com/login/oauth/access_token";
		public const string SecretPlaceholder = "githubclientid.secret";

		private readonly ICredentialsVault _vault;
		private readonly string? _overrideClientId;

		public GitHubAuthService(ICredentialsVault? vault = null, string? overrideClientId = null)
		{
			_vault = vault ?? new DefaultCredentialsVault();
			_overrideClientId = overrideClientId;
		}

		/// <inheritdoc />
		public bool IsClientIdConfigured()
		{
			return !string.IsNullOrWhiteSpace(GetConfiguredClientId());
		}

		/// <inheritdoc />
		public string? GetConfiguredClientId()
		{
			if (!string.IsNullOrWhiteSpace(_overrideClientId))
			{
				var cleanOverride = _overrideClientId.Trim();
				if (!string.Equals(cleanOverride, SecretPlaceholder, StringComparison.OrdinalIgnoreCase))
					return cleanOverride;
			}

			// 1. Environment variable override
			var envClientId = Environment.GetEnvironmentVariable("KYRAFILES_GITHUB_CLIENT_ID");
			if (!string.IsNullOrWhiteSpace(envClientId))
			{
				var clean = envClientId.Trim();
				if (!string.Equals(clean, SecretPlaceholder, StringComparison.OrdinalIgnoreCase))
					return clean;
			}

			var fallbackEnv = Environment.GetEnvironmentVariable("FILES_GITHUB_CLIENT_ID");
			if (!string.IsNullOrWhiteSpace(fallbackEnv))
			{
				var clean = fallbackEnv.Trim();
				if (!string.Equals(clean, SecretPlaceholder, StringComparison.OrdinalIgnoreCase))
					return clean;
			}

			// 2. Constants fallback
			var constantClientId = Constants.AutomatedWorkflowInjectionKeys.GitHubClientId;
			if (!string.IsNullOrWhiteSpace(constantClientId))
			{
				var clean = constantClientId.Trim();
				if (!string.Equals(clean, SecretPlaceholder, StringComparison.OrdinalIgnoreCase))
					return clean;
			}

			return null;
		}

		/// <inheritdoc />
		public async Task<DeviceCodeResult> RequestDeviceCodeAsync(HttpClient? httpClient = null, CancellationToken cancellationToken = default)
		{
			var clientId = GetConfiguredClientId();
			if (string.IsNullOrWhiteSpace(clientId))
			{
				return DeviceCodeResult.MissingConfiguration(
					"O identificador de aplicativo (Client ID) do GitHub não está configurado. Configure a variável de ambiente KYRAFILES_GITHUB_CLIENT_ID com o Client ID do seu OAuth App no GitHub.");
			}

			var shouldDisposeClient = false;
			var client = httpClient;
			if (client is null)
			{
				client = new HttpClient();
				client.DefaultRequestHeaders.Accept.Clear();
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
				client.DefaultRequestHeaders.UserAgent.ParseAdd("FilesApp/4.3");
				shouldDisposeClient = true;
			}

			try
			{
				var content = new FormUrlEncodedContent(new[]
				{
					new KeyValuePair<string, string>("client_id", clientId),
					new KeyValuePair<string, string>("scope", "repo")
				});

				var response = await client.PostAsync(DefaultDeviceCodeEndpoint, content, cancellationToken);
				if (!response.IsSuccessStatusCode)
				{
					return new DeviceCodeResult
					{
						Status = DeviceCodeStatus.ServerError,
						ErrorMessage = $"Falha ao solicitar código de dispositivo: HTTP {(int)response.StatusCode} {response.ReasonPhrase}"
					};
				}

				await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
				using var jsonDoc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
				var root = jsonDoc.RootElement;

				var userCode = root.TryGetProperty("user_code", out var uc) ? uc.GetString() ?? string.Empty : string.Empty;
				var deviceCode = root.TryGetProperty("device_code", out var dc) ? dc.GetString() ?? string.Empty : string.Empty;
				var uri = root.TryGetProperty("verification_uri", out var vu) ? vu.GetString() ?? "https://github.com/login/device" : "https://github.com/login/device";
				var interval = root.TryGetProperty("interval", out var iv) ? iv.GetInt32() : 5;
				var expiresIn = root.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 900;

				return new DeviceCodeResult
				{
					Status = DeviceCodeStatus.Success,
					UserCode = userCode,
					DeviceCode = deviceCode,
					VerificationUri = uri,
					IntervalSeconds = Math.Max(5, interval),
					ExpiresInSeconds = expiresIn
				};
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				return new DeviceCodeResult
				{
					Status = DeviceCodeStatus.NetworkError,
					ErrorMessage = ex.Message
				};
			}
			finally
			{
				if (shouldDisposeClient)
					client.Dispose();
			}
		}

		/// <inheritdoc />
		public async Task<(TokenPollStatus Status, string? AccessToken)> PollForAccessTokenAsync(
			string deviceCode,
			int intervalSeconds,
			int expiresInSeconds,
			HttpClient? httpClient = null,
			CancellationToken cancellationToken = default)
		{
			var clientId = GetConfiguredClientId();
			if (string.IsNullOrWhiteSpace(clientId))
				return (TokenPollStatus.Error, null);

			var shouldDisposeClient = false;
			var client = httpClient;
			if (client is null)
			{
				client = new HttpClient();
				client.DefaultRequestHeaders.Accept.Clear();
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
				client.DefaultRequestHeaders.UserAgent.ParseAdd("FilesApp/4.3");
				shouldDisposeClient = true;
			}

			var interval = Math.Max(5, intervalSeconds);
			var remainingTime = expiresInSeconds;

			try
			{
				while (!cancellationToken.IsCancellationRequested && remainingTime > 0)
				{
					var content = new FormUrlEncodedContent(new[]
					{
						new KeyValuePair<string, string>("client_id", clientId),
						new KeyValuePair<string, string>("device_code", deviceCode),
						new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:device_code")
					});

					HttpResponseMessage response;
					try
					{
						response = await client.PostAsync(DefaultAccessTokenEndpoint, content, cancellationToken);
					}
					catch (OperationCanceledException)
					{
						return (TokenPollStatus.Cancelled, null);
					}

					remainingTime -= interval;

					if (!response.IsSuccessStatusCode)
					{
						return (TokenPollStatus.Error, null);
					}

					await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
					using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
					var root = doc.RootElement;

					if (root.TryGetProperty("error", out var errorProp))
					{
						var err = errorProp.GetString();
						if (string.Equals(err, "authorization_pending", StringComparison.OrdinalIgnoreCase))
						{
							await Task.Delay(TimeSpan.FromSeconds(interval), cancellationToken);
							continue;
						}

						if (string.Equals(err, "slow_down", StringComparison.OrdinalIgnoreCase))
						{
							interval += 5;
							await Task.Delay(TimeSpan.FromSeconds(interval), cancellationToken);
							continue;
						}

						if (string.Equals(err, "expired_token", StringComparison.OrdinalIgnoreCase))
						{
							return (TokenPollStatus.Expired, null);
						}

						if (string.Equals(err, "access_denied", StringComparison.OrdinalIgnoreCase))
						{
							return (TokenPollStatus.AccessDenied, null);
						}

						return (TokenPollStatus.Error, null);
					}

					if (root.TryGetProperty("access_token", out var tokenProp))
					{
						var token = tokenProp.GetString();
						if (!string.IsNullOrWhiteSpace(token))
						{
							SaveAccessToken(token);
							return (TokenPollStatus.Success, token);
						}
					}

					await Task.Delay(TimeSpan.FromSeconds(interval), cancellationToken);
				}

				if (cancellationToken.IsCancellationRequested)
					return (TokenPollStatus.Cancelled, null);

				return (TokenPollStatus.Expired, null);
			}
			catch (OperationCanceledException)
			{
				return (TokenPollStatus.Cancelled, null);
			}
			catch
			{
				return (TokenPollStatus.Error, null);
			}
			finally
			{
				if (shouldDisposeClient)
					client.Dispose();
			}
		}

		/// <inheritdoc />
		public bool SaveAccessToken(string token)
		{
			if (string.IsNullOrWhiteSpace(token))
				return false;

			try
			{
				_vault.SavePassword(GitResourceName, GitResourceUsername, token);
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <inheritdoc />
		public bool RemoveAccessToken()
		{
			try
			{
				_vault.DeleteSavedPassword(GitResourceName, GitResourceUsername);
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <inheritdoc />
		public string? GetSavedAccessToken()
		{
			try
			{
				var token = _vault.GetPassword(GitResourceName, GitResourceUsername);
				return string.IsNullOrWhiteSpace(token) ? null : token;
			}
			catch
			{
				return null;
			}
		}

		/// <inheritdoc />
		public bool IsAuthenticated()
		{
			return !string.IsNullOrWhiteSpace(GetSavedAccessToken());
		}
	}
}

