// exampel for google_images 

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SerpApi;
using System;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;

namespace SerpApi.Test
{
  [TestClass]
  public class GoogleImagesTest
  {

    public  GoogleImagesTest()
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

      // Search on google_images
      Hashtable query = new Hashtable();
      query.Add("engine", "google_images");
      query.Add("tbm", "isch");
      query.Add("q", "coffee");

      JObject results = client.search(query);
      Assert.IsNotNull(results);

      JArray images_results = (JArray)results["images_results"];
      Assert.IsNotNull(images_results);
      
      // release socket connection
      client.Close();
    }
  }
}