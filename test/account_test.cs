using Microsoft.VisualStudio.TestTools.UnitTesting;
using SerpApi;
using System;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;

namespace SerpApi.Test
{
  [TestClass]
  public class AccountTest
  {
    private SerpApi.Client client;
    private string apiKey;

    public AccountTest()
    {
      apiKey = Environment.GetEnvironmentVariable("API_KEY");
      Hashtable ht = new Hashtable();
      ht.Add("api_key", apiKey);
      this.client = new SerpApi.Client(ht);
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
  }
  }