using TestAppApi.Models;

namespace TestTask.Api.Models
{
    internal class CreateStudyGroupRequest
    {
        public CreateStudyGroupRequest(string groupName, string subject, List<User> users)
        {
            GroupName = groupName;
            Subject = subject;
            Users = users;
        }

        public string GroupName { get; }
        public string Subject { get; }
        public List<User> Users { get; }
    }
}
