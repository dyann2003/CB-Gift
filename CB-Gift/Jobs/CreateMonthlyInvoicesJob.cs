using CB_Gift.Services.IService;
using Quartz;
using System;
using System.Threading.Tasks;

namespace CB_Gift.Jobs
{
    // Job này sẽ được Quartz khởi tạo tự động
    public class CreateMonthlyInvoicesJob : IJob
    {
        private readonly IInvoiceService _invoiceService;

        // Dependency Injection cho Service
        public CreateMonthlyInvoicesJob(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            // 1. Xác định tháng/năm mục tiêu
            // Vì Job chạy vào ngày 10, chúng ta cần lập hóa đơn cho tháng trước (Current Date - 1 month)
            var targetDate = DateTime.Today.AddMonths(-1);
            var year = targetDate.Year;
            var month = targetDate.Month;

            // 2. Gọi phương thức service
            // "system" là User ID đại diện cho việc Job tự động tạo
            await _invoiceService.RunMonthlyInvoiceCreationJobAsync("system", year, month);
        }
    }
}