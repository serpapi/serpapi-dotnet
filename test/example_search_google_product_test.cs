// exampel for google_product 

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SerpApi;
using System;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;

namespace SerpApi.Test
{
  [TestClass]
  public class GoogleProductTest
  {

    public  GoogleProductTest()
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

      // Search on google_product
      Hashtable query = new Hashtable();
      query.Add("engine", "google_product");
      query.Add("q", "coffee");
      query.Add("product_id", "4887235756540435899");

      JObject results = client.search(query);
      Assert.IsNotNull(results);

      //Console.WriteLine(results);
      JObject product_results = (JObject)results["product_results"];
      Assert.IsNotNull(product_results);
      
      // release socket connection
      client.Close();
    }
  }
}