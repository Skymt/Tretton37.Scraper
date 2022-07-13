using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Tretton37
{
    internal class Scraper
    {
        readonly string destination, relativeDestination;
        readonly HttpClient client;
        public Scraper(string destination)
        {
            this.destination = this.relativeDestination = destination.TrimEnd('\\');
            if (Directory.Exists(destination))
                Directory.Delete(destination, true);

            string? rootPath = Path.GetPathRoot(destination);
            if(!string.IsNullOrEmpty(rootPath))
                relativeDestination = '/' + destination.Replace(rootPath, string.Empty);

            client = new() { BaseAddress = new Uri("https://tretton37.com") };
        }

        public async Task Scrape(Action<string> onUpdate)
        {
            Queue<string> toDownload = new();
            HashSet<string> completed = new();
            toDownload.Enqueue("/index.html");

            while(toDownload.Any())
            {
                var path = toDownload.Dequeue();
                onUpdate($"Remaining: {toDownload.Count:000}, now handling {path}.");
                if (completed.Contains(path)) continue;

                string? localFile = PrepareLocalFile(path);
                if (localFile == null) continue;

                var response = await client.GetAsync(path);
                completed.Add(path);

                if (!response.IsSuccessStatusCode) continue;

                var content = response?.Content;
                if (content == null) continue;

                var contentType = content.Headers?.ContentType?.MediaType;
                if (contentType == null) continue;

                switch (contentType)
                {
                    case "text/html":
                    case "text/css":
                        var html = await content.ReadAsStringAsync();
                        var links = SearchForLinks(html).ToList();
                        var fixedHtml = PrependRelativeUrls(html, links);
                        
                        File.WriteAllText(localFile, fixedHtml);
                        links.ForEach(toDownload.Enqueue);
                        break;
                    default: 
                        File.WriteAllBytes(localFile, await content.ReadAsByteArrayAsync());
                        break;
                }
            }
        }
        static string FixPathAndQuery(string text) =>
            (text[0] == '/' ? string.Empty : "/") + text.Split('?')[0];
        string PrependRelativeUrls(string text, IList<string> links)
        {
            var newText = text;
            foreach(var link in links)
                newText = newText.Replace(link, relativeDestination + FixPathAndQuery(link));

            return newText;
        }
        string? PrepareLocalFile(string path)
        {
            string localPath = destination + FixPathAndQuery(path).Replace('/', '\\');
            string? localDirectory = Path.GetDirectoryName(localPath);
            if (string.IsNullOrEmpty(localDirectory)) return null;

            if (!Directory.Exists(localDirectory))
                Directory.CreateDirectory(localDirectory);

            return localPath;
        }
        static IEnumerable<string> SearchForLinks(string text)
        {
            foreach(var potentialLink in SearchForEnclosedStrings(text))
            {
                if (potentialLink.StartsWith("//")) continue;
                if (!potentialLink.Contains('/')) continue;
                if (potentialLink == "/") continue;

                var link = potentialLink.Replace("https://www.tretton37.com", string.Empty);
                if (link.StartsWith("http")) continue;

                if(link[0] != '/' && !link.Contains('.')) continue;
                if (link.Contains('<')) continue;

                link = link.Split('#')[0];

                if (link[0] == '/' && !link.Split('/').Last().Contains('.'))
                    link += "/index.html";
                yield return link;
            }
        }
        static IEnumerable<string> SearchForEnclosedStrings(string text)
        {
            var open = false; string current = string.Empty;
            foreach (var c in text)
            {
                switch (c)
                {
                    case '"':
                        if (!open) open = true;
                        else
                        {
                            yield return current;
                            open = false; current = string.Empty;
                        }
                        break;
                    default: 
                        if (open) 
                            current += c; 
                        break;
                }
            }
        }
    }
}
