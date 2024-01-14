using Microsoft.VisualStudio.TestTools.UnitTesting;
using SerpApi;
using System;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;

namespace SerpApi.Test
{
  [TestClass]
  public class ArchiveSearchTest
  {
    private SerpApi client;
    private Hashtable ht;
    private string apiKey;

    public ArchiveSearchTest()
    {
      apiKey = Environment.GetEnvironmentVariable("API_KEY");

      // Localized client for Coffee shop in Austin Texas
      ht = new Hashtable();
      ht.Add("engine", "google");
      ht.Add("api_key", apiKey);
    }

    [TestMethod]
    public void TestArchiveSearch()
    {
      // Skip test on travis ci
      if (apiKey == null || apiKey == "demo")
      {
        return;
      }

      client = new SerpApi(ht);

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