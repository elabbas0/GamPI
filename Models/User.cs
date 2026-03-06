namespace GamPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; } 
        public string PasswordSalt { get; set; }

        public readonly List<Game> FavoriteGames = new List<Game>();
        public string role { get; set; } = "User";
    }
}
