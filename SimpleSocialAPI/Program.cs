using Dapper;
using SimpleSocialAPI.Data.Models;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DapperContext>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapPost("/users", async (DapperContext context, User user) =>
{
    var query = @"
        INSERT INTO users (username, displayname, email)
        VALUES (@Username, @DisplayName, @Email)
        RETURNING *";

    using var conn = context.CreateConnection();
    var createdUser = await conn.QuerySingleAsync<User>(query, user);
    return Results.Created($"/users/{createdUser.Id}", createdUser);
});

app.MapGet("/users/{id:int}", async (DapperContext context, int id) =>
{
    using var conn = context.CreateConnection();
    var user = await conn.QuerySingleOrDefaultAsync<User>("SELECT * FROM users WHERE id = @Id", new { Id = id });
    return user is not null ? Results.Ok(user) : Results.NotFound();
});


app.MapGet("/posts", async (DapperContext context) =>
{
    using var conn = context.CreateConnection();
    var posts = await conn.QueryAsync<Post>("SELECT * FROM posts ORDER BY createdat DESC");
    return Results.Ok(posts);
});

app.MapPost("/posts", async (DapperContext context, Post post) =>
{
    post.CreatedAt = DateTime.UtcNow;

    var query = @"
        INSERT INTO posts (title, body, authorid, createdat)
        VALUES (@Title, @Body, @AuthorId, @CreatedAt)
        RETURNING *";

    using var conn = context.CreateConnection();
    var createdPost = await conn.QuerySingleAsync<Post>(query, post);

    return Results.Created($"/posts/{createdPost.Id}", createdPost);
});



app.MapPost("/users/{id}/follow/{followedId}", async (DapperContext context, int id, int followedId) =>
{
    var query = "INSERT INTO follows (follower_id, followed_id) VALUES (@FollowerId, @FollowedId) ON CONFLICT DO NOTHING";

    using var conn = context.CreateConnection();
    var affectedRows = await conn.ExecuteAsync(query, new { FollowerId = id, FollowedId = followedId });

    return affectedRows > 0 ? Results.Ok() : Results.BadRequest("Already following or invalid IDs");
});

app.MapDelete("/users/{id}/follow/{followedId}", async (DapperContext context, int id, int followedId) =>
{
    var query = "DELETE FROM follows WHERE follower_id = @FollowerId AND followed_id = @FollowedId";

    using var conn = context.CreateConnection();
    var affectedRows = await conn.ExecuteAsync(query, new { FollowerId = id, FollowedId = followedId });

    return affectedRows > 0 ? Results.Ok() : Results.NotFound();
});



app.MapPost("/posts/{postId}/like/{userId}", async (DapperContext context, int postId, int userId) =>
{
    var query = "INSERT INTO likes (user_id, post_id) VALUES (@UserId, @PostId) ON CONFLICT DO NOTHING";

    using var conn = context.CreateConnection();
    var affectedRows = await conn.ExecuteAsync(query, new { UserId = userId, PostId = postId });

    return affectedRows > 0 ? Results.Ok() : Results.BadRequest("Already liked or invalid IDs");
});

app.MapDelete("/posts/{postId}/like/{userId}", async (DapperContext context, int postId, int userId) =>
{
    var query = "DELETE FROM likes WHERE user_id = @UserId AND post_id = @PostId";

    using var conn = context.CreateConnection();
    var affectedRows = await conn.ExecuteAsync(query, new { UserId = userId, PostId = postId });

    return affectedRows > 0 ? Results.Ok() : Results.NotFound();
});

app.MapGet("/posts/{postId}/likes/count", async (DapperContext context, int postId) =>
{
    var query = "SELECT COUNT(*) FROM likes WHERE post_id = @PostId";

    using var conn = context.CreateConnection();
    var count = await conn.ExecuteScalarAsync<int>(query, new { PostId = postId });

    return Results.Ok(new { LikesCount = count });
});

await app.RunAsync();