using Microsoft.VisualStudio.TestTools.UnitTesting;
using SerpApi;
using System;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;

namespace SerpApi.Test
{
  [TestClass]
  public class SerpApiClientResultTest
  {
    private SerpApiClient client;
    private Hashtable ht;
    private string apiKey;

    public SerpApiClientResultTest()
    {
      apiKey = Environment.GetEnvironmentVariable("API_KEY");

      // Localized client for Coffee shop in Austin Texas
      ht = new Hashtable();
      ht.Add("engine", "google");
      ht.Add("api_key", apiKey);
    }

    [TestMethod]
    public void TestLocation()
    {
      client = new SerpApiClient(ht);
      Hashtable locationParameter = new Hashtable();
      locationParameter.Add("q", "Austin,TX");
      locationParameter.Add("limit", "5");
      JArray locations = client.location(locationParameter);
      int counter = 0;
      foreach (JObject location in locations)
      {
        counter++;
        Assert.IsNotNull(location);
        Assert.IsNotNull(location.GetValue("id"));
        Assert.IsNotNull(location.GetValue("name"));
        Assert.IsNotNull(location.GetValue("google_id"));
        Assert.IsNotNull(location.GetValue("gps"));
        // Console.WriteLine(location);
      }

      Assert.AreEqual(1, counter);
    }

    [TestMethod]
    public void TestSearch()
    {
      client = new SerpApiClient(ht);

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
    public void TestArchiveSearch()
    {
      // Skip test on travis ci
      if (apiKey == null || apiKey == "demo")
      {
        return;
      }

      client = new SerpApiClient(ht);

      Hashtable searchParameter = new Hashtable();
      searchParameter.Add("location", "Austin, Texas, United States");
      searchParameter.Add("q", "Coffee");
      searchParameter.Add("hl", "en");
      searchParameter.Add("google_domain", "google.com");

      JObject data = client.search(searchParameter);
      string id = (string)((JObject)data["search_metadata"])["id"];
      JObject archivedSearch = client.searchArchive(id);
      int expected = GetSize((JArray)data["organic_results"]);
      int actual = GetSize((JArray)archivedSearch["organic_results"]);
      Assert.IsTrue(expected == actual);
    }

    public void TestGetAccount()
    {
      // Skip test on travis ci
      if (apiKey == null || apiKey == "demo")
      {
        return;
      }
      JObject account = client.account();
      Dictionary<string, string> dict = account.ToObject<Dictionary<string, string>>();
      Assert.IsNotNull(dict["account_id"]);
      Assert.IsNotNull(dict["plan_id"]);
      Assert.AreEqual(dict["apiKey"], apiKey);
    }

    [TestMethod]
    public void TestGetHtml()
    {
      client = new SerpApiClient(ht);

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

    private int GetSize(JArray array)
    {
      int size = 0;
      foreach (JObject e in array)
      {
        size++;
      }
      return size;
    }
  }

}