namespace JumiaWebScraper
{
    public class Article
    {
        public string Name { get; set; }
        public string Price { get; set; }
        public string Sku { get; set; }
        public string Color { get; set; }
        public string Model { get; set; }
        public string Weight { get; set; }

        public string Concat()
        {
            return string.Concat(Name, ";", Price, ";", Sku, ";", Color, ";", Model, ";", Weight);
        }
    }
}
