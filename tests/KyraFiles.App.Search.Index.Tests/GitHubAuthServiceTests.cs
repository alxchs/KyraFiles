// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KyraFiles.App.Helpers;
using KyraFiles.App.Services.Git;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests
{
	[TestClass]
	public class GitHubAuthServiceTests
	{
		private sealed class InMemoryCredentialsVault : ICredentialsVault
		{
			private readonly Dictionary<string, string> _store = new(StringComparer.OrdinalIgnoreCase);

			public void SavePassword(string resourceName, string username, string password)
			{
				_store[$"{resourceName}:{username}"] = password;
			}

			public void DeleteSavedPassword(string resourceName, string username)
			{
				_store.Remove($"{resourceName}:{username}");
			}

			public string GetPassword(string resourceName, string username)
			{
				return _store.TryGetValue($"{resourceName}:{username}", out var pass) ? pass : string.Empty;
			}
		}

		private sealed class MockHttpMessageHandler : HttpMessageHandler
		{
			private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

			public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
			{
				_handler = handler;
			}

			protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				return Task.FromResult(_handler(request));
			}
		}

		[TestMethod]
		public void IsClientIdConfigured_DetectsMissingAndSecretPlaceholder()
		{
			var vault = new InMemoryCredentialsVault();

			// 1. Secret placeholder returns false
			var serviceSecret = new GitHubAuthService(vault, overrideClientId: "githubclientid.secret");
			Assert.IsFalse(serviceSecret.IsClientIdConfigured());
			Assert.IsNull(serviceSecret.GetConfiguredClientId());

			// 2. Empty string returns false
			var serviceEmpty = new GitHubAuthService(vault, overrideClientId: "   ");
			Assert.IsFalse(serviceEmpty.IsClientIdConfigured());
			Assert.IsNull(serviceEmpty.GetConfiguredClientId());

			// 3. Valid client id returns true
			var serviceValid = new GitHubAuthService(vault, overrideClientId: "Ov23liCustomApp123");
			Assert.IsTrue(serviceValid.IsClientIdConfigured());
			Assert.AreEqual("Ov23liCustomApp123", serviceValid.GetConfiguredClientId());
		}

		[TestMethod]
		public async Task RequestDeviceCodeAsync_WhenClientIdNotConfigured_ReturnsMissingClientIdWithoutHttp()
		{
			var vault = new InMemoryCredentialsVault();
			var service = new GitHubAuthService(vault, overrideClientId: "githubclientid.secret");

			var result = await service.RequestDeviceCodeAsync();

			Assert.AreEqual(DeviceCodeStatus.MissingClientId, result.Status);
			Assert.IsNotNull(result.ErrorMessage);
			StringAssert.Contains(result.ErrorMessage, "KYRAFILES_GITHUB_CLIENT_ID");
		}

		[TestMethod]
		public async Task RequestDeviceCodeAsync_WhenHttpSucceeds_ParsesResultCorrectly()
		{
			var vault = new InMemoryCredentialsVault();
			var service = new GitHubAuthService(vault, overrideClientId: "TestClientId123");

			var jsonResponse = @"{
				""device_code"": ""3584d83530557fdd1f46af82897f"",
				""user_code"": ""WDJB-MJHT"",
				""verification_uri"": ""https://github.com/login/device"",
				""expires_in"": 900,
				""interval"": 5
			}";

			using var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
			});
			using var client = new HttpClient(handler);

			var result = await service.RequestDeviceCodeAsync(client);

			Assert.AreEqual(DeviceCodeStatus.Success, result.Status);
			Assert.AreEqual("WDJB-MJHT", result.UserCode);
			Assert.AreEqual("3584d83530557fdd1f46af82897f", result.DeviceCode);
			Assert.AreEqual("https://github.com/login/device", result.VerificationUri);
			Assert.AreEqual(5, result.IntervalSeconds);
			Assert.AreEqual(900, result.ExpiresInSeconds);
		}

		[TestMethod]
		public async Task RequestDeviceCodeAsync_WhenHttpFails_ReturnsServerError()
		{
			var vault = new InMemoryCredentialsVault();
			var service = new GitHubAuthService(vault, overrideClientId: "TestClientId123");

			using var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.NotFound)
			{
				ReasonPhrase = "Not Found"
			});
			using var client = new HttpClient(handler);

			var result = await service.RequestDeviceCodeAsync(client);

			Assert.AreEqual(DeviceCodeStatus.ServerError, result.Status);
			StringAssert.Contains(result.ErrorMessage, "404");
		}

		[TestMethod]
		public async Task PollForAccessTokenAsync_WhenPendingThenSuccess_SavesTokenToVault()
		{
			var vault = new InMemoryCredentialsVault();
			var service = new GitHubAuthService(vault, overrideClientId: "TestClientId123");

			var attempt = 0;
			using var handler = new MockHttpMessageHandler(req =>
			{
				attempt++;
				if (attempt == 1)
				{
					return new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent(@"{""error"": ""authorization_pending""}", Encoding.UTF8, "application/json")
					};
				}

				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent(@"{""access_token"": ""gho_TestAccessToken987"", ""token_type"": ""bearer"", ""scope"": ""repo""}", Encoding.UTF8, "application/json")
				};
			});
			using var client = new HttpClient(handler);

			// Test with short intervals
			var (status, token) = await service.PollForAccessTokenAsync("mock_device_code", 0, 30, client);

			Assert.AreEqual(TokenPollStatus.Success, status);
			Assert.AreEqual("gho_TestAccessToken987", token);

			// Verify token was stored in vault
			Assert.IsTrue(service.IsAuthenticated());
			Assert.AreEqual("gho_TestAccessToken987", service.GetSavedAccessToken());
		}

		[TestMethod]
		public async Task PollForAccessTokenAsync_WhenAccessDenied_ReturnsAccessDenied()
		{
			var vault = new InMemoryCredentialsVault();
			var service = new GitHubAuthService(vault, overrideClientId: "TestClientId123");

			using var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(@"{""error"": ""access_denied""}", Encoding.UTF8, "application/json")
			});
			using var client = new HttpClient(handler);

			var (status, token) = await service.PollForAccessTokenAsync("mock_device_code", 0, 30, client);

			Assert.AreEqual(TokenPollStatus.AccessDenied, status);
			Assert.IsNull(token);
			Assert.IsFalse(service.IsAuthenticated());
		}

		[TestMethod]
		public void VaultOperations_SaveAndRemove_WorksAsExpected()
		{
			var vault = new InMemoryCredentialsVault();
			var service = new GitHubAuthService(vault, overrideClientId: "TestClientId123");

			Assert.IsFalse(service.IsAuthenticated());
			Assert.IsNull(service.GetSavedAccessToken());

			// Save
			Assert.IsTrue(service.SaveAccessToken("ghp_secret_token_abc"));
			Assert.IsTrue(service.IsAuthenticated());
			Assert.AreEqual("ghp_secret_token_abc", service.GetSavedAccessToken());

			// Remove
			Assert.IsTrue(service.RemoveAccessToken());
			Assert.IsFalse(service.IsAuthenticated());
			Assert.IsNull(service.GetSavedAccessToken());
		}
	}
}
