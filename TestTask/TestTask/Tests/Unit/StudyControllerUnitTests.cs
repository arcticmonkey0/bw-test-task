using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json;
using TestAppApi.Controllers;
using TestAppApi.Models;
using TestAppApi.Repos;

namespace TestTask.Tests.Unit
{
    [TestFixture(Category = "StudyGroup, Unit")]
    public class StudyControllerUnitTests
    {
        private Mock<IStudyGroupRepository> _studyGroupRepository;
        private StudyGroupController _studyGroupController;
        private int _entityId;
        private List<User> _defaultUser;

        [SetUp]
        public void Setup()
        {
            _studyGroupRepository = new Mock<IStudyGroupRepository>();
            _studyGroupController = new StudyGroupController(_studyGroupRepository.Object);
            _entityId = Guid.NewGuid().GetHashCode();
            _defaultUser = new List<User>() { new("Bill The Tester", _entityId) };
        }

        [TestCase(Subject.Chemistry, "Group")]
        [TestCase(Subject.Physics, "I suspect this is 30 char long")]
        public async Task StudyGroupWithAllowedNameSubjectAndSize_ShouldBeCreated(Subject subject, string groupName)
        {
            var studyGroup = new StudyGroup(_entityId, groupName, subject, DateTime.Now, _defaultUser);
            var result = await _studyGroupController.CreateStudyGroup(studyGroup);
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Is.InstanceOf<OkResult>());
            });
        }

        [Test]
        public async Task StudyGroupWithNoUsers_ShouldBeCreated()
        {
            var studyGroup = new StudyGroup(_entityId, "This is a math group", Subject.Math, DateTime.Now, new List<User>());
            var result = await _studyGroupController.CreateStudyGroup(studyGroup);
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Is.InstanceOf<OkResult>());
            });
        }

        [TestCase("Math")]
        [TestCase("Mathematics study group for Algebra")]
        [TestCase("")]
        public async Task CreateStudyGroup_InvalidNameLength_ShouldFail(string groupName)
        {
            var studyGroup = new StudyGroup(_entityId, groupName, Subject.Math, DateTime.Now, _defaultUser);
            var result = await _studyGroupController.CreateStudyGroup(studyGroup);
            var resultObject = result as BadRequestObjectResult;
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
                Assert.That(resultObject.Value, Is.EqualTo("Group name must be between 5-30 characters."));
            });
        }

        [Test]
        public async Task ListExistingStudyGroups()
        {
            var studyGroups = new List<StudyGroup>
            {
                new(_entityId, "Group 1", Subject.Physics, DateTime.Now.AddDays(-2), _defaultUser),
                new(_entityId+1, "Group 2", Subject.Math, DateTime.Now.AddDays(-3), _defaultUser)
            };
            _studyGroupRepository.Setup(x => x.GetStudyGroups()).ReturnsAsync(studyGroups);
            var result = await _studyGroupController.GetStudyGroups() as OkObjectResult;
            Assert.Multiple(() =>
            {
                Assert.IsNotNull(result);
                Assert.That(result.StatusCode, Is.EqualTo(200));
                Assert.That(result.Value, Is.InstanceOf<IEnumerable<StudyGroup>>());
                var resultValue = result.Value as IEnumerable<StudyGroup>;
                var expectedJson = JsonConvert.SerializeObject(studyGroups);
                var actualJson = JsonConvert.SerializeObject(resultValue);
                Assert.AreEqual(expectedJson, actualJson);
            });
        }

        [TestCase(Subject.Chemistry)]
        [TestCase(Subject.Physics)]
        [TestCase(Subject.Math)]
        public async Task FilterStudyGroupsBySubject_ShouldBePossible(Subject subjectToFilterOn)
        {
            var subject = subjectToFilterOn.ToString();
            var studyGroups = new List<StudyGroup>
            {
                new(_entityId, "Group 1", Subject.Physics, DateTime.Now, _defaultUser),
                new(_entityId+1, "Group 2", Subject.Chemistry, DateTime.Now, _defaultUser),
                new(_entityId+2, "Group 3", Subject.Math, DateTime.Now, _defaultUser)
            };
            _studyGroupRepository.Setup(x => x.SearchStudyGroups(subject)).ReturnsAsync(
                studyGroups.Where(studyGroup => studyGroup.Subject.Equals(subjectToFilterOn)));
            var result = await _studyGroupController.SearchStudyGroups(subject) as OkObjectResult;
            Assert.Multiple(() =>
            {
                Assert.IsNotNull(result);
                Assert.That(result.StatusCode, Is.EqualTo(200));
                Assert.That(result.Value, Is.InstanceOf<IEnumerable<StudyGroup>>());
                var resultValue = result.Value as IEnumerable<StudyGroup>;
                Assert.AreEqual(1, resultValue.Count());
                Assert.AreEqual(subjectToFilterOn, resultValue.First().Subject);
            });
        }

        [TestCase("Phys")]
        [TestCase("PHySIcS")]
        public async Task FilteringWithPartialSubjectNameOrCaseSensitive_ShouldNotReturnResults(string subject)
        {
            _studyGroupRepository.Setup(x => x.SearchStudyGroups(subject)).ReturnsAsync(new List<StudyGroup>());
            var result = await _studyGroupController.SearchStudyGroups(subject) as OkObjectResult;
            Assert.Multiple(() =>
            {
                Assert.IsNotNull(result);
                Assert.That(result.StatusCode, Is.EqualTo(200));
                Assert.That(result.Value, Is.InstanceOf<List<StudyGroup>>());
                var resultValue = result.Value as List<StudyGroup>;
                Assert.AreEqual(0, resultValue.Count);
            });
        }

        [Test]
        public async Task FilterStudyGroupsByInvalidSubject_ShouldNotReturnResults()
        {
            _studyGroupRepository.Setup(x => x.SearchStudyGroups("NonExistingSubject")).ReturnsAsync(new List<StudyGroup>());
            var result = await _studyGroupController.SearchStudyGroups("NonExistingSubject") as OkObjectResult;
            Assert.Multiple(() =>
            {
                Assert.IsNotNull(result);
                Assert.That(result.StatusCode, Is.EqualTo(200));
                var resultValue = result.Value as IEnumerable<StudyGroup>;
                Assert.IsNotNull(resultValue);
                Assert.IsEmpty(resultValue);
            });
        }

        [Test]
        public async Task JoiningGroupWhichUserHasAlreadyJoined_ShouldFail()
        {
            var user = new User("Bob", _entityId);
            var studyGroup = new StudyGroup(_entityId, "This is a chemistry group", Subject.Chemistry, DateTime.Now, _defaultUser);
            _studyGroupRepository.Setup(x => x.IsUserPresentInGroup(studyGroup.StudyGroupId, user.UserId)).ReturnsAsync(true);
            var result = await _studyGroupController.JoinStudyGroup(studyGroup.StudyGroupId, user.UserId);
            Assert.Multiple(() =>
            {
                Assert.IsInstanceOf<ConflictObjectResult>(result);
                var conflictResult = result as ConflictObjectResult;
                Assert.AreEqual("User is already inside this study group.", conflictResult.Value);
            });
        }

        [Test]
        public async Task LeaveStudyGroup_ShouldSucceed()
        {
            var user = new User("Bob", _entityId);
            var studyGroup = new StudyGroup(_entityId, "This is a physics group", Subject.Physics, DateTime.Now, _defaultUser);
            _studyGroupRepository.Setup(x => x.IsUserPresentInGroup(studyGroup.StudyGroupId, user.UserId)).ReturnsAsync(true);
            var result = await _studyGroupController.LeaveStudyGroup(studyGroup.StudyGroupId, user.UserId);
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Is.InstanceOf<OkResult>());
            });
        }
    }
}

