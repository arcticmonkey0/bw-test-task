using Microsoft.Extensions.Configuration;
using RestSharp;
using TestAppApi.Models;
using System.Net;
using Newtonsoft.Json;
using TestTask.TestData;
using TestTask.Api.Models;

namespace TestTask.Tests.Component
{
    [TestFixture(Category = "StudyGroup, Component")]
    public class StudyControllerComponentTests
    {
        private const string EndpointsConfigSection = "Endpoints";
        private const string CreateStudyGroupEndpointName = "CreateStudyGroup";
        private const string FilterStudyGroupsEndpointName = "FilterStudyGroups";
        private const string LeaveStudyGroupEndpointName = "LeaveStudyGroup";
        private const string GetStudyGroupsEndpointName = "GetStudyGroups";
        private const string JoinStudyGroupEndpointName = "JoinStudyGroup";
        private int _entityId;
        private IConfiguration _config;
        private RestClient _client;
        private List<User> _defaultUser;

        [SetUp]
        public void Setup()
        {
            _entityId = Guid.NewGuid().GetHashCode();
            _config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
            _client = new RestClient(_config.GetValue<string>("BaseUrl"));
            _defaultUser = new List<User>()
                {
                    new User("Bill The Tester", _entityId)
                };
        }

        [TearDown]
        public void CleanUp()
        {
            TestDataHelper.CleanUpTestDataCreated();
            _client.Dispose();
        }

