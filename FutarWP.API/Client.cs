﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace FutarWP.API
{
  public class Client
  {
    private string _apiKey;
    private string _appVersion;
    private readonly string _version = "4";

    private Settings _settings = new Settings();
    private Newtonsoft.Json.JsonSerializerSettings _deserializerSettings = new Newtonsoft.Json.JsonSerializerSettings()
      {
        MetadataPropertyHandling = Newtonsoft.Json.MetadataPropertyHandling.ReadAhead,
        NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
        TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects,
      };

    public Client()
    {
    }

    public async Task<bool> ScrapeCredentials()
    {
      var http = new HTTP();
      var pageData = await http.DoGETRequestAsync("https://futar.bkk.hu/");

      var versionRegex = new System.Text.RegularExpressions.Regex(@"\/ride-gui\/(.*?)/");
      var match = versionRegex.Match(pageData);
      if (!match.Success)
      {
        return false;
      }
      _appVersion = match.Groups[1].Value.ToString();

      var configRegex = new System.Text.RegularExpressions.Regex(@"window\.APP_CONFIG = (.*?);");
      match = configRegex.Match(pageData);
      if (!match.Success)
      {
        return false;
      }
      var configJson = match.Groups[1].Value.ToString();
      var config = Newtonsoft.Json.JsonConvert.DeserializeObject(configJson) as Newtonsoft.Json.Linq.JObject;
      if (config == null)
      {
        return false;
      }
      _apiKey = config.GetValue("API_KEY").ToString();

      return true;
    }

    public Settings Settings => _settings;
    public async Task<T> GetAsync<T>(ICommand input)
    {
      return await RequestAsync<T>("GET", input);
    }

    public async Task<T> PostAsync<T>(ICommand input)
    {
      return await RequestAsync<T>("POST", input);
    }

    protected async Task<T> RequestAsync<T>(string method, ICommand input)
    {
      var http = new HTTP();

      var url = $"https://futar.bkk.hu/api/query/v1/ws/otp/api/where/{input.Command}.json";
      string responseJson = null;
      string bodyJson = string.Empty;
      var headers = new NameValueCollection();
      switch (method)
      {
        case "GET":
          {
            var dto = new DateTimeOffset(DateTime.UtcNow);
            var unixTimeMilliSeconds = dto.ToUnixTimeMilliseconds().ToString();

            var kvpArray = SerializeInput(input);
            kvpArray.Add("key", _apiKey);
            kvpArray.Add("version", _version);
            kvpArray.Add("appVersion", _appVersion);
            kvpArray.Add("_", unixTimeMilliSeconds.ToString());

            var first = true;
            foreach (var kvp in kvpArray)
            {
              url += first ? "?" : "&";
              url += $"{kvp.Key}={WebUtility.UrlEncode(kvp.Value)}";
              first = false;
            }
            responseJson = await http.DoGETRequestAsync(url, null, headers);
          }
          break;
        case "POST":
          {
            var inputType = input.GetType();
            var fields = inputType.GetFields();
            bodyJson = Newtonsoft.Json.JsonConvert.SerializeObject(input, _deserializerSettings);
            if (bodyJson == "{}" || fields.Length == 0)
            {
              bodyJson = string.Empty;
            }
            responseJson = await http.DoPOSTRequestAsync(url, bodyJson, headers);
          }
          break;
      }

      return responseJson != null ? Newtonsoft.Json.JsonConvert.DeserializeObject<T>(responseJson, _deserializerSettings) : default(T);
    }

    private Dictionary<string,string> SerializeInput(ICommand input)
    {
      var inputType = input.GetType();
      var queryString = new Dictionary<string, string>();
      foreach (var field in inputType.GetFields())
      {
        if (field.GetCustomAttribute(typeof(Newtonsoft.Json.JsonIgnoreAttribute)) != null)
        {
          continue;
        }
        var value = inputType.GetField(field.Name).GetValue(input);
        if (value != null)
        {
          var a = value as IEnumerable<object>;
          if (a != null)
          {
            queryString.Add(field.Name, string.Join(",", a.Select(s => s.ToString())));
          }
          else
          {
            queryString.Add(field.Name, value.ToString());
          }
        }
      }
      return queryString;
    }
  }
}
