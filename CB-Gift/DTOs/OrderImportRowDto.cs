using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace CB_Gift.Orders.Import
{
    public class OrderImportRowDto
    {
        public int RowNumber { get; set; }


        // Required
        public string? OrderCode { get; set; }
        public string? CustomerName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }


        public string? Address { get; set; }
        public string? Province { get; set; }
        public string? District { get; set; }
        public string? Ward { get; set; }


        // Product by SKU
        public string? SKU { get; set; }
        public int Quantity { get; set; } = 1;
        public string? Accessory { get; set; }
        public string? Note { get; set; }
        public string? LinkImg { get; set; }
        public string? LinkThanksCard { get; set; }
        public string? LinkFileDesign { get; set; }
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new List<string>();
    }
    public class OrderImportRowValidator2 : AbstractValidator<OrderImportRowDto>
    {
        public OrderImportRowValidator2(ReferenceDataCache cache)
        {
            RuleFor(x => x.OrderCode)
            .NotEmpty().WithMessage("OrderCode là bắt buộc.");


            RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("CustomerName là bắt buộc.");


            RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone là bắt buộc.")
            .MinimumLength(7).WithMessage("Phone không hợp lệ");


            RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("SKU là bắt buộc.")
            .Must(sku => sku != null && cache.ProductVariants.Any(v => !string.IsNullOrWhiteSpace(v.Sku) && v.Sku.Equals(sku.Trim(), StringComparison.OrdinalIgnoreCase)))
            .WithMessage("SKU không tồn tại trong hệ thống.");


            RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity phải lớn hơn 0.");


            RuleFor(x => x.Province)
             .Cascade(CascadeMode.Stop)
             .NotEmpty().WithMessage("Province là bắt buộc.")
             // Thay vì dùng cache.Provinces.Any(...), ta dùng cache.FindProvinceId(...)
             .Must(p => !string.IsNullOrEmpty(p) && cache.FindProvinceId(p).HasValue)
             .WithMessage("Province không hợp lệ (Không tìm thấy trong hệ thống GHN).");


            // District/Ward are optional but if provided validate minimal existence by non-empty
            RuleFor(x => x.District)
            .NotEmpty().WithMessage("District là bắt buộc.");


            RuleFor(x => x.Ward)
            .NotEmpty().WithMessage("Ward là bắt buộc.");
        }


        private static string NormalizeText(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            return s.Trim().ToLowerInvariant();
        }
    }
}