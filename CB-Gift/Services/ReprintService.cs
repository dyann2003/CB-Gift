using AutoMapper;
using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Models;
using CB_Gift.Models.Enums;
using CB_Gift.Services.IService;
using Microsoft.EntityFrameworkCore;

namespace CB_Gift.Services
{
    public class ReprintService : IReprintService
    {
        private readonly CBGiftDbContext _context;
        private readonly IOrderService _orderService;
        private readonly IMapper _mapper;

        public ReprintService(CBGiftDbContext context, IOrderService orderService, IMapper mapper)
        {
            _context = context;
            _orderService = orderService;
            _mapper = mapper;
        }

        // 1️ USER SUBMIT REPRINT REQUEST
        public async Task SubmitReprintAsync(ReprintSubmitDto dto)
        {
            var orderDetail = await _context.OrderDetails
                .Include(od => od.Order)
                .FirstOrDefaultAsync(od => od.OrderDetailId == dto.OriginalOrderDetailId);

            if (orderDetail == null)
                throw new Exception("OrderDetail not found.");

            // Đặt order vào trạng thái chờ xử lý reprint
            orderDetail.Order.StatusOrder = 17;
            orderDetail.ProductionStatus = ProductionStatus.HOLD_RP;

            // Tạo record Reprint
            var reprint = new Reprint
            {
                OriginalOrderDetailId = dto.OriginalOrderDetailId,
                Reason = dto.Reason,
                RequestedBy = dto.RequestedByUserId,
                RequestDate = DateTime.Now,
                Processed = false,
                Status = "Pending",
                ProofUrl = dto.ProofUrl
            };

            _context.Reprints.Add(reprint);
            await _context.SaveChangesAsync();
        }

        // 2️ MANAGER APPROVE
        public async Task ApproveReprintAsync(ReprintManagerDto dto)
        {
            // 1. Kiểm tra input
            if (dto.OriginalOrderDetailIds == null || !dto.OriginalOrderDetailIds.Any())
                throw new Exception("Danh sách sản phẩm trống.");

            // 2. Lấy danh sách OrderDetails theo List ID
            var listOrderDetails = await _context.OrderDetails
                .Include(od => od.Order)
                .ThenInclude(o => o.EndCustomer)
                .Where(od => dto.OriginalOrderDetailIds.Contains(od.OrderDetailId))
                .ToListAsync();

            if (!listOrderDetails.Any())
                throw new Exception("Không tìm thấy sản phẩm nào trong hệ thống.");

            // 3. VALIDATION QUAN TRỌNG: Kiểm tra xem tất cả có thuộc cùng 1 Order không?
            // (Không thể gộp sản phẩm của đơn A và đơn B vào chung 1 đơn in lại được)
            var firstOrderId = listOrderDetails.First().OrderId;
            if (listOrderDetails.Any(od => od.OrderId != firstOrderId))
            {
                throw new Exception("Lỗi: Không thể duyệt cùng lúc các sản phẩm thuộc các đơn hàng khác nhau.");
            }

            var originalOrder = listOrderDetails.First().Order;

            // 4. Lấy danh sách yêu cầu Reprint tương ứng để update trạng thái
            var listReprints = await _context.Reprints
                .Where(r => dto.OriginalOrderDetailIds.Contains(r.OriginalOrderDetailId))
                .ToListAsync();

            // Update trạng thái Reprint thành Approved
            foreach (var reprint in listReprints)
            {
                reprint.ManagerAcceptedBy = dto.ManagerUserId;
                reprint.Processed = true;
                reprint.Status = "Approved";
            }

            // 5. Chuẩn bị danh sách sản phẩm cho đơn mới
            var newDetailsList = new List<OrderDetailCreateRequest>();

            foreach (var item in listOrderDetails)
            {
                // Tìm lý do lỗi tương ứng của từng sản phẩm để ghi chú
                var reprintRequest = listReprints.FirstOrDefault(r => r.OriginalOrderDetailId == item.OrderDetailId);
                string reason = reprintRequest?.Reason ?? "N/A";

                newDetailsList.Add(new OrderDetailCreateRequest
                {
                    ProductVariantID = item.ProductVariantId,
                    Quantity = item.Quantity,
                    Price = 0, // Giá 0đ
                    LinkImg = item.LinkImg,
                    NeedDesign = item.NeedDesign,
                    LinkDesign = item.LinkFileDesign,
                    LinkThanksCard = item.LinkThanksCard,
                    Accessory = item.Accessory,
                    // Ghi chú rõ ràng lý do cho xưởng
                    Note = $"REPRINT item {item.ProductVariantId}. Lý do: {reason} \n Note cũ: {item.Note}",
                    ProductionStatus = ProductionStatus.READY_PROD
                });
            }

            // 6. Build Order mới
            var makeOrderDto = new MakeOrderDto
            {
                CustomerInfo = new EndCustomerCreateRequest
                {
                    Name = originalOrder.EndCustomer.Name,
                    Phone = originalOrder.EndCustomer.Phone,
                    Email = originalOrder.EndCustomer.Email,
                    Address = originalOrder.EndCustomer.Address,
                    Address1 = originalOrder.EndCustomer.Address1,
                    ZipCode = originalOrder.EndCustomer.Zipcode,
                    ShipState = originalOrder.EndCustomer.ShipState,
                    ShipCity = originalOrder.EndCustomer.ShipCity,
                    ShipCountry = originalOrder.EndCustomer.ShipCountry
                },
                OrderCreate = new OrderCreateRequest
                {
                    CostScan = 0,
                    OrderCode = originalOrder.OrderCode, // Tạm lấy mã cũ
                    ToDistrictId = originalOrder.ToDistrictId,
                    ToProvinceId = originalOrder.ToProvinceId,
                    ToWardCode = originalOrder.ToWardCode,
                    ProductionStatus = "Reprint",
                    PaymentStatus = "PAID",
                    ActiveTTS = false,
                    Tracking = string.Empty,
                    TotalCost = 0
                },
                OrderDetails = newDetailsList // Gán danh sách đã tạo ở trên
            };

            // 7. Gọi Service tạo đơn
            string sellerId = originalOrder.SellerUserId;

            var created = await _orderService.MakeOrder(makeOrderDto, sellerId);

            // 8. Cập nhật mã đơn hàng _RE và trạng thái đơn gốc
            var newOrder = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == created.OrderId);
            if (newOrder != null)
            {
                string oldCode = originalOrder.OrderCode;
                int index = oldCode.LastIndexOf("_RE");

                if (index == -1) newOrder.OrderCode = oldCode + "_RE";
                else
                {
                    string suffixStr = oldCode.Substring(index + 3);
                    if (string.IsNullOrEmpty(suffixStr)) newOrder.OrderCode = oldCode + "2";
                    else if (int.TryParse(suffixStr, out int version))
                    {
                        string baseCode = oldCode.Substring(0, index);
                        newOrder.OrderCode = $"{baseCode}_RE{version + 1}";
                    }
                    else newOrder.OrderCode = oldCode + "_RE";
                }

                newOrder.StatusOrder = 8;
                newOrder.ProductionStatus = "Reprint";
                newOrder.PaymentStatus = "PAID";
                newOrder.ActiveTts = false;
                newOrder.TotalCost = 0;
                newOrder.ToDistrictId = originalOrder.ToDistrictId;
                newOrder.ToProvinceId = originalOrder.ToProvinceId;
                newOrder.ToWardCode = originalOrder.ToWardCode;

                // Reset giá của từng sản phẩm con về 0
                if (newOrder.OrderDetails != null)
                {
                    foreach (var detail in newOrder.OrderDetails)
                    {
                        detail.Price = 0;
                    }
                }
            }

