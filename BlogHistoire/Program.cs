using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScrapySharp.Network;
using HtmlAgilityPack;
using ScrapySharp.Extensions;
using System.Web;
using System.Xml;

namespace BlogHistoire
{
    class item
    {
        public string titre { get; set; }
        public string pageUrl { get; set; }
        public string mp3Url { get; set; }
        public string auteur { get; set; }
        public DateTime datePublished { get; set; }

        public item(string titre, string pageUrl, string auteur, DateTime datePubliee)
        {
            this.titre = titre;
            this.pageUrl = pageUrl;
            this.auteur = auteur;
            this.datePublished = datePubliee;
        }
    }

    class Program
    {

        static ScrapingBrowser sb = new ScrapingBrowser();
        public static  List<item> ListeEmissions;
        
        /// <summary>
        /// Point d'entrée.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            ListeEmissions = new List<item>();
            ScrapeblogAuComplet();
            int i = 1;
            foreach (var item in ListeEmissions)
            {
                ScrapePourTrouverMP3(item);
                Console.WriteLine(i++.ToString() +" - "+  item.titre + " - done!");
            }
                

            ecrireXML();
        }

        /// <summary>
        /// Recherche le lien mp3 dans la page actuelle.
        /// </summary>
        /// <param name="item"></param>
        private static void ScrapePourTrouverMP3(item item)
        {
            var uri = new Uri(item.pageUrl);
            WebPage PageResult = sb.NavigateToPage(uri);

            var audio = PageResult.Html.CssSelect(".wp-audio-shortcode").First();

            //Get links...
            var anchors = audio.Descendants("a").ToList();
            if (anchors.Count > 0)
            {
                var url = anchors[0].Attributes["href"].Value;
                item.mp3Url = url;
            }
        }

        /// <summary>
        /// Scanne le site à la recherche des lists d'épisodes.
        /// </summary>
        private static void ScrapeblogAuComplet()
        {
            var uri = new Uri("http://blog-histoire.fr/liste-des-episodes-de-2000-ans-dhistoire");
            sb.AllowAutoRedirect = false;
            sb.AllowMetaRedirect = false;
            WebPage PageResult = sb.NavigateToPage(uri);
            var Table = PageResult.Html.CssSelect(".entry").First();
            foreach (var row in Table.SelectNodes("p"))
                ScrapeÉpisodes(row);
            
        }

        private static void ScrapeÉpisodes(HtmlNode root)
        {
            // root = "<p>1999 08 30 &gt; <a href=\"http://blog-histoire.fr/2000-ans-histoire/1722-attila.html\">Attila</a> &#8211; Pierre Riché</p>");
            var completeString = HttpUtility.HtmlDecode(root.InnerText);
            if (string.IsNullOrWhiteSpace(completeString))
                return;

            //Récupère la date.
            DateTime date = DateTime.MinValue;
            if (completeString.Contains('>'))
                date = DateTime.Parse(completeString.Split('>')[0]);

            //Récupère l'auteur..
            var auteur = string.Empty;
            if (completeString.Contains('–'))
                auteur = completeString.Split('–')[1];


            //Obtient les liens...
            var anchors = root.Descendants("a").ToList();
            if (anchors.Count > 0)
            {
                var url = anchors[0].Attributes["href"].Value;
                var titre = anchors[0].InnerText;

                ListeEmissions.Add(new item(titre, url, auteur, date));
            }

        }

        /// <summary>
        ///  Génère un simple fichier RSS.
        ///  From http://www.dailycoding.com/posts/create_rss_feed_programatically_from_data_in_c.aspx
        /// </summary>
        static void ecrireXML()
        {
            XmlTextWriter feedWriter = new XmlTextWriter(@"..\..\Episodes.xml", Encoding.UTF8);
            feedWriter.Formatting = Formatting.Indented;
            feedWriter.WriteStartDocument();
           
            // These are RSS Tags
            feedWriter.WriteStartElement("rss");
            feedWriter.WriteAttributeString("version", "2.0");
            feedWriter.WriteStartElement("channel");
            feedWriter.WriteElementString("title", "2000 ans d’histoire");
            feedWriter.WriteElementString("link", "http://blog-histoire.fr/");
            feedWriter.WriteElementString("description", "L'Histoire en podcast");
            feedWriter.WriteElementString("lastBuildDate", DateTime.Now.ToString());
            //feedWriter.WriteElementString("copyright",
            //  "Copyright 2008 dailycoding.com. All rights reserved.");

            foreach (var item in ListeEmissions)
            {
                feedWriter.WriteStartElement("item");
                feedWriter.WriteElementString("title", item.titre);
                feedWriter.WriteElementString("description", item.titre);
                feedWriter.WriteElementString("link",item.mp3Url);
                feedWriter.WriteElementString("pubDate",item.datePublished.ToString());
                feedWriter.WriteEndElement();
            }

            feedWriter.WriteEndElement();
            feedWriter.WriteEndDocument();
            feedWriter.Flush();
            feedWriter.Close();
        }
    }
}
