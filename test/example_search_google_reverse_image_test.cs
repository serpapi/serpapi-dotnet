// exampel for google_reverse_image 

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SerpApi;
using System;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;

namespace SerpApi.Test
{
  [TestClass]
  public class GoogleReverseImageTest
  {

    public  GoogleReverseImageTest()
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

      // Search on google_reverse_image
      Hashtable query = new Hashtable();
      query.Add("engine", "google_reverse_image");
      query.Add("image_url", "https://i.imgur.com/5bGzZi7.jpg");

      JObject results = client.search(query);
      Assert.IsNotNull(results);

      JArray image_sizes = (JArray)results["image_sizes"];
      Assert.IsNotNull(image_sizes);
      
      // release socket connection
      client.Close();
    }
  }
}