            // Cập nhật trạng thái đơn gốc
            originalOrder.StatusOrder = 15;

            await _context.SaveChangesAsync();
        }

        // 3️ MANAGER REJECT
        public async Task RejectReprintAsync(ReprintManagerDto dto)
        {
            if (dto.OriginalOrderDetailIds == null || !dto.OriginalOrderDetailIds.Any())
                throw new Exception("Danh sách sản phẩm trống.");

            // 1. Lấy danh sách yêu cầu Reprint cần từ chối
            var listReprints = await _context.Reprints
                .Include(r => r.OriginalOrderDetail)
                .ThenInclude(od => od.Order)
                .Where(r => dto.OriginalOrderDetailIds.Contains(r.OriginalOrderDetailId))
                .ToListAsync();

            if (!listReprints.Any())
                throw new Exception("Không tìm thấy yêu cầu in lại nào.");

            var firstOrderId = listReprints.First().OriginalOrderDetail.OrderId;
            if (listReprints.Any(r => r.OriginalOrderDetail.OrderId != firstOrderId))
            {
                throw new Exception("Lỗi: Vui lòng chỉ xử lý (Từ chối) các sản phẩm thuộc cùng 1 đơn hàng.");
            }

            // 2. Duyệt qua từng yêu cầu để cập nhật
            foreach (var reprint in listReprints)
            {
                reprint.Processed = true;
                reprint.ManagerAcceptedBy = dto.ManagerUserId; // Lấy từ DTO
                reprint.Status = "Rejected"; //Set trạng thái từ chối
                reprint.StaffRejectionReason = dto.RejectReason;

                // 3. Khôi phục trạng thái đơn hàng gốc (Nếu cần)
                if (reprint.OriginalOrderDetail?.Order != null)
                {
                    reprint.OriginalOrderDetail.Order.StatusOrder = 14;
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
