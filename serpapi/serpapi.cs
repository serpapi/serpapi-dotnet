using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

/***
 * Client for SerpApi.com
 */
namespace SerpApi
{
  public class SerpApi
  {
    const string JSON_FORMAT = "json";
    const string HTML_FORMAT = "html";
    const string HOST = "https://serpapi.com";

    // contextual parameter provided to SerpApi
    public Hashtable defaultParameter;

    // Core HTTP search
    public HttpClient client;

    public SerpApi(Hashtable parameter = null)
    {
      // assign query parameter
      if(parameter == null) {
        parameter = new Hashtable();
      }
      this.defaultParameter = parameter;

      // initialize clean
      this.client = new HttpClient();

      // set default timeout to 60s
      this.SetTimeoutSeconds(60);
    }

    /***
     * Set HTTP timeout in seconds
     */
    public void SetTimeoutSeconds(int seconds)
    {
      this.client.Timeout = TimeSpan.FromSeconds(seconds);
    }

    /***
     * Get Json result
     */
    public JObject Search(Hashtable parameter)
    {
      return Json("/search", parameter);
    }

    /***
     * Get search archive for JSON results
     */
    public JObject SearchArchive(string searchId)
    {
      return Json("/searches/" + searchId + ".json", new Hashtable());
    }

    /***
     * Get search HTML results
     */
    public string Html(Hashtable parameter)
    {
      return Get("/search", parameter, false);
    }

    /***
   * Get user account 
   */
    public JObject Account(string apiKey = "")
    {
      Hashtable parameter = new Hashtable();
      if(apiKey != "") {
        parameter.Add("api_key", apiKey);
      }
      return Json("/account", parameter);
    }

    /***
    * Get location using location API 
    */
    public JArray Location(Hashtable parameter)
    {
      // get json result
      string buffer = Get("/locations.json", parameter, true);
      // parse json response (ignore http response status)
      try {
        JArray data = JArray.Parse(buffer);
        return data;
      }
      catch 
      {
        // report error if something went wrong
        JObject data = JObject.Parse(buffer);
        if (data.ContainsKey("error"))
        {
          throw new ClientException(data.GetValue("error").ToString());
        }
        throw new ClientException("oops no error found when parsing: " + buffer);
      }
    }

    public string Get(string endpoint, Hashtable parameter, bool jsonEnabled)
    {
      string url = CreateUrl(endpoint, parameter, jsonEnabled);
      // run asynchonous http query (.net framework implementation)
      Task<string> queryTask = CreateQuery(url, jsonEnabled);
      // block until http query is completed
      queryTask.ConfigureAwait(true);
      // parse result into json
      return queryTask.Result;
    }


    public JObject Json(string uri, Hashtable parameter)
    {
      // get json result
      string buffer = Get(uri, parameter, true);
      // parse json response (ignore http response status)
      JObject data = JObject.Parse(buffer);
      // report error if something went wrong
      if (data.ContainsKey("error"))
      {
        throw new ClientException(data.GetValue("error").ToString());
      }
      return data;
    }

    // Convert parmaterContext into URL request.
    // 
    // note:
    //  - C# URL encoding is pretty buggy and the API provides method which are not functional.
    //  - System.Web.HttpUtility.UrlEncode breaks if apply the full URL
    ///
    public string CreateUrl(string endpoint, Hashtable parameter, bool jsonEnabled)
    {
      // merge parameter
      Hashtable table = new Hashtable();
      // default parameter
      foreach(DictionaryEntry e in this.defaultParameter)
      {
        if (!parameter.ContainsKey(e.Key)) {
          table.Add(e.Key, e.Value);
        }
      }
      // user parameter override
      foreach(DictionaryEntry e in parameter)
      {
        table.Add(e.Key, e.Value);
      }

      string s = "";
      foreach (DictionaryEntry entry in table)
      {
        if (s != "")
        {
          s += "&";
        }
        // encode each value in case of special character
        s += entry.Key + "=" + System.Web.HttpUtility.UrlEncode((string)entry.Value, System.Text.Encoding.UTF8);
      }

      // append output format
      s += "&output=" + (jsonEnabled ? JSON_FORMAT : HTML_FORMAT);

      // append source language
      s += "&source=dotnet";

      return HOST + endpoint + "?" + s;
    }

    /***
     * Close socket connection associated to HTTP search
     */
    public void Close()
    {
      this.client.Dispose();
    }

    private async Task<string> CreateQuery(string url, bool jsonEnabled)
    {
      // display url for debug: 
      //Console.WriteLine("url: " + url);
      try
      {
        HttpResponseMessage response = await this.client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        // return raw JSON
        if (jsonEnabled)
        {
          response.Dispose();
          return content;
        }
        // HTML response or other
        if (response.IsSuccessStatusCode)
        {
          response.Dispose();
          return content;
        }
        else
        {
          response.Dispose();
          throw new ClientException("Http request fail: " + content);
        }
      }
      catch (Exception ex)
      {
        // handle HTTP issues
        throw new ClientException(ex.ToString());
      }
      //throw new ClientException("Oops something went very wrong");
    }
  }

  public class ClientException : Exception
  {
    public ClientException(string message) : base(message) { }
  }

}
