// exampel for yahoo 

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SerpApi;
using System;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;

namespace SerpApi.Test
{
  [TestClass]
  public class YahooTest
  {

    public  YahooTest()
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

      // Search on yahoo
      Hashtable query = new Hashtable();
      query.Add("engine", "yahoo");
      query.Add("p", "coffee");

      JObject results = client.search(query);
      Assert.IsNotNull(results);

      JArray organic_results = (JArray)results["organic_results"];
      Assert.IsNotNull(organic_results);
      
      // release socket connection
      client.Close();
    }
  }
}