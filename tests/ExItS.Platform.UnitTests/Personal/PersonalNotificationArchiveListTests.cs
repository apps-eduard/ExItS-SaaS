using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.UnitTests.Personal;

public sealed class PersonalNotificationArchiveListTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);
    private static readonly PlatformUserId UserA = PlatformUserId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PlatformUserId UserB = PlatformUserId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

    [Fact]
    public async Task Recent_includes_29_day_old_and_excludes_31_day_old()
    {
        var repo = new InMemoryRepo();
        repo.Add(Create(UserA, "recent-29", Now.AddDays(-29)));
        repo.Add(Create(UserA, "boundary-30", Now.AddDays(-30)));
        repo.Add(Create(UserA, "archived-31", Now.AddDays(-31)));
        repo.Add(Create(UserB, "other-user", Now.AddDays(-2)));

        var sut = new ListPersonalInAppNotifications(repo, new FixedClock(Now));
        var recent = await sut.ExecutePagedAsync(UserA, "recent", page: 1, pageSize: 50, unreadOnly: false);

        Assert.Equal(2, recent.TotalCount);
        Assert.Contains(recent.Items, x => x.Preview == "recent-29");
        Assert.Contains(recent.Items, x => x.Preview == "boundary-30");
        Assert.DoesNotContain(recent.Items, x => x.Preview == "archived-31");
        Assert.DoesNotContain(recent.Items, x => x.Preview == "other-user");
    }

    [Fact]
    public async Task Archived_includes_31_day_old_newest_first_and_paginates()
    {
        var repo = new InMemoryRepo();
        repo.Add(Create(UserA, "a-31", Now.AddDays(-31)));
        repo.Add(Create(UserA, "a-40", Now.AddDays(-40)));
        repo.Add(Create(UserA, "a-50", Now.AddDays(-50)));
        repo.Add(Create(UserA, "recent", Now.AddDays(-1)));

        var sut = new ListPersonalInAppNotifications(repo, new FixedClock(Now));
        var page1 = await sut.ExecutePagedAsync(UserA, "archived", page: 1, pageSize: 2, unreadOnly: false);
        var page2 = await sut.ExecutePagedAsync(UserA, "archived", page: 2, pageSize: 2, unreadOnly: false);

        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal("a-31", page1.Items[0].Preview);
        Assert.Equal("a-40", page1.Items[1].Preview);
        Assert.Single(page2.Items);
        Assert.Equal("a-50", page2.Items[0].Preview);
        var page3 = await sut.ExecutePagedAsync(UserA, "archived", page: 3, pageSize: 2, unreadOnly: false);
        Assert.Empty(page3.Items);
        Assert.DoesNotContain(page1.Items.Concat(page2.Items), x => x.Preview == "recent");
        Assert.Equal(page1.Items.Count + page2.Items.Count, page1.Items.Select(i => i.Id).Union(page2.Items.Select(i => i.Id)).Count());
    }

    [Fact]
    public async Task Archived_unread_is_preserved_and_counted_globally()
    {
        var repo = new InMemoryRepo();
        var unreadArchived = Create(UserA, "old-unread", Now.AddDays(-45), isRead: false);
        repo.Add(unreadArchived);
        repo.Add(Create(UserA, "old-read", Now.AddDays(-46), isRead: true));
        repo.Add(Create(UserA, "new-unread", Now.AddDays(-1), isRead: false));

        var list = new ListPersonalInAppNotifications(repo, new FixedClock(Now));
        var archivedUnread = await list.ExecutePagedAsync(UserA, "archived", 1, 30, unreadOnly: true);
        Assert.Single(archivedUnread.Items);
        Assert.False(archivedUnread.Items[0].IsRead);

        var count = await new CountPersonalInAppNotificationUnread(repo).ExecuteAsync(UserA);
        Assert.Equal(2, count.UnreadCount);
    }

    [Fact]
    public async Task Cross_user_list_does_not_leak_other_recipient_notifications()
    {
        var repo = new InMemoryRepo();
        repo.Add(Create(UserA, "a-only", Now.AddDays(-40)));
        repo.Add(Create(UserB, "b-only", Now.AddDays(-41)));

        var sut = new ListPersonalInAppNotifications(repo, new FixedClock(Now));
        var forA = await sut.ExecutePagedAsync(UserA, "archived", 1, 30, false);
        var forB = await sut.ExecutePagedAsync(UserB, "archived", 1, 30, false);

        Assert.Single(forA.Items);
        Assert.Equal("a-only", forA.Items[0].Preview);
        Assert.Single(forB.Items);
        Assert.Equal("b-only", forB.Items[0].Preview);
    }

    private static PersonalInAppNotification Create(
        PlatformUserId recipient,
        string preview,
        DateTimeOffset createdAtUtc,
        bool isRead = false) =>
        PersonalInAppNotification.Rehydrate(
            PersonalInAppNotificationId.New(),
            recipient,
            "Connection request",
            preview,
            "PersonalConnectionRequest",
            Guid.NewGuid().ToString("D"),
            isRead,
            createdAtUtc,
            isRead ? createdAtUtc : null);

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }

    private sealed class InMemoryRepo : IPersonalInAppNotificationRepository
    {
        private readonly List<PersonalInAppNotification> _items = [];

        public void Add(PersonalInAppNotification n) => _items.Add(n);

        public Task<PersonalInAppNotification?> GetByIdAsync(
            PersonalInAppNotificationId id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<PersonalInAppNotification>> ListForUserAsync(
            PlatformUserId recipientUserIdentityId,
            int take,
            CancellationToken cancellationToken = default) =>
            ListForUserPagedAsync(recipientUserIdentityId, null, null, false, 0, take, cancellationToken)
                .ContinueWith(t => t.Result.Items, cancellationToken);

        public Task<(IReadOnlyList<PersonalInAppNotification> Items, int TotalCount)> ListForUserPagedAsync(
            PlatformUserId recipientUserIdentityId,
            DateTimeOffset? createdOnOrAfterUtc,
            DateTimeOffset? createdBeforeUtc,
            bool unreadOnly,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<PersonalInAppNotification> q = _items.Where(n => n.RecipientUserIdentityId == recipientUserIdentityId);
            if (createdOnOrAfterUtc is not null)
            {
                q = q.Where(n => n.CreatedAtUtc >= createdOnOrAfterUtc.Value);
            }

            if (createdBeforeUtc is not null)
            {
                q = q.Where(n => n.CreatedAtUtc < createdBeforeUtc.Value);
            }

            if (unreadOnly)
            {
                q = q.Where(n => !n.IsRead);
            }

            var filtered = q.OrderByDescending(n => n.CreatedAtUtc).ThenByDescending(n => n.Id.Value).ToList();
            return Task.FromResult<(IReadOnlyList<PersonalInAppNotification>, int)>(
                (filtered.Skip(skip).Take(take).ToList(), filtered.Count));
        }

        public Task<int> CountUnreadForUserAsync(
            PlatformUserId recipientUserIdentityId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Count(n => n.RecipientUserIdentityId == recipientUserIdentityId && !n.IsRead));

        public Task<PersonalInAppNotification?> FindByRecipientRelatedAsync(
            PlatformUserId recipientUserIdentityId,
            string relatedType,
            string relatedId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(n =>
                n.RecipientUserIdentityId == recipientUserIdentityId
                && n.RelatedType == relatedType
                && n.RelatedId == relatedId));

        public Task AddAsync(PersonalInAppNotification notification, CancellationToken cancellationToken = default)
        {
            _items.Add(notification);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(PersonalInAppNotification notification, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
