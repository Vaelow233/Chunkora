using Chunkora.Server.Authentication;
using Chunkora.Server.Dtos;
using Chunkora.Server.Errors;
using Chunkora.Server.Models;
using Chunkora.Server.Requests;
using Chunkora.Server.Utils;
using Microsoft.EntityFrameworkCore;

namespace Chunkora.Server.Services;

public class UserService(
    ILogger<UserService> logger, 
    ICurrentUser currentUser,
    GlobalDatabaseContext dbContext)
{
    public async Task<Result<UserDto>> CreateAsync(CreateUserRequest request)
    {
        if (!currentUser.HasPermission("admin.user.create"))
        {
            return Result<UserDto>.Failure(UserErrors.NoPermission);
        }
        if (await dbContext.Users.AnyAsync(user => user.Name.Equals(request.Name)))
        {
            return Result<UserDto>.Failure(UserErrors.NameExists);
        }
        var hashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(request.Password);
        var user = new User
        {
            RoleId = request.RoleId,
            Name = request.Name,
            HashedPassword = hashedPassword,
            Description = request.Description,
            AccessTokenExpiresAt = DateTime.UtcNow.AddHours(2),
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return Result<UserDto>.Success(UserDto.Create(user));
    }
    
    public async Task<Result<UserDto>> UpdateAsync(UpdateUserRequest request)
    {
        if (!currentUser.HasPermission("admin.user.update"))
        {
            return Result<UserDto>.Failure(UserErrors.NoPermission);
        }
        var user = await dbContext.Users.FirstOrDefaultAsync(user => user.Id.Equals(request.Id));
        if (user == null)
        {
            return Result<UserDto>.Failure(UserErrors.NotFound);
        }
        var hashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(request.Password);
        if (request.RoleId != Guid.Empty)
        {
            user.RoleId = request.RoleId;
        }
        if (request.Name != string.Empty)
        {
            user.Name = request.Name;
        }
        if (request.Password != string.Empty)
        {
            user.HashedPassword = hashedPassword;
        }
        if (request.Description != string.Empty)
        {
            // TODO: consider whether to do this (or make description required)
            user.Description = request.Description;
        }
        user.UpdatedAt = DateTime.UtcNow;
        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync();
        return Result<UserDto>.Success(UserDto.Create(user));
    }

    public async Task<Result<UserSecureDto>> LoginAsync(LoginRequest request)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(user => user.Name.Equals(request.Name));
        if (user == null)
        {
            return Result<UserSecureDto>.Failure(UserErrors.IncorrectNameOrPassword);
        }
        if (!BCrypt.Net.BCrypt.EnhancedVerify(request.Password, user.HashedPassword))
        {
            return Result<UserSecureDto>.Failure(UserErrors.IncorrectNameOrPassword);
        }
        return Result<UserSecureDto>.Success(UserSecureDto.Create(user));
    }

    public async Task<Result<UserSecureDto>> RefreshAsync(RefreshTokenRequest request)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(user => user.Name.Equals(request.Name));
        if (user == null)
        {
            return Result<UserSecureDto>.Failure(UserErrors.NotFound);
        }
        if (user.RefreshToken != request.RefreshToken)
        {
            return Result<UserSecureDto>.Failure(UserErrors.InvalidRefreshToken);
        }
        if (DateTimeOffset.UtcNow > user.RefreshTokenExpiresAt)
        {
            return Result<UserSecureDto>.Failure(UserErrors.RefreshTokenExpired);
        }
        user.AccessToken = User.GenerateToken();
        user.AccessTokenExpiresAt = DateTime.UtcNow.AddHours(2);
        user.RefreshToken = User.GenerateToken();
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<UserSecureDto>.Failure(UserErrors.InvalidRefreshToken);
        }
        return Result<UserSecureDto>.Success(UserSecureDto.Create(user));
    }

    public async Task<Result<UserDto>> GetAsync(Guid id)
    {
        if (!currentUser.HasPermission("admin.user.get"))
        {
            return Result<UserDto>.Failure(UserErrors.NoPermission);
        }
        var user = await dbContext.Users.FindAsync(id);
        if (user == null)
        {
            return Result<UserDto>.Failure(UserErrors.NotFound);
        }
        return Result<UserDto>.Success(UserDto.Create(user));
    }

    public async Task<Result<UserDto>> DeleteAsync(Guid id)
    {
        if (!currentUser.HasPermission("admin.user.delete"))
        {
            return Result<UserDto>.Failure(UserErrors.NoPermission);
        }
        var user = await dbContext.Users.FindAsync(id);
        if (user == null)
        {
            return Result<UserDto>.Failure(UserErrors.NotFound);
        }
        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync();
        return Result<UserDto>.Success(UserDto.Create(user));
    }

    public async Task<Result<PageResult<UserDto>>> ListAsync(int limit, int offset)
    {
        if (!currentUser.HasPermission("admin.user.list"))
        {
            return Result<PageResult<UserDto>>.Failure(UserErrors.NoPermission);
        }
        var total = await dbContext.Users.CountAsync();
        var users = await dbContext.Users.OrderBy(user => user.Name).Skip(offset).Take(limit).ToListAsync();
        return Result<PageResult<UserDto>>.Success(new(offset, limit, total, users.Count, users.ConvertAll(user => UserDto.Create(user))));
    }
}