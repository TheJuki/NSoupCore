﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSoup.Nodes;
using NSoup.Select;
using System.IO;
using System.Reflection;

namespace Test.Integration
{
    /// <summary>
    /// Integration test: parses from real-world example HTML.
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley
    /// Ported to .NET by: Amir Grozki
    /// -->
    [TestClass]
    public class ParseTest
    {
        public ParseTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        [TestMethod]
        public void testSmhBizArticle()
        {
            using (Stream input = getFile("Test.htmltests.smh-biz-article-1.html"))
            {
                Document doc = NSoup.NSoupClient.Parse(input, "UTF-8",
                    "http://www.smh.com.au/business/the-boards-next-fear-the-female-quota-20100106-lteq.html");
                Assert.AreEqual("The board’s next fear: the female quota",
                    doc.Title); // note that the apos input the source is a literal ’ (8217), not escaped or '
                Assert.AreEqual("en", doc.Select("html").Attr("xml:lang"));

                Elements articleBody = doc.Select(".articleBody > *");
                Assert.AreEqual(17, articleBody.Count);
                // todo: more tests!
            }
        }

        [TestMethod]
        public void testNewsHomepage()
        {
            using (Stream input = getFile("Test.htmltests.news-com-au-home.html"))
            {
                Document doc = NSoup.NSoupClient.Parse(input, "UTF-8", "http://www.news.com.au/");
                Assert.AreEqual("News.com.au | News from Australia and around the world online | NewsComAu", doc.Title);
                Assert.AreEqual("Brace yourself for Metro meltdown", doc.Select(".id1225817868581 h4").Text.Trim());

                Element a = doc.Select("a[href=/entertainment/horoscopes]").First;
                Assert.AreEqual("/entertainment/horoscopes", a.Attr("href"));
                Assert.AreEqual("http://www.news.com.au/entertainment/horoscopes", a.Attr("abs:href"));

                Element hs = doc.Select("a[href*=naughty-corners-are-a-bad-idea]").First;
                Assert.AreEqual(
                    "http://www.heraldsun.com.au/news/naughty-corners-are-a-bad-idea-for-kids/story-e6frf7jo-1225817899003",
                    hs.Attr("href"));
                Assert.AreEqual(hs.Attr("href"), hs.Attr("abs:href"));
            }
        }

        [TestMethod]
        public void testGoogleSearchIpod()
        {
            using (Stream input = getFile("Test.htmltests.google-ipod.html"))
            {
                Document doc = NSoup.NSoupClient.Parse(input, "UTF-8", "http://www.google.com/search?hl=en&q=ipod&aq=f&oq=&aqi=g10");
                Assert.AreEqual("ipod - Google Search", doc.Title);
                Elements results = doc.Select("h3.r > a");
                Assert.AreEqual(12, results.Count);
                Assert.AreEqual(
                    "http://news.google.com/news?hl=en&q=ipod&um=1&ie=UTF-8&ei=uYlKS4SbBoGg6gPf-5XXCw&sa=X&oi=news_group&ct=title&resnum=1&ved=0CCIQsQQwAA",
                    results[0].Attr("href"));
                Assert.AreEqual("http://www.apple.com/itunes/",
                    results[1].Attr("href"));
            }
        }

        [TestMethod]
        public void testBinary()
        {
            using (Stream input = getFile("Test.htmltests.thumb.jpg"))
            {
                Document doc = NSoup.NSoupClient.Parse(input, "UTF-8");
                // nothing useful, but did not blow up
                Assert.IsTrue(doc.Text().Contains("gd-jpeg"));
            }
        }

        [TestMethod]
        public void testYahooJp()
        {
            using (Stream input = getFile("Test.htmltests.yahoo-jp.html"))
            {
                Document doc = NSoup.NSoupClient.Parse(input, "UTF-8", "http://www.yahoo.co.jp/index.html"); // http charset is utf-8.
                Assert.AreEqual("Yahoo! JAPAN", doc.Title);
                Element a = doc.Select("a[href=t/2322m2]").First;
                Assert.AreEqual("http://www.yahoo.co.jp/_ylh=X3oDMTB0NWxnaGxsBF9TAzIwNzcyOTYyNjUEdGlkAzEyBHRtcGwDZ2Ex/t/2322m2",
                    a.Attr("abs:href")); // session put into <base>
                Assert.AreEqual("全国、人気の駅ランキング", a.Text());
            }
        }

