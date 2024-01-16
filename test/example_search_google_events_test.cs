// exampel for google_events 

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SerpApi;
using System;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;

namespace SerpApi.Test
{
  [TestClass]
  public class GoogleEventsTest
  {

    public  GoogleEventsTest()
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

      // Search on google_events
      Hashtable query = new Hashtable();
      query.Add("engine", "google_events");
      query.Add("q", "coffee");

      JObject results = client.search(query);
      Assert.IsNotNull(results);

      JArray events_results = (JArray)results["events_results"];
      Assert.IsNotNull(events_results);
      
      // release socket connection
      client.Close();
    }
  }
}