using System;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Authentication;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Test.Authentication
{
    [TestFixture]
    public class UserServiceFixture : CoreTest<UserService>
    {
        private const string ValidUsername = "admin";
        private const string ValidPassword = "password123";

        private User _user;

        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IUserRepository>()
                .Setup(r => r.Insert(It.IsAny<User>()))
                .Returns<User>(u =>
                {
                    u.Id = 1;
                    return u;
                });

            Mocker.GetMock<IUserRepository>()
                .Setup(r => r.Update(It.IsAny<User>()))
                .Returns<User>(u => u);

            _user = Subject.Add(ValidUsername, ValidPassword);

            Mocker.GetMock<IUserRepository>()
                .Setup(r => r.FindUser(It.Is<string>(s => s == ValidUsername)))
                .Returns(_user);

            Mocker.GetMock<IUserRepository>()
                .Setup(r => r.FindUser(It.Is<string>(s => s != ValidUsername)))
                .Returns((User)null);
        }

        [Test]
        public void add_should_create_user_with_hashed_password()
        {
            _user.Username.Should().Be(ValidUsername);
            _user.Password.Should().NotBe(ValidPassword);
            _user.Salt.Should().NotBeNullOrWhiteSpace();
            _user.Iterations.Should().BeGreaterThan(0);
            _user.Identifier.Should().NotBe(Guid.Empty);
        }

        [Test]
        public void add_should_lowercase_username()
        {
            var user = Subject.Add("ADMIN", ValidPassword);

            user.Username.Should().Be("admin");
        }

        [Test]
        public void find_user_should_return_user_with_valid_credentials()
        {
            var result = Subject.FindUser(ValidUsername, ValidPassword);

            result.Should().NotBeNull();
            result.Username.Should().Be(ValidUsername);
        }

        [Test]
        public void find_user_should_return_null_with_wrong_password()
        {
            var result = Subject.FindUser(ValidUsername, "wrongpassword");

            result.Should().BeNull();
        }

        [Test]
        public void find_user_should_return_null_with_unknown_username()
        {
            var result = Subject.FindUser("unknownuser", ValidPassword);

            result.Should().BeNull();
        }

        [Test]
        public void find_user_should_return_null_with_empty_username()
        {
            var result = Subject.FindUser("", ValidPassword);

            result.Should().BeNull();
        }

        [Test]
        public void find_user_should_return_null_with_empty_password()
        {
            var result = Subject.FindUser(ValidUsername, "");

            result.Should().BeNull();
        }

        [Test]
        public void find_user_should_return_null_with_null_username()
        {
            var result = Subject.FindUser(null, ValidPassword);

            result.Should().BeNull();
        }

        [Test]
        public void find_user_should_return_null_with_null_password()
        {
            var result = Subject.FindUser(ValidUsername, null);

            result.Should().BeNull();
        }

        [Test]
        public void find_user_should_be_case_insensitive_for_username()
        {
            // FindUser lowercases input, so "ADMIN" becomes "admin" which matches the mock
            var result = Subject.FindUser("ADMIN", ValidPassword);

            result.Should().NotBeNull();
        }

        [Test]
        public void find_user_should_be_case_sensitive_for_password()
        {
            var result = Subject.FindUser(ValidUsername, "PASSWORD123");

            result.Should().BeNull();
        }

        [Test]
        public void find_user_should_migrate_sha256_password()
        {
            var sha256Hash = ValidPassword.SHA256Hash();
            var legacyUser = new User
            {
                Id = 2,
                Identifier = Guid.NewGuid(),
                Username = ValidUsername,
                Password = sha256Hash,
                Salt = null,
                Iterations = 0
            };

            Mocker.GetMock<IUserRepository>()
                .Setup(r => r.FindUser(ValidUsername))
                .Returns(legacyUser);

            var result = Subject.FindUser(ValidUsername, ValidPassword);

            result.Should().NotBeNull();
            result.Salt.Should().NotBeNullOrWhiteSpace();
            result.Iterations.Should().BeGreaterThan(0);
            result.Password.Should().NotBe(sha256Hash);

            Mocker.GetMock<IUserRepository>()
                .Verify(r => r.Update(It.Is<User>(u => u.Salt.IsNotNullOrWhiteSpace())), Times.Once());
        }

        [Test]
        public void find_user_should_reject_wrong_sha256_password()
        {
            var legacyUser = new User
            {
                Id = 2,
                Identifier = Guid.NewGuid(),
                Username = ValidUsername,
                Password = ValidPassword.SHA256Hash(),
                Salt = null,
                Iterations = 0
            };

            Mocker.GetMock<IUserRepository>()
                .Setup(r => r.FindUser(ValidUsername))
                .Returns(legacyUser);

            var result = Subject.FindUser(ValidUsername, "wrongpassword");

            result.Should().BeNull();
        }

        [Test]
        public void find_user_by_identifier_should_return_user()
        {
            var identifier = Guid.NewGuid();
            var user = new User { Identifier = identifier, Username = ValidUsername };

            Mocker.GetMock<IUserRepository>()
                .Setup(r => r.FindUser(identifier))
                .Returns(user);

            var result = Subject.FindUser(identifier);

            result.Should().NotBeNull();
            result.Identifier.Should().Be(identifier);
        }

        [Test]
        public void find_user_by_unknown_identifier_should_return_null()
        {
            var identifier = Guid.NewGuid();

            Mocker.GetMock<IUserRepository>()
                .Setup(r => r.FindUser(identifier))
                .Returns((User)null);

            var result = Subject.FindUser(identifier);

            result.Should().BeNull();
        }

        [Test]
        public void different_passwords_should_produce_different_hashes()
        {
            var user1 = Subject.Add("user1", "password1");
            var user2 = Subject.Add("user2", "password2");

            user1.Password.Should().NotBe(user2.Password);
        }

        [Test]
        public void same_password_should_produce_different_salts()
        {
            var user1 = Subject.Add("user1", "samepassword");
            var user2 = Subject.Add("user2", "samepassword");

            user1.Salt.Should().NotBe(user2.Salt);
        }
    }
}
