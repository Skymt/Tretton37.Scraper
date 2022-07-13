var destination = args.FirstOrDefault();

if(string.IsNullOrEmpty(destination))
{
    destination = @"C:\temp";
    Console.WriteLine($"Save files to {destination}? (Y/N)");
    if (Console.ReadKey(true).Key != ConsoleKey.Y) return;
}

var scraper = new Tretton37.Scraper(destination);

Console.WriteLine($"Saving https://tretton37.com to local folder {destination}");
await scraper.Scrape(onUpdate: update => Console.Write($"\r{update.PadRight(Console.WindowWidth)}"));

Console.WriteLine("Parse complete - open folder? (Y/N)");
if (Console.ReadKey(true).Key != ConsoleKey.Y) return;
System.Diagnostics.Process.Start("explorer.exe", destination);