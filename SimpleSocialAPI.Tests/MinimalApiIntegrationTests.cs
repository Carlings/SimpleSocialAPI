using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using SQLitePCL;
using SimpleSocialAPI.Data.Models;

namespace SimpleSocialAPI.Tests
{
    [TestFixture]
    public class AllServicesTests
    {
        private IDbConnection _db = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Batteries.Init();
        }

        [SetUp]
        public void SetUp()
        {
            _db = new SqliteConnection("Data Source=:memory:");
            _db.Open();

            _db.Execute(@"
                CREATE TABLE users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    username TEXT UNIQUE NOT NULL,
                    displayname TEXT NOT NULL,
                    email TEXT UNIQUE NOT NULL
                );
                CREATE TABLE posts (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    title TEXT NOT NULL,
                    body TEXT NOT NULL,
                    authorid INTEGER NOT NULL,
                    createdat TEXT NOT NULL
                );
                CREATE TABLE follows (
                    follower_id INTEGER NOT NULL,
                    followed_id INTEGER NOT NULL,
                    PRIMARY KEY (follower_id, followed_id)
                );
                CREATE TABLE likes (
                    user_id INTEGER NOT NULL,
                    post_id INTEGER NOT NULL,
                    PRIMARY KEY (user_id, post_id)
                );
            ");
        }

        [TearDown]
        public void TearDown()
        {
            _db.Dispose();
        }

        [Test]
        public async Task CreateUser_ShouldReturnWithId()
        {
            var svc = new UserService(_db);
            var u = new User { Username = "u1", DisplayName = "User One", Email = "u1@example.com" };

            var created = await svc.CreateAsync(u);

            Assert.That(created.Id, Is.GreaterThan(0));
            Assert.AreEqual(u.Username, created.Username);
        }

        [Test]
        public async Task GetUser_Nonexistent_ShouldReturnNull()
        {
            var svc = new UserService(_db);
            var user = await svc.GetByIdAsync(9999);
            Assert.IsNull(user);
        }

        [Test]
        public async Task CreatePost_AssignsIdAndFields()
        {
            _db.Execute("INSERT INTO users(username,displayname,email) VALUES('a','A','a@x');");

            var svc = new PostService(_db);
            var p = new Post { Title = "Hello", Body = "World", AuthorId = 1 };

            var created = await svc.CreateAsync(p);

            Assert.That(created.Id, Is.GreaterThan(0));
            Assert.AreEqual(p.Title, created.Title);
        }

        [Test]
        public async Task GetAllPosts_ShouldReturnInserted()
        {
            _db.Execute("INSERT INTO users(username,displayname,email) VALUES('a','A','a@x');");
            _db.Execute("INSERT INTO posts(title,body,authorid,createdat) VALUES('X','Y',1,datetime('now'));");

            var svc = new PostService(_db);
            var list = await svc.GetAllAsync();

            Assert.That(list, Has.Exactly(1).Items);
        }

        [Test]
        public async Task Follow_Unfollow_Behavior()
        {
            _db.Execute("INSERT INTO users(username,displayname,email) VALUES('a','A','a@x'),('b','B','b@x');");
            var svc = new FollowService(_db);

            Assert.IsTrue(await svc.FollowAsync(1, 2));
            Assert.IsFalse(await svc.FollowAsync(1, 2));
            Assert.IsTrue(await svc.UnfollowAsync(1, 2));
            Assert.IsFalse(await svc.UnfollowAsync(1, 2));
        }

        [Test]
        public async Task Like_Unlike_Count_Behavior()
        {
            _db.Execute("INSERT INTO users(username,displayname,email) VALUES('u','U','u@x');");
            _db.Execute("INSERT INTO posts(title,body,authorid,createdat) VALUES('T','B',1,datetime('now'));");
            var svc = new LikeService(_db);

            Assert.IsTrue(await svc.LikeAsync(1, 1));
            Assert.IsFalse(await svc.LikeAsync(1, 1));
            Assert.AreEqual(1, await svc.CountAsync(1));
            Assert.IsTrue(await svc.UnlikeAsync(1, 1));
            Assert.AreEqual(0, await svc.CountAsync(1));
        }
    }

    public class UserService
    {
        private readonly IDbConnection _db;
        public UserService(IDbConnection db) => _db = db;

        public Task<User> CreateAsync(User u) =>
            _db.QuerySingleAsync<User>(
                @"INSERT INTO users(username,displayname,email)
                  VALUES(@Username,@DisplayName,@Email);
                  SELECT last_insert_rowid() AS Id,
                         @Username AS Username,
                         @DisplayName AS DisplayName,
                         @Email AS Email;",
                u);

        public Task<User?> GetByIdAsync(int id) =>
            _db.QuerySingleOrDefaultAsync<User>(
                "SELECT * FROM users WHERE id = @Id;", new { Id = id });
    }

    public class PostService
    {
        private readonly IDbConnection _db;
        public PostService(IDbConnection db) => _db = db;

        public async Task<Post> CreateAsync(Post p)
        {
            p.CreatedAt = DateTime.UtcNow;
            return await _db.QuerySingleAsync<Post>(
                @"INSERT INTO posts(title,body,authorid,createdat)
                  VALUES(@Title,@Body,@AuthorId,@CreatedAt);
                  SELECT last_insert_rowid() AS Id,
                         @Title AS Title,
                         @Body AS Body,
                         @AuthorId AS AuthorId,
                         @CreatedAt AS CreatedAt;",
                p);
        }

        public Task<IEnumerable<Post>> GetAllAsync() =>
            _db.QueryAsync<Post>("SELECT * FROM posts ORDER BY createdat DESC;");
    }

    public class FollowService
    {
        private readonly IDbConnection _db;
        public FollowService(IDbConnection db) => _db = db;

        public Task<bool> FollowAsync(int followerId, int followedId) =>
            _db.ExecuteAsync(
                @"INSERT OR IGNORE INTO follows(follower_id,followed_id)
                  VALUES(@followerId,@followedId);",
                new { followerId, followedId }
            ).ContinueWith(t => t.Result > 0);

        public Task<bool> UnfollowAsync(int followerId, int followedId) =>
            _db.ExecuteAsync(
                "DELETE FROM follows WHERE follower_id=@followerId AND followed_id=@followedId;",
                new { followerId, followedId }
            ).ContinueWith(t => t.Result > 0);
    }

    public class LikeService
    {
        private readonly IDbConnection _db;
        public LikeService(IDbConnection db) => _db = db;

        public Task<bool> LikeAsync(int userId, int postId) =>
            _db.ExecuteAsync(
                @"INSERT OR IGNORE INTO likes(user_id,post_id)
                  VALUES(@userId,@postId);",
                new { userId, postId }
            ).ContinueWith(t => t.Result > 0);

        public Task<bool> UnlikeAsync(int userId, int postId) =>
            _db.ExecuteAsync(
                "DELETE FROM likes WHERE user_id=@userId AND post_id=@postId;",
                new { userId, postId }
            ).ContinueWith(t => t.Result > 0);

        public Task<int> CountAsync(int postId) =>
            _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM likes WHERE post_id=@postId;",
                new { postId });
    }
}
