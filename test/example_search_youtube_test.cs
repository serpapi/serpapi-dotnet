// exampel for youtube 

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SerpApi;
using System;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;

namespace SerpApi.Test
{
  [TestClass]
  public class YoutubeTest
  {

    public  YoutubeTest()
    {
    }

    [TestMethod]
    public void TestSearch()
    {
      string apiKey = Environment.GetEnvironmentVariable("API_KEY");
      Assert.IsNotNull(apiKey);

      // Setup client
      Hashtable auth = new Hashtable();
      auth.Add("api_key", apiKey);

      SerpApi client = new SerpApi(auth);

      // Search on youtube
      Hashtable query = new Hashtable();
      query.Add("engine", "youtube");
      query.Add("search_query", "coffee");

      JObject results = client.search(query);
      Assert.IsNotNull(results);

      JArray video_results = (JArray)results["video_results"];
      Assert.IsNotNull(video_results);
      
      // release socket connection
      client.Close();
    }
  }
}