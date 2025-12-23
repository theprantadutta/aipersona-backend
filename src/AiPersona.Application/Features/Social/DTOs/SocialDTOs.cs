namespace AiPersona.Application.Features.Social.DTOs;

public record LikeResultDto(
    bool IsLiked,
    int LikeCount);

public record FavoriteResultDto(
    bool IsFavorited,
    int FavoriteCount);

public record FollowResultDto(
    bool IsFollowing,
    int FollowerCount,
    int FollowingCount);

public record BlockResultDto(
    bool IsBlocked,
    string Message);

public record ReportResultDto(
    Guid ReportId,
    string Status,
    string Message);

public record PersonaStatsDto(
    Guid PersonaId,
    int LikeCount,
    int FavoriteCount,
    int ViewCount,
    int ChatCount,
    int MessageCount);

public record UserProfileDto(
    Guid Id,
    string DisplayName,
    string? ProfileImage,
    string? Bio,
    int FollowerCount,
    int FollowingCount,
    int PersonaCount,
    bool IsFollowing,
    bool IsBlocked,
    DateTime CreatedAt);

public record UserListItemDto(
    Guid Id,
    string DisplayName,
    string? ProfileImage,
    bool IsFollowing,
    DateTime FollowedAt);

public record UserListDto(
    List<UserListItemDto> Users,
    int Total,
    int Limit,
    int Offset);

public record BlockedUserDto(
    Guid Id,
    string DisplayName,
    string? ProfileImage,
    string? Reason,
    DateTime BlockedAt);

public record BlockedUsersListDto(
    List<BlockedUserDto> Users,
    int Total,
    int Limit,
    int Offset);

public record FavoritePersonaDto(
    Guid PersonaId,
    string Name,
    string? ImagePath,
    string? Description,
    int LikeCount,
    DateTime FavoritedAt);

public record FavoritesListDto(
    List<FavoritePersonaDto> Personas,
    int Total,
    int Limit,
    int Offset);

public record ActivityItemDto(
    Guid Id,
    string Type,
    string Description,
    Guid? TargetId,
    string? TargetType,
    string? Metadata,  // JSON serialized metadata
    DateTime CreatedAt);

public record ActivityFeedDto(
    List<ActivityItemDto> Activities,
    int Total,
    int Limit,
    int Offset);

public record CheckLikedDto(bool IsLiked);
public record CheckFavoritedDto(bool IsFavorited);
