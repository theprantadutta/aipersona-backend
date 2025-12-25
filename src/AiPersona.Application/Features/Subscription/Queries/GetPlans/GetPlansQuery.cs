using MediatR;
using AiPersona.Application.Common;
using AiPersona.Application.Features.Subscription.DTOs;

namespace AiPersona.Application.Features.Subscription.Queries.GetPlans;

public record GetPlansQuery : IRequest<Result<SubscriptionPlansDto>>;

public class GetPlansQueryHandler : IRequestHandler<GetPlansQuery, Result<SubscriptionPlansDto>>
{
    public Task<Result<SubscriptionPlansDto>> Handle(GetPlansQuery request, CancellationToken cancellationToken)
    {
        // NOTE: Limits MUST match GetLimitsForTier() in GetCurrentUsageQuery and SendMessageCommand
        var plans = new List<SubscriptionPlanDto>
        {
            new SubscriptionPlanDto(
                "free",
                "Free",
                "Free",
                0,
                null,
                "USD",
                new List<string> { "50 messages per day", "3 custom personas", "7-day chat history", "100MB storage", "Basic chat features" },
                50, 3, 100, 7),
            new SubscriptionPlanDto(
                "basic_monthly",
                "Basic",
                "Basic",
                4.99m,
                49.99m,
                "USD",
                new List<string> { "500 messages per day", "10 custom personas", "30-day chat history", "1GB storage", "Voice features", "PDF export" },
                500, 10, 1024, 30),
            new SubscriptionPlanDto(
                "premium_monthly",
                "Premium",
                "Premium",
                9.99m,
                99.99m,
                "USD",
                new List<string> { "Unlimited messages", "Unlimited personas", "Unlimited chat history", "10GB storage", "All voice features", "Full analytics", "Priority support" },
                -1, -1, 10240, -1),
            new SubscriptionPlanDto(
                "pro_monthly",
                "Pro",
                "Pro",
                19.99m,
                199.99m,
                "USD",
                new List<string> { "Unlimited messages", "Unlimited personas", "Unlimited chat history", "100GB storage", "All voice features", "Full analytics + API access", "Dedicated support" },
                -1, -1, 102400, -1)
        };

        return Task.FromResult(Result<SubscriptionPlansDto>.Success(new SubscriptionPlansDto(plans)));
    }
}
