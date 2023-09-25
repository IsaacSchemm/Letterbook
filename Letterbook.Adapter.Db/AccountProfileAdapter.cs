﻿using Letterbook.Core.Adapters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;

namespace Letterbook.Adapter.Db;

public class AccountProfileAdapter : IAccountProfileAdapter, IAsyncDisposable
{
    private readonly ILogger<AccountProfileAdapter> _logger;
    private readonly RelationalContext _context;

    public AccountProfileAdapter(ILogger<AccountProfileAdapter> logger, RelationalContext context)
    {
        _logger = logger;
        _context = context;
    }

    public bool RecordAccount(Models.Account account)
    {
        var added = _context.Accounts.Add(account);
        _logger.LogDebug("{Method} added properties {@Properties}", nameof(RecordAccount), added.Properties.Select(p => $"{p.Metadata.Name}:{p.CurrentValue}"));
        return added.State == EntityState.Added;
    }

    public Task<bool> RecordAccounts(IEnumerable<Models.Account> accounts)
    {
        throw new NotImplementedException();
    }

    public Task<Models.Account?> LookupAccount(Guid id)
    {
        throw new NotImplementedException();
    }

    public IQueryable<Models.Account> SearchAccounts()
    {
        throw new NotImplementedException();
    }

    public Task<bool> AnyProfile(IAccountProfileAdapter.ProfileComparer comparer)
    {
        return _context.Profiles.AnyAsync();
    }

    public Task<bool> AnyProfile(string handle)
    {
        return _context.Profiles.AnyAsync(profile => profile.Handle == handle);
    }

    public Task<bool> AnyProfile(Uri id)
    {
        return _context.Profiles.AnyAsync(profile => profile.Id == id);
    }

    public Task<Models.Profile?> LookupProfile(Guid localId)
    {
        return _context.Profiles.FirstOrDefaultAsync(profile => profile.LocalId == localId);
    }

    public Task<Models.Profile?> LookupProfile(Uri id)
    {
        return _context.Profiles.FirstOrDefaultAsync(profile => profile.Id == id);
    }

    public Task<Models.Profile?> LookupProfileWithRelation(Uri id, Uri relationId)
    {
        return _context.Profiles.Where(profile => profile.Id == id)
            .Include(profile => profile.Following.Where(relation => relation.Follows.Id == relationId))
                .ThenInclude(relation => relation.Follows)
            .Include(profile => profile.Followers.Where(relation => relation.Follower.Id == relationId))
                .ThenInclude(relation => relation.Follower)
            .AsSplitQuery()
            .FirstOrDefaultAsync();
    }

    public Task<Models.Profile?> LookupProfileWithRelation(Guid localId, Uri relationId)
    {
        return _context.Profiles.Where(profile => profile.LocalId == localId)
            .Include(profile => profile.Following.Where(relation => relation.Follows.Id == relationId))
                .ThenInclude(relation => relation.Follows)
            .Include(profile => profile.Followers.Where(relation => relation.Follower.Id == relationId))
                .ThenInclude(relation => relation.Follower)
            .AsSplitQuery()
            .FirstOrDefaultAsync();
    }

    public IAsyncEnumerable<Models.Profile> FindProfilesByHandle(string handle, bool partial = false, int limit = 20, int page = 0)
    {
        limit = limit >= 100 ? 100 : limit;
        var query = _context.Profiles.OrderBy(profile => profile.Id)
            .Skip(limit * page)
            .Take(limit);
        query = partial 
            ? query.Where(profile => profile.Handle.StartsWith(handle)) 
            : query.Where(profile => profile.Handle == handle);
        return query.AsAsyncEnumerable();
    }

    public void Add(Models.Profile profile)
    {
        _context.Profiles.Add(profile);
    }

    public void AddRange(IEnumerable<Models.Profile> profile)
    {
        _context.Profiles.AddRange(profile);
    }

    public void Update(Models.Profile profile)
    {
        _context.Profiles.Update(profile);
    }

    public void UpdateRange(IEnumerable<Models.Profile> profile)
    {
        _context.Profiles.UpdateRange(profile);
    }

    public void Delete(object record)
    {
        if (record is EntityEntry entry)
        {
            entry.State = EntityState.Deleted;
        }
    }

    public Task Cancel()
    {
        if (_context.Database.CurrentTransaction is not null)
        {
            return _context.Database.RollbackTransactionAsync();
        }
        
        return Task.CompletedTask;
    }
    
    public Task Commit()
    {
        if (_context.Database.CurrentTransaction is not null)
        {
            return _context.Database.CommitTransactionAsync();
        }

        return _context.SaveChangesAsync();
    }

    private void Start()
    {
        if (_context.Database.CurrentTransaction is null)
        {
            _context.Database.BeginTransaction();
        }
    }
    public void Dispose()
    {
        _context.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}
