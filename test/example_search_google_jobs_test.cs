// exampel for google_jobs 

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SerpApi;
using System;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;

namespace SerpApi.Test
{
  [TestClass]
  public class GoogleJobsTest
  {

    public  GoogleJobsTest()
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

      // Search on google_jobs
      Hashtable query = new Hashtable();
      query.Add("engine", "google_jobs");
      query.Add("q", "coffee");

      JObject results = client.search(query);
      Assert.IsNotNull(results);

      JArray jobs_results = (JArray)results["jobs_results"];
      Assert.IsNotNull(jobs_results);
      
      // release socket connection
      client.Close();
    }
  }
}