using MediatR;
using AiPersona.Application.Common;
using AiPersona.Application.Features.Subscription.DTOs;

namespace AiPersona.Application.Features.Subscription.Queries.GetPlans;

public record GetPlansQuery : IRequest<Result<SubscriptionPlansDto>>;

public class GetPlansQueryHandler : IRequestHandler<GetPlansQuery, Result<SubscriptionPlansDto>>
{
    public Task<Result<SubscriptionPlansDto>> Handle(GetPlansQuery request, CancellationToken cancellationToken)
    {
        var plans = new List<SubscriptionPlanDto>
        {
            new SubscriptionPlanDto(
                "free",
                "Free",
                "Free",
                0,
                null,
                "USD",
                new List<string> { "3 AI personas", "50 messages/day", "7-day chat history", "100MB storage" },
                50, 3, 100, 7),
            new SubscriptionPlanDto(
                "basic_monthly",
                "Basic",
                "Basic",
                4.99m,
                49.99m,
                "USD",
                new List<string> { "10 AI personas", "500 messages/day", "30-day chat history", "1GB storage", "Priority support" },
                500, 10, 1024, 30),
            new SubscriptionPlanDto(
                "premium_monthly",
                "Premium",
                "Premium",
                9.99m,
                99.99m,
                "USD",
                new List<string> { "Unlimited personas", "Unlimited messages", "Unlimited history", "10GB storage", "Advanced analytics", "API access" },
                -1, -1, 10240, -1),
            new SubscriptionPlanDto(
                "pro_monthly",
                "Pro",
                "Pro",
                19.99m,
                199.99m,
                "USD",
                new List<string> { "Everything in Premium", "White-label option", "Team collaboration", "Custom AI models", "Dedicated support" },
                -1, -1, 102400, -1)
        };

        return Task.FromResult(Result<SubscriptionPlansDto>.Success(new SubscriptionPlansDto(plans)));
    }
}
