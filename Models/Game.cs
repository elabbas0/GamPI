namespace GamPI.Models
{
    public class Game
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Genre { get; set; }
        public string Platforms { get; set; }
        public DateTime ReleaseDate { get; set; }
        public double Rating { get; set; }
        public string? ImgURL { get; set; }

        public string Developer { get; set; }

    }
}
