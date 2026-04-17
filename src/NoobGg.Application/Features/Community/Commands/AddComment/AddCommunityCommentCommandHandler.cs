using System.Text.RegularExpressions;
using MediatR;
using MongoDB.Driver;
using NoobGg.Application.Common.Constants;
using NoobGg.Application.Common.Interfaces;
using NoobGg.Application.Common.Models;
using NoobGg.Application.Features.Community.DTOs;
using NoobGg.Domain.Entities;
using NoobGg.Domain.Enums;

namespace NoobGg.Application.Features.Community.Commands.AddComment;

public class AddCommunityCommentCommandHandler
    : IRequestHandler<AddCommunityCommentCommand, Result<CommunityCommentResponse>>
{
    private readonly IMongoContext _mongoContext;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notificationService;

    private static readonly Regex MentionRegex = new(@"@([A-Za-z0-9_]{2,30})", RegexOptions.Compiled);

    public AddCommunityCommentCommandHandler(
        IMongoContext mongoContext,
        ICurrentUser currentUser,
        INotificationService notificationService)
    {
        _mongoContext = mongoContext;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task<Result<CommunityCommentResponse>> Handle(
        AddCommunityCommentCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            return Result<CommunityCommentResponse>.Unauthorized();

        var userId = _currentUser.UserId;

        var posts = _mongoContext.GetCollection<CommunityPost>(CollectionNames.CommunityPosts);
        var post = await posts.Find(p => p.Id == request.PostId).FirstOrDefaultAsync(ct);
        if (post is null)
            return Result<CommunityCommentResponse>.NotFound("Post not found");

        var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
        var user = await users.Find(u => u.Id == userId).FirstOrDefaultAsync(ct);
        if (user is null)
            return Result<CommunityCommentResponse>.NotFound("User not found");

        var profiles = _mongoContext.GetCollection<UserProfile>(CollectionNames.UserProfiles);
        var profile = await profiles.Find(p => p.UserId == userId).FirstOrDefaultAsync(ct);

        var comment = new CommunityComment
        {
            PostId = request.PostId,
            AuthorId = userId,
            Content = request.Content,
            UpvoteCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var comments = _mongoContext.GetCollection<CommunityComment>(CollectionNames.CommunityComments);
        await comments.InsertOneAsync(comment, cancellationToken: ct);

        await posts.UpdateOneAsync(
            p => p.Id == request.PostId,
            Builders<CommunityPost>.Update
                .Inc(p => p.CommentCount, 1)
                .Set(p => p.LastActivityAt, DateTime.UtcNow)
                .Set(p => p.UpdatedAt, DateTime.UtcNow),
            cancellationToken: ct);

        var response = new CommunityCommentResponse(
            comment.Id,
            comment.AuthorId,
            user.Username,
            profile?.AvatarUrl,
            comment.Content,
            comment.UpvoteCount,
            false,
            comment.CreatedAt);

        _ = Task.Run(() => SendNotificationsAsync(post, userId, user.Username, request.Content, ct), ct);

        return Result<CommunityCommentResponse>.Created(response);
    }

    private async Task SendNotificationsAsync(
        CommunityPost post, string commenterId, string commenterUsername, string content, CancellationToken ct)
    {
        try
        {
            var notifiedUserIds = new HashSet<string>();
            var topicTitle = string.IsNullOrWhiteSpace(post.Title) ? "a topic" : post.Title;
            var data = new Dictionary<string, string>
            {
                ["postId"] = post.Id,
                ["commenterId"] = commenterId
            };

            if (post.AuthorId != commenterId)
            {
                notifiedUserIds.Add(post.AuthorId);
                await _notificationService.CreateAsync(
                    post.AuthorId,
                    NotificationType.CommunityTopicCommented,
                    $"{commenterUsername} replied to your topic",
                    $"New reply on \"{topicTitle}\"",
                    data,
                    ct);
            }

            var mentionedUsernames = ParseMentions(content);
            if (mentionedUsernames.Count == 0) return;

            var users = _mongoContext.GetCollection<User>(CollectionNames.Users);
            var normalizedMentions = mentionedUsernames
                .Select(m => m.ToLowerInvariant())
                .Distinct()
                .ToList();

            var mentionedUsers = await users
                .Find(Builders<User>.Filter.In(u => u.Username, mentionedUsernames))
                .ToListAsync(ct);

            foreach (var mentioned in mentionedUsers)
            {
                if (mentioned.Id == commenterId) continue;
                if (notifiedUserIds.Contains(mentioned.Id)) continue;

                notifiedUserIds.Add(mentioned.Id);
                await _notificationService.CreateAsync(
                    mentioned.Id,
                    NotificationType.CommunityMentioned,
                    $"{commenterUsername} mentioned you",
                    $"You were mentioned in \"{topicTitle}\"",
                    data,
                    ct);
            }
        }
        catch
        {
            // fire-and-forget: don't fail the comment creation
        }
    }

    private static List<string> ParseMentions(string content)
    {
        var matches = MentionRegex.Matches(content);
        return matches
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
