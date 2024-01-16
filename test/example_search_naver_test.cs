// exampel for naver 

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SerpApi;
using System;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;

namespace SerpApi.Test
{
  [TestClass]
  public class NaverTest
  {

    public  NaverTest()
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

      // Search on naver
      Hashtable query = new Hashtable();
      query.Add("engine", "naver");
      query.Add("query", "coffee");

      JObject results = client.search(query);
      Assert.IsNotNull(results);

      JArray ads_results = (JArray)results["ads_results"];
      Assert.IsNotNull(ads_results);
      
      // release socket connection
      client.Close();
    }
  }
}