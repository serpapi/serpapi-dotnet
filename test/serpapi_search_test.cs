using Microsoft.VisualStudio.TestTools.UnitTesting;
using SerpApi;
using System;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;

namespace SerpApi.Test
{
  [TestClass]
  public class SerpApiClientTest
  {
    // [TestMethod]
    // public void TestSearpApiSearchGetJson()
    // {
    //   String apiKey = Environment.GetEnvironmentVariable("API_KEY");
    //   Hashtable ht = new Hashtable();
    //   ht.Add("q", "Coffee");
    //   ht.Add("location", "Austin, Texas, United States");
    //   ht.Add("hl", "en");
    //   ht.Add("tbs", "qdr:d");
    //   ht.Add("num", "10");
    //   ht.Add("google_domain", "google.com");
    //   ht.Add("engine", "google");

    //   SerpApiClient client = new SerpApiClient(ht, apiKey);
    //   JObject data = client.GetJson();
    //   JArray coffeeShops = (JArray)data["organic_results"];
    //   Assert.IsNotNull(coffeeShops);
    //   Assert.IsTrue(coffeeShops.Count > 5);
    //   // Release socket connection
    //   client.Close();
    // }

    [TestMethod]
    public void TestSpecialCharactersEncoding()
    {
      // special query
      string q = "¡ ™£¢∞§¶•ªº–≠œ∑´®†¥¨ˆøπ“‘«åß∂ƒ©åß∂ƒ©˚¬…æ≈ç√∫˜µ≤≥÷" + "`⁄€‹›ﬁﬂ‡°·‚—±Œ„´‰ˇÁ¨Ø∏”’/* ÍÎ˝ÓÔÒÚÆ¸˛Ç◊ı˜Â¯˘¿\\n*/" + "ĀāĒēĪīŌōŪū";
      String apiKey = Environment.GetEnvironmentVariable("API_KEY");
      Hashtable ht = new Hashtable();
      ht.Add("q", "Coffee");
      ht.Add("location", "Austin, Texas, United States");
      ht.Add("hl", "en");
      ht.Add("tbs", "qdr:d");
      ht.Add("num", "10");
      ht.Add("google_domain", "google.com");
      ht.Add("engine", "google");
      ht.Add("api_key", apiKey);
      SerpApi client = new SerpApi(ht);
      client.defaultParameter.Remove("q");
      client.defaultParameter.Remove("location");
      client.defaultParameter.Remove("hl");
      client.defaultParameter.Remove("tbs");
      client.defaultParameter.Remove("num");
      client.defaultParameter.Remove("google_domain");
      client.defaultParameter.Add("q", q);

      string encoded = client.createUrl("/search", client.defaultParameter, false);
      string qEncoded = "q=%c2%a1+%e2%84%a2%c2%a3%c2%a2%e2%88%9e%c2%a7%c2%b6%e2%80%a2%c2%aa%c2%ba%e2%80%93%e2%89%a0%c5%93%e2%88%91%c2%b4%c2%ae%e2%80%a0%c2%a5%c2%a8%cb%86%c3%b8%cf%80%e2%80%9c%e2%80%98%c2%ab%c3%a5%c3%9f%e2%88%82%c6%92%c2%a9%c3%a5%c3%9f%e2%88%82%c6%92%c2%a9%cb%9a%c2%ac%e2%80%a6%c3%a6%e2%89%88%c3%a7%e2%88%9a%e2%88%ab%cb%9c%c2%b5%e2%89%a4%e2%89%a5%c3%b7%60%e2%81%84%e2%82%ac%e2%80%b9%e2%80%ba%ef%ac%81%ef%ac%82%e2%80%a1%c2%b0%c2%b7%e2%80%9a%e2%80%94%c2%b1%c5%92%e2%80%9e%c2%b4%e2%80%b0%cb%87%c3%81%c2%a8%c3%98%e2%88%8f%e2%80%9d%e2%80%99%2f*+%c3%8d%c3%8e%cb%9d%c3%93%c3%94%ef%a3%bf%c3%92%c3%9a%c3%86%c2%b8%cb%9b%c3%87%e2%97%8a%c4%b1%cb%9c%c3%82%c2%af%cb%98%c2%bf%5cn*%2f%c4%80%c4%81%c4%92%c4%93%c4%aa%c4%ab%c5%8c%c5%8d%c5%aa%c5%ab";
      Assert.IsTrue(encoded.Contains(qEncoded));
    }

    [TestMethod]
    public void TestSearchModifiedSpecialCharacters()
    {
      String apiKey = Environment.GetEnvironmentVariable("API_KEY");
      Hashtable ht = new Hashtable();
      ht.Add("q", "(h)wīt ˈkôfē !");
      ht.Add("engine", "google");
      ht.Add("api_key", apiKey);
      SerpApi client = new SerpApi(ht);
      JObject data = client.search(ht);
      dynamic first = ((JArray)data["organic_results"])[0];
      Assert.AreEqual<string>((string)first["position"], "1");

      // modified request
      ht.Remove("q");
      ht.Add("q", "blak ˈkôfē !");

      // run client 2
      JObject data_ = client.search(ht);
      dynamic first_ = ((JArray)data_["organic_results"])[0];
      Assert.AreEqual<string>((string)first_["position"], "1");

      // check output is different
      Assert.AreNotEqual<string>((string)first_["title"], (string)first["title"]);

      // Release socket
      client.Close();
    }
  }

}