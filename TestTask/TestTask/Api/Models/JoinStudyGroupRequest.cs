using TestAppApi.Models;

namespace TestTask.Api.Models
{
    public class JoinStudyGroupRequest
    {
        public JoinStudyGroupRequest(User user, string groupName)
        {
            User = user;
            GroupName = groupName;
        }

        public User User { get; }
        public string GroupName { get; }
    }
}
