using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CtrlCV
{
    public record UpdateResult(
        bool IsUpdateAvailable,
        string LatestVersion,
        string DownloadUrl,
        string ReleaseUrl,
        string ReleaseNotes);

    public static class UpdateChecker
    {
        private const string GitHubApiUrl =
            "https://api.github.com/repos/keatkean/CtrlCV/releases/latest";

        private static readonly HttpClient _httpClient = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("CtrlCV", "1.0"));
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            return client;
        }

        public static async Task<UpdateResult> CheckForUpdateAsync(string currentVersion)
        {
            var response = await _httpClient.GetAsync(GitHubApiUrl);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;

            var tagName = root.GetProperty("tag_name").GetString() ?? "";
            var releaseUrl = root.GetProperty("html_url").GetString() ?? "";
            var body = root.TryGetProperty("body", out var bodyEl)
                ? bodyEl.GetString() ?? ""
                : "";

            var latestVersion = tagName.TrimStart('v', 'V');

            string downloadUrl = "";
            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString() ?? "";
                    if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                        break;
                    }
                }
            }

            bool isNewer = false;
            if (Version.TryParse(latestVersion, out var latest) &&
                Version.TryParse(currentVersion, out var current))
            {
                isNewer = latest > current;
            }

            return new UpdateResult(isNewer, latestVersion, downloadUrl, releaseUrl, body);
        }

        public static async Task DownloadUpdateAsync(
            string downloadUrl, string targetPath, IProgress<int>? progress = null)
        {
            using var response = await _httpClient.GetAsync(downloadUrl,
                HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            long bytesRead = 0;

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(
                targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

            var buffer = new byte[81920];
            int read;
            while ((read = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read));
                bytesRead += read;

                if (totalBytes > 0)
                    progress?.Report((int)(bytesRead * 100 / totalBytes));
            }

            progress?.Report(100);
        }

        public static void ApplyUpdateAndRestart(string updateFilePath, string currentExePath)
        {
            var dir = Path.GetDirectoryName(currentExePath)!;
            var exeName = Path.GetFileName(currentExePath);
            var oldName = exeName + ".old";
            var updateName = Path.GetFileName(updateFilePath);

            var script =
                $"/c timeout /t 2 /nobreak >nul " +
                $"&& del \"{Path.Combine(dir, oldName)}\" 2>nul " +
                $"&& ren \"{currentExePath}\" \"{oldName}\" " +
                $"&& ren \"{updateFilePath}\" \"{exeName}\" " +
                $"&& start \"\" \"{Path.Combine(dir, exeName)}\" " +
                $"&& del \"{Path.Combine(dir, oldName)}\" 2>nul";

            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = script,
                WorkingDirectory = dir,
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
    }
}
