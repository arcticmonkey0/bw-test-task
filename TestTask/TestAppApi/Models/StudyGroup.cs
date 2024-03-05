using System.ComponentModel.DataAnnotations;

namespace TestAppApi.Models
{
    public class StudyGroup
    {
        public StudyGroup(int studyGroupId, string name, Subject subject, DateTime createDate, List<User> users)
        {
            StudyGroupId = studyGroupId;
            Name = name;
            Subject = subject;
            CreateDate = createDate;
            Users = users;
        }
        //Some logic will be missing to validate values according to acceptance criteria, but imagine it is existing or do it yourself
        public int StudyGroupId { get; }
        public string Name { get; }
        public Subject Subject { get; }
        public DateTime CreateDate { get; }
        public List<User> Users { get; private set; }
        public void AddUser(User user)
        {
            Users.Add(user);
        }
        public void RemoveUser(User user)
        {
            Users.Remove(user);
        }
    }

    public enum Subject
    {
        Math,
        Chemistry,
        Physics 
    }
}
