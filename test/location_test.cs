using Microsoft.VisualStudio.TestTools.UnitTesting;
using SerpApi;
using System;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;

namespace SerpApi.Test
{
  [TestClass]
  public class LocationTest
  {
    private SerpApi client;
    private string apiKey;

    public LocationTest()
    {
      apiKey = Environment.GetEnvironmentVariable("API_KEY");
    }

    [TestMethod]
    public void TestLocation()
    {
      client = new SerpApi();
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
  }
}