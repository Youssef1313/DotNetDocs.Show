﻿using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DotNetDocs.Services.Models;
using DotNetDocs.Services.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace DotNetDocs.Services
{
    public class LogicAppService
    {
        readonly LogicAppOptions _settings;
        readonly HttpClient _client;
        readonly ILogger<LogicAppService> _logger;

        public LogicAppService(IOptions<LogicAppOptions> options, HttpClient client, ILogger<LogicAppService> logger) =>
            (_settings, _client, _logger) = (options.Value, client, logger);

        public ValueTask<bool> CreateShowCalendarInviteAsync(DocsShow show)
        {
            DateTimeOffset showTime = show.Date!.Value;
            string bodyText =
                $"<strong>🕚</strong> 30 mins before the show, work through the <a href='https://aka.ms/go-live-checklist'>go-live checklist</a>.<br>" +
                $"<strong>📺</strong> <a href='{show.GuestStreamUrl}'>Join the stream</a>.<br>" +
                $"<strong>👋</strong> <a href='https://dotnetdocs.dev/show/{show.Id}'>Share your episode to help boost viewership</a>.";

            return PostJsonAsync(new
            {
                to = string.Join(";", show.Guests.Select(g => g.Email).Concat(new[] { "dotnetdocsshow@microsoft.com" })),
                title = $"The .NET Docs Show: {show.Title}",
                body = bodyText,
                startTime = $"{showTime:yyyy-MM-ddThh:mm:ss}",
                endTime = $"{showTime.AddHours(1):yyyy-MM-ddThh:mm:ss}",
            },
            _settings.PostCalendarUrl);
        }

        public ValueTask<bool> RequestShowAsync(
            DateTimeOffset showDate,
            string tentativeTitle,
            string idea,
            string firstName,
            string lastName,
            string email,
            string twitterHandle) => PostJsonAsync(new
            {
                showDate,
                tentativeTitle,
                idea,
                firstName,
                lastName,
                email,
                twitterHandle
            }, _settings.PostShowRequestUrl);

        public ValueTask<bool> ProposeShowIdeaAsync(
            string idea,
            string firstName,
            string lastName,
            string email,
            string twitterHandle) => PostJsonAsync(new
            {
                idea,
                firstName,
                lastName,
                email,
                twitterHandle
            }, _settings.PostShowIdeaUrl);

        async ValueTask<bool> PostJsonAsync<T>(T obj, string url)
        {
            try
            {
                string json = JsonConvert.SerializeObject(obj);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage? response = await _client.PostAsync(url, content);
                if (response != null)
                {
                    response.EnsureSuccessStatusCode();
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return false;
        }
    }
}
