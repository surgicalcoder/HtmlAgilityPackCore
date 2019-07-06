using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace HtmlAgilityPackCore.Tests
{
    [TestFixture]
    public class QuerySelectorTest
    {
        private static readonly HtmlDocument Doc = LoadHtml();

        [Test]
        public void IdSelectorMustReturnOnlyFirstElement()
        {
            var elements = Doc.QuerySelectorAll("#myDiv");
            Assert.IsTrue(elements.Count == 1);
            Assert.IsTrue(elements[0].Id == "myDiv");
            Assert.IsTrue(elements[0].Attributes["first"].Value == "1");
        }

        [Test]
        public void GetElementsByAttribute()
        {
            var elements = Doc.QuerySelectorAll("*[id=myDiv]");

            Assert.IsTrue(elements.Distinct().Count() == 2 && elements.Count == 2);
            foreach (HtmlNode node in elements)
                Assert.IsTrue(node.Id == "myDiv");
        }

        [Test]
        public void GetElementsByClassName1()
        {
            var elements1 = Doc.QuerySelectorAll(".cls-a");
            var elements2 = Doc.QuerySelectorAll(".clsb");

            Assert.IsTrue(elements1.Count == 1);
            for (int i = 0; i < elements1.Count; i++)
                Assert.IsTrue(elements1[i] == elements2[i]);
        }

		[Test]
        public void GetElementsByClassName_MultiClasses()
        {
            var elements = Doc.QuerySelectorAll(".cls-a, .cls-b");

            Assert.IsTrue(elements.Count == 2);
            Assert.IsTrue(elements[0].Id == "spanA");
            Assert.IsTrue(elements[1].Id == "spanB");
        }

		[Test]
        public void GetElementsByClassName_WithUnderscore()
        {
            var elements = Doc.QuerySelectorAll(".underscore_class");

            Assert.IsTrue(elements.Count == 1);
            Assert.IsTrue(elements[0].Id == "spanB");
        }

		[Test]
        public void GetElementsWithTwoClasses()
        {
            var elements = Doc.QuerySelectorAll(".active.lv1");

            Assert.IsTrue(elements.Count == 1);
            Assert.AreEqual(elements[0].InnerText.ToString(), "L12");
        }

		[Test]
        public void GetElementsByClassInsideClass()
        {
            var elements = Doc.QuerySelectorAll(".lv0 .lv1");

            Assert.IsTrue(elements.Count == 3);
            Assert.AreEqual(elements[1].InnerText.ToString(), "L12");
        }

		[Test]
        public void GetElementsByClassInsideId()
        {
            var elements = Doc.QuerySelectorAll("#ul .lv1");

            Assert.IsTrue(elements.Count == 3);
            Assert.AreEqual(elements[1].InnerText.ToString(), "L12");
        }

		[Test]
        public void GetElementsWithoutComments()
        {
            var element = Doc.QuerySelector("#with-comments");
            string text = element.ChildNodes
                .Where(d => d.NodeType == HtmlNodeType.Element)
                .SelectMany(d => d.ChildNodes)
                .Where(d => d.NodeType != HtmlNodeType.Comment)
                .Aggregate("", (s, n) => s + n.InnerHtml.ToString());
            Assert.IsTrue(text == "Hello World!");
        }

        [Test]
        public void GetElementsWithRelaxedStartsWithFilter()
        {
            var matches = Doc.QuerySelectorAll("#relaxed-starts-with-tests > p[class^=\"match\"]");
            Assert.IsTrue(matches.Any() && matches.All(m => m.InnerText.ToString().Equals("Match")));
            var mismatches = matches.First().ParentNode.ChildNodes.Where(n => n.Name == "p").Except(matches);
            Assert.IsTrue(mismatches.Any() && mismatches.All(n => n.InnerText.ToString().Equals("NoMatch")));
        }

        [Test]
        public void GetElementsWithStrictStartsWithFilter()
        {
            var matches = Doc.QuerySelectorAll("#strict-starts-with-tests > p[class|=\"match\"]");
            Assert.IsTrue(matches.Any() && matches.All(m => m.InnerText.ToString().Equals("Match")));
            var mismatches = matches.First().ParentNode.ChildNodes.Where(n => n.Name == "p").Except(matches);
            Assert.IsTrue(mismatches.Any() && mismatches.All(n => n.InnerText.ToString().Equals("NoMatch")));

            // Test as well when te provided filter ends with a dash
            matches = Doc.QuerySelectorAll("#strict-starts-with-tests-trailing-dash > p[class|=\"match-\"]");
            Assert.IsTrue(matches.Any() && matches.All(m => m.InnerText.ToString().Equals("Match")));
            mismatches = matches.First().ParentNode.ChildNodes.Where(n => n.Name == "p").Except(matches);
            Assert.IsTrue(mismatches.Any() && mismatches.All(n => n.InnerText.ToString().Equals("NoMatch")));
        }

        [Test]
        public void GetElementsWithEndingBracketFollowedByClassName()
        {
            var matches = Doc.QuerySelectorAll("#ending-bracket-followed-by-class-test > a[href$=\".pdf\"].match");
            Assert.IsTrue(matches.Any() && matches.All(m => m.InnerText.ToString().Equals("Match")));
            var mismatches = matches.First().ParentNode.ChildNodes.Where(n => n.Name == "a").Except(matches);
            Assert.IsTrue(mismatches.Any() && mismatches.All(n => n.InnerText.ToString().Equals("NoMatch")));
        }

        [Test]
        public void GetElementsByClassName_WithWhitespace()
        {
            var elements = Doc.QuerySelectorAll(".whitespace");
            Assert.IsNotNull(elements.Count == 3);
        }

        private static HtmlDocument LoadHtml()
        {
            var htmlDocument = new HtmlDocument();
            var path = Path.GetDirectoryName(typeof(QuerySelectorTest).Assembly.Location).ToString() + "\\files\\";
            htmlDocument.LoadHtml(File.ReadAllText(Path.Combine(path, "CSS-Selector-Test-1.html"), Encoding.UTF8)).Wait();

            return htmlDocument;
        }

    }
}
