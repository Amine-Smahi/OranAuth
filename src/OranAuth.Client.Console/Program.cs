﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OranAuth.Client.Console
{
    //Note: First you should run the `OranAuth.Client.WebApp` project and then run the `ConsoleClient` project.

    public class Token
    {
        [JsonProperty("access_token")] public string AccessToken { get; set; }

        [JsonProperty("refresh_token")] public string RefreshToken { get; set; }
    }

    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    internal class Program
    {
        private const string BaseAddress = "https://localhost:5001/";

        private static readonly HttpClientHandler HttpClientHandler = new HttpClientHandler
        {
            UseCookies = true,
            UseDefaultCredentials = true,
            CookieContainer = new CookieContainer()
        };

        private static readonly HttpClient HttpClient = new HttpClient(HttpClientHandler)
        {
            BaseAddress = new Uri(BaseAddress)
        };

        public static async Task Main(string[] args)
        {
            var (token, appCookies) = await LoginAsync(
                "/api/account/login",
                "Amine",
                "1234");
            await CallProtectedApiAsync("/api/MyProtectedApi", token);
            var newToken = await RefreshTokenAsync("/api/account/RefreshToken", token, appCookies);
        }

        private static Dictionary<string, string> GetAntiforgeryCookies()
        {
            System.Console.WriteLine("\nGet Antiforgery Cookies:");
            var cookies = HttpClientHandler.CookieContainer.GetCookies(new Uri(BaseAddress));

            var appCookies = new Dictionary<string, string>();
            System.Console.WriteLine("WebApp Cookies:");
            foreach (Cookie cookie in cookies)
            {
                System.Console.WriteLine($"Name : {cookie.Name}");
                System.Console.WriteLine($"Value: {cookie.Value}");
                appCookies.Add(cookie.Name, cookie.Value);
            }

            return appCookies;
        }

        private static async Task<(Token Token, Dictionary<string, string> AppCookies)> LoginAsync(string requestUri,
            string username, string password)
        {
            System.Console.WriteLine("\nLogin:");

            var response =
                await HttpClient.PostAsJsonAsync(requestUri, new User {Username = username, Password = password});
            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadAsAsync<Token>(new[] {new JsonMediaTypeFormatter()});
            System.Console.WriteLine($"Response    : {response}");
            System.Console.WriteLine($"AccessToken : {token.AccessToken}");
            System.Console.WriteLine($"RefreshToken: {token.RefreshToken}");

            var appCookies = GetAntiforgeryCookies();
            return (token, appCookies);
        }

        private static async Task<Token> RefreshTokenAsync(string requestUri, Token token,
            IReadOnlyDictionary<string, string> appCookies)
        {
            System.Console.WriteLine("\nRefreshToken:");

            if (!HttpClient.DefaultRequestHeaders.Contains("X-XSRF-TOKEN"))
                // this is necessary for [AutoValidateAntiforgeryTokenAttribute] and all of the 'POST' requests
                HttpClient.DefaultRequestHeaders.Add("X-XSRF-TOKEN", appCookies["XSRF-TOKEN"]);
            // this is necessary to populate the this.HttpContext.User object automatically
            HttpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token.AccessToken);
            var response = await HttpClient.PostAsJsonAsync(requestUri, new {refreshToken = token.RefreshToken});
            response.EnsureSuccessStatusCode();

            var newToken = await response.Content.ReadAsAsync<Token>(new[] {new JsonMediaTypeFormatter()});
            System.Console.WriteLine($"Response    : {response}");
            System.Console.WriteLine($"New AccessToken : {newToken.AccessToken}");
            System.Console.WriteLine($"New RefreshToken: {newToken.RefreshToken}");
            return newToken;
        }

        private static async Task CallProtectedApiAsync(string requestUri, Token token)
        {
            System.Console.WriteLine("\nCall ProtectedApi:");
            HttpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token.AccessToken);
            var response = await HttpClient.GetAsync(requestUri);
            var message = await response.Content.ReadAsStringAsync();
            System.Console.WriteLine("URL response: " + message);
        }
    }
}
