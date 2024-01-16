// exampel for google_local_services 

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SerpApi;
using System;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;

namespace SerpApi.Test
{
  [TestClass]
  public class GoogleLocalServicesTest
  {

    public  GoogleLocalServicesTest()
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

      // Search on google_local_services
      Hashtable query = new Hashtable();
      query.Add("engine", "google_local_services");
      query.Add("q", "electrician");
      query.Add("data_cid", "6745062158417646970");

      JObject results = client.search(query);
      Assert.IsNotNull(results);

      JArray local_ads = (JArray)results["local_ads"];
      Assert.IsNotNull(local_ads);
      
      // release socket connection
      client.Close();
    }
  }
}