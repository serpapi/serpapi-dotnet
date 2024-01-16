// exampel for google_autocomplete 

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SerpApi;
using System;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;

namespace SerpApi.Test
{
  [TestClass]
  public class GoogleAutocompleteTest
  {

    public  GoogleAutocompleteTest()
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

      // Search on google_autocomplete
      Hashtable query = new Hashtable();
      query.Add("engine", "google_autocomplete");
      query.Add("q", "coffee");

      JObject results = client.search(query);
      Assert.IsNotNull(results);

      JArray suggestions = (JArray)results["suggestions"];
      Assert.IsNotNull(suggestions);
      
      // release socket connection
      client.Close();
    }
  }
}