        [Test]
        public void MultipleGroupsOfSameSubject_ShouldNotBeCreated()
        {
            var subject = "Math";
            var createStudyGroupRequestBody = new CreateStudyGroupRequest(
                    "Group 1", subject, _defaultUser);
            var createStudyGroupOfSameSubjectRequestBody = new CreateStudyGroupRequest(
                    "Group 2", subject, _defaultUser);
            var createStudyGroupRequest = new RestRequest(_config.GetSection(EndpointsConfigSection).GetValue<string>(CreateStudyGroupEndpointName), Method.Post)
                .AddJsonBody(createStudyGroupRequestBody);
            var createDuplicatedStudyGroupRequest = new RestRequest(_config.GetSection(EndpointsConfigSection).GetValue<string>(CreateStudyGroupEndpointName), Method.Post)
                .AddJsonBody(createStudyGroupOfSameSubjectRequestBody);
            RestResponse createGroupResponse = _client.Execute(createStudyGroupRequest);
            RestResponse duplicatedStudyGroupCreationAttemptResponse = _client.Execute(createDuplicatedStudyGroupRequest);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(HttpStatusCode.OK, createGroupResponse.StatusCode);
                Assert.AreEqual(HttpStatusCode.Conflict, duplicatedStudyGroupCreationAttemptResponse.StatusCode);
                Assert.IsTrue(duplicatedStudyGroupCreationAttemptResponse.Content.Contains($"A study group by {subject} subject already exists."));
            });
        }

        [Test]
        public void MultipleGroupsOfSameName_ShouldNotBeCreated()
        {
            var studyGroupName = "Math";
            var createStudyGroupRequestBody = new CreateStudyGroupRequest(
                    studyGroupName, "Physics", _defaultUser);
            var createStudyGroupOfSameSubjectRequestBody = new CreateStudyGroupRequest(
                    studyGroupName, "Chemistry", _defaultUser);
            var createStudyGroupRequest = new RestRequest(_config.GetSection(EndpointsConfigSection).GetValue<string>(CreateStudyGroupEndpointName), Method.Post)
                .AddJsonBody(createStudyGroupRequestBody);
            var createDuplicatedStudyGroupRequest = new RestRequest(_config.GetSection(EndpointsConfigSection).GetValue<string>(CreateStudyGroupEndpointName), Method.Post)
                .AddJsonBody(createStudyGroupOfSameSubjectRequestBody);
            _client.Execute(createStudyGroupRequest);
            RestResponse duplicatedStudyGroupCreationAttemptResponse = _client.Execute(createDuplicatedStudyGroupRequest);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(HttpStatusCode.Conflict, duplicatedStudyGroupCreationAttemptResponse.StatusCode);
                Assert.IsTrue(duplicatedStudyGroupCreationAttemptResponse.Content.Contains($"A study group with {studyGroupName} name already exists."));
            });
        }

        [Test]
        public void CreatingStudyGroupWithInvalidSubject_ShouldFail()
        {
            var invalidInputSubject = "Mathz";
            var createStudyGroupRequestBody = new CreateStudyGroupRequest(
                "Non-existing group", invalidInputSubject, new List<User>());
            var createStudyGroupRequest = new RestRequest(_config.GetSection(EndpointsConfigSection).GetValue<string>(CreateStudyGroupEndpointName), Method.Post)
                .AddJsonBody(createStudyGroupRequestBody);
            RestResponse invalidSubjectCreationAttemptResponse = _client.Execute(createStudyGroupRequest);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, invalidSubjectCreationAttemptResponse.StatusCode);
                Assert.AreEqual(invalidSubjectCreationAttemptResponse.Content,
                    $"Study group with {invalidInputSubject} subject can not be created as {invalidInputSubject} is invalid value.");
            });
        }

        [Test]
        public void VerifyCreationDateIsRecordedForCreatedGroup()
        {
            var createStudyGroupRequestBody = new CreateStudyGroupRequest(
                "A group of Math", "Math", new List<User>());
            var createStudyGroupRequest = new RestRequest(_config.GetSection(EndpointsConfigSection).GetValue<string>(CreateStudyGroupEndpointName), Method.Post)
                .AddJsonBody(createStudyGroupRequestBody);
            var filterStudyGroupsRequest = new RestRequest(_config.GetSection(EndpointsConfigSection).GetValue<string>(FilterStudyGroupsEndpointName), Method.Get)
                .AddParameter("subject", "Math");
            var creationTime = DateTime.Now;
            _client.Execute(createStudyGroupRequest);
            RestResponse filterStudyGroupsResponse = _client.Execute(filterStudyGroupsRequest);
            var studyGroups = JsonConvert.DeserializeObject<List<StudyGroup>>(filterStudyGroupsResponse.Content);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(HttpStatusCode.OK, filterStudyGroupsResponse.StatusCode);
                Assert.That(studyGroups.First().CreateDate, Is.EqualTo(creationTime).Within(5).Seconds);
            });
        }

        [Test]
        public void UserLeavesTheGroup_LastUserLeavingTheGroupDoesNotDeleteGroup()
        {
            var userToLeave = _defaultUser.First();
            var createStudyGroupRequestBody = new CreateStudyGroupRequest(
                "Group 1", "Physics", _defaultUser);
            var createStudyGroupRequest = new RestRequest(_config.GetSection(EndpointsConfigSection).GetValue<string>(CreateStudyGroupEndpointName), Method.Post)
                .AddJsonBody(createStudyGroupRequestBody);
            var leaveStudyGroupRequest = new RestRequest(_config.GetSection(EndpointsConfigSection).GetValue<string>(LeaveStudyGroupEndpointName), Method.Post)
                .AddParameter("groupName", createStudyGroupRequestBody.GroupName)
                .AddParameter("userId", userToLeave.UserId);
            var getStudyGroupsRequest = new RestRequest(_config.GetSection(EndpointsConfigSection).GetValue<string>(GetStudyGroupsEndpointName), Method.Get);
            _client.Execute(createStudyGroupRequest);
            RestResponse leaveStudyGroupResponse = _client.Execute(leaveStudyGroupRequest);
            RestResponse getStudyGroupsResponse = _client.Execute(getStudyGroupsRequest);
            Assert.AreEqual(HttpStatusCode.OK, getStudyGroupsResponse.StatusCode);
            Assert.Multiple(() =>
            {
                Assert.That(leaveStudyGroupResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                IEnumerable<StudyGroup> existingStudyGroups = JsonConvert.DeserializeObject<IEnumerable<StudyGroup>>(getStudyGroupsResponse.Content)
                    .Where(studyGroup => studyGroup.Subject.ToString().Equals(createStudyGroupRequestBody.Subject)
                    && studyGroup.Name.Equals(createStudyGroupRequestBody.GroupName));
                Assert.That(existingStudyGroups, Is.Not.Empty);
                CollectionAssert.DoesNotContain(existingStudyGroups.First().Users, userToLeave);
            });
        }

        [Test]
        public void UserLeavesTheNonExistingGroup()
        {
            var userToLeave = _defaultUser.First();
            var studyGroupName = "nowhereToBeFound";
            var leaveStudyGroupRequest = new RestRequest(_config.GetSection(EndpointsConfigSection).GetValue<string>(LeaveStudyGroupEndpointName), Method.Post)
                .AddParameter("groupName", studyGroupName)
                .AddParameter("userId", userToLeave.UserId);
            RestResponse leaveStudyGroupResponse = _client.Execute(leaveStudyGroupRequest);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, leaveStudyGroupResponse.StatusCode);
                Assert.AreEqual(leaveStudyGroupResponse.Content, $"Can not remove ${userToLeave} from group ${studyGroupName} as this group does not exist");
            });
        }

        [Test]
        public void JoiningDifferentGroupsIsPossible()
        {
            var joiningUser = new User("Bobby", _entityId + 1);
            var createStudyGroupRequestBody = new CreateStudyGroupRequest(
                    "Group 1", "Physics", _defaultUser);
            var createSecondStudyGroupRequestBody = new CreateStudyGroupRequest(
                    "Group 2", "Math", _defaultUser);
            var joinStudyGroupRequestBody = new JoinStudyGroupRequest(joiningUser, createStudyGroupRequestBody.GroupName);
            var joinSecondStudyGroupRequestBody = new JoinStudyGroupRequest(joiningUser, createSecondStudyGroupRequestBody.GroupName);
            var createStudyGroupRequest = new RestRequest(_config.GetSection(EndpointsConfigSection).GetValue<string>(CreateStudyGroupEndpointName), Method.Post)
                .AddBody(createStudyGroupRequestBody);

            var createSecondStudyGroupRequest = new RestRequest(_config.GetSection(EndpointsConfigSection).GetValue<string>(CreateStudyGroupEndpointName), Method.Post)
                .AddBody(createSecondStudyGroupRequestBody);
            var joinStudyGroupRequest = new RestRequest(_config.GetSection(EndpointsConfigSection).GetValue<string>(JoinStudyGroupEndpointName), Method.Put)
                .AddBody(joinStudyGroupRequestBody);
            var joinSecondStudyGroupRequest = new RestRequest(_config.GetSection(EndpointsConfigSection).GetValue<string>(JoinStudyGroupEndpointName), Method.Put)
                .AddBody(joinSecondStudyGroupRequestBody);
            var getStudyGroupsRequest = new RestRequest(_config.GetSection(EndpointsConfigSection).GetValue<string>(GetStudyGroupsEndpointName), Method.Get);

            _client.Execute(createStudyGroupRequest);
            _client.Execute(createSecondStudyGroupRequest);
            RestResponse joinStudyGroupResponse = _client.Execute(joinStudyGroupRequest);
            RestResponse joinSecondStudyGroupResponse = _client.Execute(joinSecondStudyGroupRequest);
            RestResponse getStudyGroupsResponse = _client.Execute(getStudyGroupsRequest);
            List<StudyGroup> existingStudyGroupsAfterJoining = JsonConvert.DeserializeObject<List<StudyGroup>>(getStudyGroupsResponse.Content);

            Assert.Multiple(() =>
            {
                Assert.That(joinStudyGroupResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(joinSecondStudyGroupResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                existingStudyGroupsAfterJoining.ForEach(group => CollectionAssert.Contains(group.Users, joiningUser));
                existingStudyGroupsAfterJoining.ForEach(group => CollectionAssert.Contains(group.Users, _defaultUser.First()));
            });
        }

        [Test]
        public void JoiningOneGroupDoesNotAddUserToOtherGroup()
        {
            var joiningUser = new User("Bobby", _entityId + 1);
            var groupName1 = "Group 1";
            var groupName2 = "Group 2";
            var createStudyGroupRequestBody = new CreateStudyGroupRequest(
                    groupName1, "Physics", _defaultUser);
            var createSecondStudyGroupRequestBody = new CreateStudyGroupRequest(
                    groupName2, "Math", _defaultUser);
            var joinStudyGroupRequestBody = new JoinStudyGroupRequest(joiningUser, createStudyGroupRequestBody.GroupName);
            var createStudyGroupRequest = new RestRequest(_config.GetSection(EndpointsConfigSection).GetValue<string>(CreateStudyGroupEndpointName), Method.Post)
                .AddBody(createStudyGroupRequestBody);
            var createSecondStudyGroupRequest = new RestRequest(_config.GetSection(EndpointsConfigSection).GetValue<string>(CreateStudyGroupEndpointName), Method.Post)
                .AddBody(createSecondStudyGroupRequestBody);
            var getStudyGroupsRequest = new RestRequest(_config.GetSection(EndpointsConfigSection).GetValue<string>(GetStudyGroupsEndpointName), Method.Get);
            var joinStudyGroupRequest = new RestRequest(_config.GetSection(EndpointsConfigSection).GetValue<string>(JoinStudyGroupEndpointName), Method.Put)
                .AddBody(joinStudyGroupRequestBody);

            _client.Execute(createStudyGroupRequest);
            _client.Execute(createStudyGroupRequest);
            RestResponse joinStudyGroupResponse = _client.Execute(joinStudyGroupRequest);
            RestResponse studyGroupsResponse = _client.Execute(getStudyGroupsRequest);
            List<StudyGroup> existingGroupsAfterJoining = JsonConvert.DeserializeObject<List<StudyGroup>>(studyGroupsResponse.Content);
            CollectionAssert.DoesNotContain(existingGroupsAfterJoining.Find(studyGroup => studyGroup.Name.Equals(groupName2)).Users, joiningUser);
        }

        [Test]
        public void LeavingOneGroupDoesNotRemoveUserFromOtherGroup()
        {
            var userToLeave = _defaultUser.First();
            var groupName1 = "Group 1";
            var groupName2 = "Group 2";
            var createStudyGroupRequestBody = new CreateStudyGroupRequest(
                    groupName1, "Physics", _defaultUser);
            var createSecondStudyGroupRequestBody = new CreateStudyGroupRequest(
                    groupName2, "Math", _defaultUser);
            var createStudyGroupRequest = new RestRequest(_config.GetSection(EndpointsConfigSection).GetValue<string>(CreateStudyGroupEndpointName), Method.Post)
                .AddBody(createStudyGroupRequestBody);
            var createSecondStudyGroupRequest = new RestRequest(_config.GetSection(EndpointsConfigSection).GetValue<string>(CreateStudyGroupEndpointName), Method.Post)
                .AddBody(createSecondStudyGroupRequestBody);
            var getStudyGroupsRequest = new RestRequest(_config.GetSection(EndpointsConfigSection).GetValue<string>(GetStudyGroupsEndpointName), Method.Get);
            var leaveStudyGroupRequest = new RestRequest(_config.GetSection(EndpointsConfigSection).GetValue<string>(LeaveStudyGroupEndpointName), Method.Post)
                .AddParameter("groupName", groupName1)
                .AddParameter("userId", userToLeave.UserId);

            _client.Execute(createStudyGroupRequest);
            _client.Execute(createStudyGroupRequest);

            RestResponse leaveStudyGroupResponse = _client.Execute(leaveStudyGroupRequest);
            RestResponse getStudyGroupsResponse = _client.Execute(getStudyGroupsRequest);
            List<StudyGroup> existingStudyGroupsAfterLeaving = JsonConvert.DeserializeObject<List<StudyGroup>>(getStudyGroupsResponse.Content);
            CollectionAssert.Contains(existingStudyGroupsAfterLeaving.Find(studyGroup => studyGroup.Name.Equals(groupName2)).Users, userToLeave);
        }
    }
}