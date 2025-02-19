﻿using System;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using static BBDown.Core.Logger;

namespace BBDown.Core.Util
{
    public class HTTPUtil
    {
        public static string DmCoverImgStr { get; set; } = GetDmCoverImgStr();

        private static string GetDmCoverImgStr()
        {
            byte[] buffer = Encoding.UTF8.GetBytes(HTTPUtil.UserAgent);
            string base64 = Convert.ToBase64String(buffer);
            return base64[..^2];
        }


        public static readonly HttpClient AppHttpClient = new(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.All,
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        })
        {
            Timeout = TimeSpan.FromMinutes(2)
        };

        private static readonly Random random = new Random();
        private static readonly string[] platforms = { "Windows NT 10.0; Win64", "Macintosh; Intel Mac OS X 10_15", "X11; Linux x86_64" };

        private static string RandomVersion(int min, int max)
        {
            double version = random.NextDouble() * (max - min) + min;
            return version.ToString("F3");
        }

        private static string GetRandomUserAgent()
        {
            string[] browsers = { $"AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{RandomVersion(80, 110)} Safari/537.36", $"Gecko/20100101 Firefox/{RandomVersion(80, 110)}" };
            return $"Mozilla/5.0 ({platforms[random.Next(platforms.Length)]}) {browsers[random.Next(browsers.Length)]}";
        }

        public static string UserAgent { get; set; } = GetRandomUserAgent();

        public static async Task<string> GetWebSourceAsync(string url)
        {
            using var webRequest = new HttpRequestMessage(HttpMethod.Get, url);
            webRequest.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
            webRequest.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
            webRequest.Headers.TryAddWithoutValidation("Cookie", (url.Contains("/ep") || url.Contains("/ss")) ? Config.COOKIE + ";CURRENT_FNVAL=4048;" : Config.COOKIE);
            if (url.Contains("api.bilibili.com"))
                webRequest.Headers.TryAddWithoutValidation("Referer", "https://www.bilibili.com/");
            webRequest.Headers.CacheControl = CacheControlHeaderValue.Parse("no-cache");
            webRequest.Headers.Connection.Clear();
            LogDebug("获取网页内容: Url: {0}, Headers: {1}", url, webRequest.Headers);
            var webResponse = (await AppHttpClient.SendAsync(webRequest, HttpCompletionOption.ResponseHeadersRead)).EnsureSuccessStatusCode();

            string htmlCode = await webResponse.Content.ReadAsStringAsync();
            LogDebug("Response: {0}", htmlCode);
            return htmlCode;
        }

        // 重写重定向处理, 自动跟随多次重定向
        public static async Task<string> GetWebLocationAsync(string url)
        {
            using var webRequest = new HttpRequestMessage(HttpMethod.Head, url);
            webRequest.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
            webRequest.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
            webRequest.Headers.CacheControl = CacheControlHeaderValue.Parse("no-cache");
            webRequest.Headers.Connection.Clear();

            LogDebug("获取网页重定向地址: Url: {0}, Headers: {1}", url, webRequest.Headers);
            var webResponse = (await AppHttpClient.SendAsync(webRequest, HttpCompletionOption.ResponseHeadersRead)).EnsureSuccessStatusCode();
            string location = webResponse.RequestMessage.RequestUri.AbsoluteUri;
            LogDebug("Location: {0}", location);
            return location;
        }

        public static async Task<string> GetPostResponseAsync(string Url, byte[] postData)
        {
            LogDebug("Post to: {0}, data: {1}", Url, Convert.ToBase64String(postData));
            using HttpRequestMessage request = new(HttpMethod.Post, Url);
            request.Headers.TryAddWithoutValidation("Content-Type", "application/grpc");
            request.Headers.TryAddWithoutValidation("Content-Length", postData.Length.ToString());
            request.Headers.TryAddWithoutValidation("User-Agent", "Dalvik/2.1.0 (Linux; U; Android 6.0.1; oneplus a5010 Build/V417IR) 6.10.0 os/android model/oneplus a5010 mobi_app/android build/6100500 channel/bili innerVer/6100500 osVer/6.0.1 network/2");
            request.Headers.TryAddWithoutValidation("Cookie", Config.COOKIE);
            request.Content = new ByteArrayContent(postData);
            var webResponse = await AppHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            string htmlCode = await webResponse.Content.ReadAsStringAsync();
            return htmlCode;
        }
    }
}
