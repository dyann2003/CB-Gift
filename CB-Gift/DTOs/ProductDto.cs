namespace CB_Gift.DTOs
{
    public class ProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? Describe { get; set; }
        public string? ItemLink { get; set; }
        public string? Template { get; set; }
        public int? Status { get; set; }

        public List<ProductVariantDto>? Variants { get; set; }
    }
    public class ProductCreateDto
    {
        public int CategoryId { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public int? Status { get; set; }
        public string? ItemLink { get; set; }
        public string? Describe { get; set; }
        public string? Template { get; set; }

        public List<ProductVariantCreateDto>? Variants { get; set; }
    }
    public class ProductUpdateDto
    {
        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }
        public int? CategoryId { get; set; }
        public string? Describe { get; set; }
        public string? ItemLink { get; set; }
        public string? Template { get; set; }
        public int? Status { get; set; }
        public List<ProductVariantUpdateDto>? Variants { get; set; }
    }
    public class ProductVariantDto
    {
        public int ProductVariantId { get; set; }
        public decimal? LengthCm { get; set; }
        public decimal? HeightCm { get; set; }
        public decimal? WidthCm { get; set; }
        public decimal? WeightGram { get; set; }
        public decimal? ShipCost { get; set; }
        public decimal? BaseCost { get; set; }
        public string? ThicknessMm { get; set; }
        public string? SizeInch { get; set; }
        public string? Layer { get; set; }
        public string? CustomShape { get; set; }
        public string? Sku { get; set; }
        public decimal? ExtraShipping { get; set; }
        public decimal? TotalCost { get; set; }
    }
    public class ProductVariantCreateDto
    {
        public decimal? LengthCm { get; set; }
        public decimal? HeightCm { get; set; }
        public decimal? WidthCm { get; set; }
        public decimal? WeightGram { get; set; }
        public decimal? ShipCost { get; set; }
        public decimal? BaseCost { get; set; }
        public string? ThicknessMm { get; set; }
        public string? SizeInch { get; set; }
        public string? Layer { get; set; }
        public string? CustomShape { get; set; }
        public string? Sku { get; set; }
        public decimal? ExtraShipping { get; set; }
        public decimal? TotalCost { get; set; }
    }
    public class ProductVariantUpdateDto : ProductVariantCreateDto
    {
        public int ProductVariantId { get; set; }
    }
    //Update Status theo danh sách
    public class BulkUpdateProductStatusDto
    {
        public List<int> ProductIds { get; set; } = new();
        public int Status { get; set; }  // 1 = bật, 0 = tắt
    }

}
