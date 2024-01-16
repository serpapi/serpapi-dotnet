// exampel for google_maps 

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SerpApi;
using System;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;

namespace SerpApi.Test
{
  [TestClass]
  public class GoogleMapsTest
  {

    public  GoogleMapsTest()
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

      // Search on google_maps
      Hashtable query = new Hashtable();
      query.Add("engine", "google_maps");
      query.Add("q", "pizza");
      query.Add("ll", "@40.7455096,-74.0083012,15.1z");
      query.Add("type", "search");

      JObject results = client.search(query);
      Assert.IsNotNull(results);

      JArray local_results = (JArray)results["local_results"];
      Assert.IsNotNull(local_results);
      
      // release socket connection
      client.Close();
    }
  }
}