using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading.Tasks;

namespace JumiaWebScraper
{
    public partial class Form1 : Form
    {
        string webUrl = "https://www.jumia.com.tn/";
        async Task<List<string>> GetLinks()
        {
            var doc = await GetHtmlDocument(webUrl + txtUrl.Text);

            //extract liens articles 1ere page
            var hrefArticles = new List<string>();
            foreach (var item in doc.DocumentNode.SelectNodes("//article[@class='prd _fb col c-prd']/a"))
                hrefArticles.Add(item.Attributes["href"].Value);

            //nombre de pages à parcourir
            var nbArticlesText = doc.DocumentNode.SelectSingleNode("//p [@class=\"-gy5 -phs\"]");
            int nbArticles = Int32.Parse(nbArticlesText.InnerText.Substring(0, nbArticlesText.InnerText.IndexOf(" ")));
            int nbPages = (int)(((nbArticles / 40) > 50) ? 50 : Math.Round(Convert.ToDecimal(nbArticles / 40)));

            for (int i = 0; i < nbPages ; i++)
            {
                txtStatus.Text = $"charging page {i + 2}/ {nbPages}";
                var doc1 = await GetHtmlDocument(webUrl + doc.DocumentNode.SelectSingleNode("//a[@aria-label=\"Page suivante\"]").Attributes["href"].Value);
                //string link = await client.DownloadStringTaskAsync(webUrl + doc.DocumentNode.SelectSingleNode("//a[@aria-label=\"Page suivante\"]").Attributes["href"].Value);
                //doc.LoadHtml(link);
                foreach (var item in doc1.DocumentNode.SelectNodes("//*[@class='core']/@href"))
                    hrefArticles.Add(item.Attributes["href"].Value);
            }
            return hrefArticles;
        }

        async Task<Article> GetArticleAsync(string url)
        {
            var doc = await GetHtmlDocument(webUrl + url);
            var article = new Article();
            article.Name = doc.DocumentNode.SelectSingleNode("//h1/@class")?.InnerText.Replace("&amp", " ");
            article.Price = doc.DocumentNode.SelectSingleNode("//span[@dir=\"ltr\"]")?.InnerText.Replace(";", " ");
            article.Sku = doc.DocumentNode.SelectSingleNode("//*[text()=\"SKU\"]//following-sibling::text()")?.InnerText.Substring(2);
            article.Color = doc.DocumentNode.SelectSingleNode("//*[text()=\"Couleur\"]//following-sibling::text()")?.InnerText.Substring(2);
            article.Model = doc.DocumentNode.SelectSingleNode("//*[text()=\"Modèle\"]//following-sibling::text()")?.InnerText.Substring(2);
            article.Weight = doc.DocumentNode.SelectSingleNode("//*[text()=\"Poids (kg)\"]//following-sibling::text()")?.InnerText.Substring(2);

            //télécharger image correspondante
            WebClient client = new WebClient();
            string imageurl = doc.DocumentNode.SelectSingleNode("//img[@class=\"-fw -fh\"]").Attributes["data-src"].Value;
            client.DownloadFile(imageurl, article.Sku + ".jpg");
            return article;
        }
        public async Task Work()
        {
            var sw = new Stopwatch();
            sw.Start();
            var hrefArticles = await GetLinks();

            //creation Stringbuilder
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Nom;Prix;SKU;Couleur;Model;Poids");
            var tasks = new List<Task<Article>>();

            for (int i = 0; i < hrefArticles.Count-1 ; i++)
            {
                progressBar.Value = (i+1) * 100 / (hrefArticles.Count-1);
                lblprct.Text = $"{(i + 1) * 100 / (hrefArticles.Count - 1)} %";

                txtStatus.Text = $"{i + 1} article / {hrefArticles.Count} articles";
                
                tasks.Add(GetArticleAsync(hrefArticles[i]));
                if (tasks.Count == 5)
                {
                    var completedTask = await Task.WhenAny(tasks);
                    var article = await completedTask;
                    sb.AppendLine(article.Concat());
                    tasks.Remove(completedTask);
                }
            }
            var compltedTasks = await Task.WhenAll(tasks);
            foreach (var article in compltedTasks)
            {
                sb.AppendLine(article.Concat());
            }
            sw.Stop();
            File.WriteAllText(@"C:\Mon Travail\Articles.csv", sb.ToString());
            txtStatus.Text = $"Download complete successfully!!! {hrefArticles.Count} Articles {sw.Elapsed}";

        }
        private async Task<HtmlAgilityPack.HtmlDocument> GetHtmlDocument(string Url)
        {
            var client = new WebClient();
            string webCode = await client.DownloadStringTaskAsync(Url);
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(webCode);
            return doc;
        }
        public Form1()
        {
            InitializeComponent();
        }
        private async void button1_Click(object sender, EventArgs e)
        {
            await Work();
        }
    }
}
