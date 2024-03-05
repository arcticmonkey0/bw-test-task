namespace TestAppApi.Models
{
    public class User
    {
        public string UserName { get; set; }
        public int UserId { get; set; }
        public User(string name, int userId)
        {
            UserName = name;
            UserId= userId;
        }
    }
}
