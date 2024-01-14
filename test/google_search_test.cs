using Microsoft.VisualStudio.TestTools.UnitTesting;
using SerpApi;
using System;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;

namespace SerpApi.Test
{
  [TestClass]
  public class GoogleSearchTest
  {
    private SerpApi client;
    private Hashtable ht;
    private string apiKey;

    public GoogleSearchTest()
    {
      apiKey = Environment.GetEnvironmentVariable("API_KEY");

      // Localized client for Coffee shop in Austin Texas
      ht = new Hashtable();
      ht.Add("engine", "google");
      ht.Add("api_key", apiKey);
    }

    [TestMethod]
    public void TestSearch()
    {
      client = new SerpApi(ht);

      Hashtable searchParameter = new Hashtable();
      searchParameter.Add("location", "Austin, Texas, United States");
      searchParameter.Add("q", "Coffee");
      searchParameter.Add("hl", "en");
      searchParameter.Add("google_domain", "google.com");

      JObject data = client.search(searchParameter);
      JArray coffeeShops = (JArray)data["local_results"]["places"];
      int counter = 0;
      foreach (JObject coffeeShop in coffeeShops)
      {
        Assert.IsNotNull(coffeeShop["title"]);
        counter++;
      }
      Assert.IsTrue(counter >= 1);

      coffeeShops = (JArray)data["organic_results"];
      Assert.IsNotNull(coffeeShops);
      foreach (JObject coffeeShop in coffeeShops)
      {
        Console.WriteLine("Found: " + coffeeShop["title"]);
        Assert.IsNotNull(coffeeShop["title"]);
      }

      // Release socket connection
      client.Close();
    }


    [TestMethod]
    public void TestHtml()
    {
      client = new SerpApi(ht);

      Hashtable searchParameter = new Hashtable();
      searchParameter.Add("location", "Austin, Texas, United States");
      searchParameter.Add("q", "Coffee");
      searchParameter.Add("hl", "en");
      searchParameter.Add("google_domain", "google.com");
      string htmlContent = client.html(searchParameter);
      Assert.IsNotNull(htmlContent);
      //Console.WriteLine(htmlContent);
      Assert.IsTrue(htmlContent.Contains("</body>"));
      // Release socket connection
      client.Close();
    }
  }

}