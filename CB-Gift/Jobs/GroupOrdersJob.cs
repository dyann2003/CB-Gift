using CB_Gift.Services.IService;
using Quartz;

namespace CB_Gift.Jobs
{
    public class GroupOrdersJob : IJob
    {
        private readonly IPlanService _planService;

        public GroupOrdersJob(IPlanService planService)
        {
            _planService = planService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await _planService.GroupSubmittedOrdersAsync("system");
        }
    }
}
