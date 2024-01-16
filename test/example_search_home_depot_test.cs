// exampel for home_depot 

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SerpApi;
using System;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;

namespace SerpApi.Test
{
  [TestClass]
  public class HomeDepotTest
  {

    public  HomeDepotTest()
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

      // Search on home_depot
      Hashtable query = new Hashtable();
      query.Add("engine", "home_depot");
      query.Add("q", "table");

      JObject results = client.search(query);
      Assert.IsNotNull(results);

      JArray products = (JArray)results["products"];
      Assert.IsNotNull(products);
      
      // release socket connection
      client.Close();
    }
  }
}