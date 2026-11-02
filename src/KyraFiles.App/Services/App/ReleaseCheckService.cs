// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using Windows.ApplicationModel;

namespace Files.App.Services
{
	/// <summary>
	/// Outcome of a <see cref="ReleaseCheckService.CheckAsync"/> call.
	/// </summary>
	internal enum ReleaseCheckStatus
	{
		UpToDate,
		Available,
		NoReleases,
		Failed,
	}

	/// <param name="Status">What the check found.</param>
	/// <param name="Current">Installed version.</param>
	/// <param name="Latest">Latest published version (only for <see cref="ReleaseCheckStatus.UpToDate"/> and <see cref="ReleaseCheckStatus.Available"/>).</param>
	/// <param name="PageUrl">Release page to open for download (only for <see cref="ReleaseCheckStatus.Available"/>).</param>
	internal sealed record ReleaseCheckResult(ReleaseCheckStatus Status, Version Current, Version? Latest = null, string? PageUrl = null);

	/// <summary>
	/// Asks the project's GitHub releases for the latest published version. Runs only on demand (F018, AC-7).
	/// </summary>
	internal static partial class ReleaseCheckService
	{
		private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

		[GeneratedRegex(@"\d+(\.\d+){1,3}")]
		private static partial Regex VersionPattern();

		public static Version CurrentVersion
		{
			get
			{
				var v = Package.Current.Id.Version;
				return new Version(v.Major, v.Minor, v.Build, v.Revision);
			}
		}

		public static async Task<ReleaseCheckResult> CheckAsync(CancellationToken cancellationToken = default)
		{
			var current = CurrentVersion;

			try
			{
				using var client = new HttpClient { Timeout = Timeout };
				client.DefaultRequestHeaders.UserAgent.ParseAdd("FilesCustom-UpdateCheck");
				client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

				using var response = await client.GetAsync(Constants.ExternalUrl.CustomReleasesApiUrl, cancellationToken);

				if (response.StatusCode == HttpStatusCode.NotFound)
					return new(ReleaseCheckStatus.NoReleases, current);

				if (!response.IsSuccessStatusCode)
					return new(ReleaseCheckStatus.Failed, current);

				await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
				using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

				var tag = json.RootElement.TryGetProperty("tag_name", out var tagElement) ? tagElement.GetString() : null;
				var match = tag is null ? null : VersionPattern().Match(tag);
				if (match is not { Success: true } || !Version.TryParse(match.Value, out var latest))
					return new(ReleaseCheckStatus.Failed, current);

				var pageUrl = json.RootElement.TryGetProperty("html_url", out var urlElement) ? urlElement.GetString() : null;

				// Missing parts count as zero ("4.2" == "4.2.0.0"), so compare normalized versions.
				var normalizedLatest = new Version(latest.Major, latest.Minor, Math.Max(latest.Build, 0), Math.Max(latest.Revision, 0));

				return normalizedLatest > current
					? new(ReleaseCheckStatus.Available, current, normalizedLatest, pageUrl ?? Constants.ExternalUrl.CustomReleasesPageUrl)
					: new(ReleaseCheckStatus.UpToDate, current, normalizedLatest);
			}
			catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
			{
				App.Logger.LogWarning(ex, "Update check failed.");
				return new(ReleaseCheckStatus.Failed, current);
			}
		}
	}
}