        [TestMethod]
        public void testBaidu()
        {
            // tests <meta http-equiv="Content-Type" content="text/html;charset=gb2312">
            using (Stream input = getFile("Test.htmltests.baidu-cn-home.html"))
            {
                Document doc = NSoup.NSoupClient.Parse(input, null,
                    "http://www.baidu.com/"); // http charset is gb2312, but NOT specifying it, to test http-equiv parse
                Element submit = doc.Select("#su").First;
                Assert.AreEqual("百度一下", submit.Attr("value"));

                // test from attribute match
                submit = doc.Select("input[value=百度一下]").First;
                Assert.AreEqual("su", submit.Id);
                Element newsLink = doc.Select("a:contains(新)").First;
                //Assert.AreEqual("http://news.baidu.com", newsLink.AbsUrl("href"));
                Assert.AreEqual("http://news.baidu.com/", newsLink.AbsUrl("href")); // This was changed due to System.Uri's behavior. I think this change is acceptable.

                // check auto-detect from meta
                Assert.AreEqual("GB2312", doc.OutputSettings().Encoding.WebName.ToUpperInvariant());
                Assert.AreEqual("<title>百度一下，你就知道      </title>", doc.Select("title").OuterHtml());

                doc.OutputSettings().SetEncoding("ascii");
                Assert.AreEqual("<title>&#30334;&#24230;&#19968;&#19979;&#65292;&#20320;&#23601;&#30693;&#36947;      </title>",
                    doc.Select("title").OuterHtml());
            }
        }

        [TestMethod]
        public void testBaiduVariant()
        {
            // tests <meta charset> when preceded by another <meta>
            Stream input = getFile("Test.htmltests.baidu-variant.html");
            Document doc = NSoup.NSoupClient.Parse(input, null,
                "http://www.baidu.com/"); // http charset is UTF-8, but NOT specifying it, to test http-equiv parse
            // check auto-detect from meta
            Assert.AreEqual("UTF-8", doc.OutputSettings().Encoding.WebName.ToUpperInvariant());
            Assert.AreEqual("<title>百度一下，你就知道</title>", doc.Select("title").OuterHtml());
        }

        [TestMethod]
        public void testHtml5Charset()
        {
            // test that <meta charset="UTF-8"> works
            using (Stream input = getFile("Test.htmltests.meta-charset-1.html"))
            {
                Document doc = NSoup.NSoupClient.Parse(input, null, "http://example.com/"); //UTF-8, has html5 <meta charset>
                Assert.AreEqual("新", doc.Text());
                Assert.AreEqual("UTF-8", doc.OutputSettings().Encoding.WebName.ToUpperInvariant());
            }
            // double check, no charset, falls back to utf8 which is incorrect
            using (Stream input = getFile("Test.htmltests.meta-charset-2.html"))
            {
                Document doc = NSoup.NSoupClient.Parse(input, null, "http://example.com"); // UTF-8, no charset
                Assert.AreEqual("UTF-8", doc.OutputSettings().Encoding.WebName.ToUpperInvariant());
                Assert.IsFalse("新".Equals(doc.Text()));
            }
            // confirm fallback to utf8
            using (Stream input = getFile("Test.htmltests.meta-charset-3.html"))
            {
                Document doc = NSoup.NSoupClient.Parse(input, null, "http://example.com/"); // utf8, no charset
                Assert.AreEqual("UTF-8", doc.OutputSettings().Encoding.WebName.ToUpperInvariant());
                Assert.AreEqual("新", doc.Text());
            }
        }

        [TestMethod]
        public void testNytArticle()
        {
            // has tags like <nyt_text>
            using (Stream input = getFile("Test.htmltests.nyt-article-1.html"))
            {
                Document doc = NSoup.NSoupClient.Parse(input, null, "http://www.nytimes.com/2010/07/26/business/global/26bp.html?hp");

                Element headline = doc.Select("nyt_headline[version=1.0]").First();
                Assert.AreEqual("As BP Lays Out Future, It Will Not Include Hayward", headline.Text());
            }
        }

        [TestMethod]
        public void testYahooArticle()
        {
            Stream input = getFile("Test.htmltests.yahoo-article-1.html");
            Document doc = NSoup.NSoupClient.Parse(input, "UTF-8", "http://news.yahoo.com/s/nm/20100831/bs_nm/us_gm_china");
            Element p = doc.Select("p:contains(Volt will be sold in the United States").First;
            Assert.AreEqual("In July, GM said its electric Chevrolet Volt will be sold in the United States at $41,000 -- $8,000 more than its nearest competitor, the Nissan Leaf.", p.Text());
        }

        Stream getFile(string resourceName)
        {
            try
            {
                var assembly = typeof(ParseTest).GetTypeInfo().Assembly;
                return assembly.GetManifestResourceStream(resourceName);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}