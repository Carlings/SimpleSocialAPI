namespace SimpleSocialAPI.Data.Models
{
    public class Post
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Body { get; set; } = null!;
        public int AuthorId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
