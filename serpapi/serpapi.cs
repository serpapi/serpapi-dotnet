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
  public class SerpApiClient
  {
    const string JSON_FORMAT = "json";
    const string HTML_FORMAT = "html";
    const string HOST = "https://serpapi.com";

    // contextual parameter provided to SerpApi
    public Hashtable defaultParameter;

    // Core HTTP search
    public HttpClient client;


    public SerpApiClient(Hashtable parameter)
    {
      // assign query parameter
      this.defaultParameter = parameter;

      // initialize clean
      this.client = new HttpClient();

      // set default timeout to 60s
      this.setTimeoutSeconds(60);
    }

    /***
     * Set HTTP timeout in seconds
     */
    public void setTimeoutSeconds(int seconds)
    {
      this.client.Timeout = TimeSpan.FromSeconds(seconds);
    }

    /***
     * Get Json result
     */
    public JObject search(Hashtable parameter)
    {
      return json("/search", parameter);
    }

    /***
     * Get search archive for JSON results
     */
    public JObject searchArchive(string searchId)
    {
      return json("/searches/" + searchId + ".json", new Hashtable());
    }

    /***
     * Get search HTML results
     */
    public string html(Hashtable parameter)
    {
      return get("/search", parameter, false);
    }

    /***
   * Get user account 
   */
    public JObject account()
    {
      return json("/account", new Hashtable());
    }

    /***
    * Get location using location API 
    */
    public JArray location(Hashtable parameter)
    {
      // get json result
      string buffer = get("/locations.json", parameter, true);
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
          throw new SerpApiClientException(data.GetValue("error").ToString());
        }
        throw new SerpApiClientException("oops no error found when parsing: " + buffer);
      }
    }

    public string get(string endpoint, Hashtable parameter, bool jsonEnabled)
    {
      string url = createUrl(endpoint, parameter, jsonEnabled);
      // run asynchonous http query (.net framework implementation)
      Task<string> queryTask = createQuery(url, jsonEnabled);
      // block until http query is completed
      queryTask.ConfigureAwait(true);
      // parse result into json
      return queryTask.Result;
    }


    public JObject json(string uri, Hashtable parameter)
    {
      // get json result
      string buffer = get(uri, parameter, true);
      // parse json response (ignore http response status)
      JObject data = JObject.Parse(buffer);
      // report error if something went wrong
      if (data.ContainsKey("error"))
      {
        throw new SerpApiClientException(data.GetValue("error").ToString());
      }
      return data;
    }

    // Convert parmaterContext into URL request.
    // 
    // note:
    //  - C# URL encoding is pretty buggy and the API provides method which are not functional.
    //  - System.Web.HttpUtility.UrlEncode breaks if apply the full URL
    ///
    public string createUrl(string endpoint, Hashtable parameter, bool jsonEnabled)
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

    private async Task<string> createQuery(string url, bool jsonEnabled)
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
          throw new SerpApiClientException("Http request fail: " + content);
        }
      }
      catch (Exception ex)
      {
        // handle HTTP issues
        throw new SerpApiClientException(ex.ToString());
      }
      throw new SerpApiClientException("Oops something went very wrong");
    }
  }

  public class SerpApiClientException : Exception
  {
    public SerpApiClientException(string message) : base(message) { }
  }

}
