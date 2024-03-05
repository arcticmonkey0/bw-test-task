using Microsoft.AspNetCore.Mvc;
using TestAppApi.Models;
using TestAppApi.Repos;

namespace TestAppApi.Controllers
{
    public class StudyGroupController
    {
        private readonly IStudyGroupRepository _studyGroupRepository;
        public StudyGroupController(IStudyGroupRepository studyGroupRepository)
        {
            _studyGroupRepository = studyGroupRepository;
        }
        public async Task<IActionResult> CreateStudyGroup(StudyGroup studyGroup)
        {
            if (string.IsNullOrWhiteSpace(studyGroup.Name) || studyGroup.Name.Length < 5 || studyGroup.Name.Length > 30)
            {
                return new BadRequestObjectResult("Group name must be between 5-30 characters.");
            }
            await _studyGroupRepository.CreateStudyGroup(studyGroup);
            return new OkResult();
        }
        public async Task<IActionResult> GetStudyGroups()
        {
            var studyGroups = await _studyGroupRepository.GetStudyGroups();
            return new OkObjectResult(studyGroups);
        }
        public async Task<IActionResult> SearchStudyGroups(string subject)
        {
            var studyGroups = await _studyGroupRepository.SearchStudyGroups(subject);
            return new OkObjectResult(studyGroups);
        }
        public async Task<IActionResult> JoinStudyGroup(int studyGroupId, int userId)
        {
            bool isUserInGroup = await _studyGroupRepository.IsUserPresentInGroup(studyGroupId, userId);
            if (isUserInGroup)
            {
                return new ConflictObjectResult("User is already inside this study group.");
            }
            await _studyGroupRepository.JoinStudyGroup(studyGroupId, userId);
            return new OkResult();
        }
        public async Task<IActionResult> LeaveStudyGroup(int studyGroupId, int userId)
        {
            await _studyGroupRepository.LeaveStudyGroup(studyGroupId, userId);
            return new OkResult();
        }
    }
}