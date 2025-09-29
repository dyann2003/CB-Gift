using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CB_Gift.Models; // nơi đặt các entity scaffold (Category, Order, ...)

namespace CB_Gift.Data
{
    public partial class CBGiftDbContext : IdentityDbContext<AppUser, IdentityRole, string>
    {
        public CBGiftDbContext(DbContextOptions<CBGiftDbContext> options) : base(options) { }

        // DbSet nghiệp vụ
        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<DesignerSeller> DesignerSellers { get; set; }
        public virtual DbSet<EndCustomer> EndCustomers { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<OrderDetail> OrderDetails { get; set; }
        public virtual DbSet<OrderDetailDesign> OrderDetailDesigns { get; set; }
        public virtual DbSet<OrderStatus> OrderStatuses { get; set; }
        public virtual DbSet<Plan> Plans { get; set; }
        public virtual DbSet<PlanDetail> PlanDetails { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<ProductVariant> ProductVariants { get; set; }
        public virtual DbSet<Qc> Qcs { get; set; }
        public virtual DbSet<Tag> Tags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // BẮT BUỘC: để Identity tạo mapping cho AspNetUsers/roles...
            base.OnModelCreating(modelBuilder);

            // ====== MAPPING TỪ Scaffolded CBGiftContext ======

            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Category");

                entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
                entity.Property(e => e.CategoryCode).HasMaxLength(50);
                entity.Property(e => e.CategoryName)
                    .IsRequired()
                    .HasMaxLength(100);
            });

            modelBuilder.Entity<DesignerSeller>(entity =>
            {
                entity.HasKey(e => new { e.DesignerUserId, e.SellerUserId });

                entity.ToTable("DesignerSeller");

                entity.HasIndex(e => e.SellerUserId, "IX_DesignerSeller_Seller");

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("(getdate())")
                    .HasAnnotation("Relational:DefaultConstraintName", "DF_DesignerSeller_CreatedAt")
                    .HasColumnType("datetime");
                entity.Property(e => e.CreatedByUserId).HasMaxLength(450);
            });

            modelBuilder.Entity<EndCustomer>(entity =>
            {
                entity.HasKey(e => e.CustId);

                entity.ToTable("EndCustomer");

                entity.Property(e => e.CustId).HasColumnName("CustID");
                entity.Property(e => e.Address).HasMaxLength(200);
                entity.Property(e => e.Address1).HasMaxLength(200);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.ShipCity).HasMaxLength(50);
                entity.Property(e => e.ShipCountry).HasMaxLength(50);
                entity.Property(e => e.ShipState).HasMaxLength(50);
                entity.Property(e => e.Zipcode).HasMaxLength(20);
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("Order");

                entity.HasIndex(e => e.EndCustomerId, "IX_Order_EndCustomerID");
                entity.HasIndex(e => e.SellerUserId, "IX_Order_SellerUserId");
                entity.HasIndex(e => e.StatusOrder, "IX_Order_StatusOrder");

                entity.Property(e => e.OrderId).HasColumnName("OrderID");
                entity.Property(e => e.ActiveTts).HasColumnName("ActiveTTS");
                entity.Property(e => e.CostScan).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.CreationDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasAnnotation("Relational:DefaultConstraintName", "DF_Order_CreationDate")
                    .HasColumnType("datetime");
                entity.Property(e => e.EndCustomerId).HasColumnName("EndCustomerID");
                entity.Property(e => e.OrderCode).HasMaxLength(100);
                entity.Property(e => e.OrderDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasAnnotation("Relational:DefaultConstraintName", "DF_Order_OrderDate")
                    .HasColumnType("datetime");
                entity.Property(e => e.PaymentStatus).HasMaxLength(50);
                entity.Property(e => e.ProductionStatus).HasMaxLength(50);
                entity.Property(e => e.SellerUserId).IsRequired();
                entity.Property(e => e.StatusOrder)
                    .HasDefaultValue(1)
                    .HasAnnotation("Relational:DefaultConstraintName", "DF_Order_StatusOrder");
                entity.Property(e => e.TotalCost).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.Tracking)
                    .HasMaxLength(200)
                    .IsUnicode(false);

                entity.HasOne(d => d.EndCustomer).WithMany(p => p.Orders)
                    .HasForeignKey(d => d.EndCustomerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Order_EndCustomer");

                entity.HasOne(d => d.StatusOrderNavigation).WithMany(p => p.Orders)
                    .HasForeignKey(d => d.StatusOrder)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Order_OrderStatus");
            });

            modelBuilder.Entity<OrderDetail>(entity =>
            {
                entity.ToTable("OrderDetail");

                entity.HasIndex(e => e.AssignedDesignerUserId, "IX_OrderDetail_AssignedDesignerUserId");
                entity.HasIndex(e => e.NeedDesign, "IX_OrderDetail_NeedDesign");
                entity.HasIndex(e => e.OrderId, "IX_OrderDetail_OrderID");
                entity.HasIndex(e => e.ProductVariantId, "IX_OrderDetail_ProductVariantID");

                entity.Property(e => e.OrderDetailId).HasColumnName("OrderDetailID");
                entity.Property(e => e.Accessory).HasMaxLength(200);
                entity.Property(e => e.AssignedAt).HasColumnType("datetime");
                entity.Property(e => e.CreatedDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasAnnotation("Relational:DefaultConstraintName", "DF_OrderDetail_CreatedDate")
                    .HasColumnType("datetime");
                entity.Property(e => e.LinkFileDesign).HasMaxLength(200);
                entity.Property(e => e.LinkImg).HasMaxLength(200);
                entity.Property(e => e.LinkThanksCard).HasMaxLength(200);
                entity.Property(e => e.NeedDesign)
                    .HasDefaultValue(true)
                    .HasAnnotation("Relational:DefaultConstraintName", "DF_OrderDetail_NeedDesign");
                entity.Property(e => e.Note).HasMaxLength(200);
                entity.Property(e => e.OrderId).HasColumnName("OrderID");
                entity.Property(e => e.ProductVariantId).HasColumnName("ProductVariantID");

                entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                    .HasForeignKey(d => d.OrderId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_OrderDetail_Order");

                entity.HasOne(d => d.ProductVariant).WithMany(p => p.OrderDetails)
                    .HasForeignKey(d => d.ProductVariantId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_OrderDetail_Variant");
            });

            modelBuilder.Entity<OrderDetailDesign>(entity =>
            {
                entity.HasKey(e => e.DesignId).HasName("PK__OrderDet__32B8E17F610ED9F4");

                entity.ToTable("OrderDetailDesign");

                entity.HasIndex(e => new { e.DesignerUserId, e.CreatedAt }, "IX_ODDesign_Designer");
                entity.HasIndex(e => e.OrderDetailId, "IX_ODDesign_OrderDetailID");
                entity.HasIndex(e => e.OrderDetailId, "UX_ODDesign_Final")
                    .IsUnique()
                    .HasFilter("([IsFinal]=(1))");

                entity.Property(e => e.DesignId).HasColumnName("DesignID");
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("(getdate())")
                    .HasAnnotation("Relational:DefaultConstraintName", "DF_OrderDetailDesign_CreatedAt")
                    .HasColumnType("datetime");
                entity.Property(e => e.DesignerUserId).IsRequired();
                entity.Property(e => e.FileUrl)
                    .IsRequired()
                    .HasMaxLength(500);
                entity.Property(e => e.IsFinal)
                    .HasAnnotation("Relational:DefaultConstraintName", "DF_OrderDetailDesign_IsFinal");
                entity.Property(e => e.Note).HasMaxLength(400);
                entity.Property(e => e.OrderDetailId).HasColumnName("OrderDetailID");

                entity.HasOne(d => d.OrderDetail).WithOne(p => p.OrderDetailDesign)
                    .HasForeignKey<OrderDetailDesign>(d => d.OrderDetailId)
                    .HasConstraintName("FK_ODDesign_OrderDetail");
            });

            modelBuilder.Entity<OrderStatus>(entity =>
            {
                entity.HasKey(e => e.StatusId);

                entity.ToTable("OrderStatus");

                entity.Property(e => e.StatusId).ValueGeneratedNever();
                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(50);
                entity.Property(e => e.NameVi)
                    .IsRequired()
                    .HasMaxLength(100);
            });

            modelBuilder.Entity<Plan>(entity =>
            {
                entity.HasIndex(e => e.ApproveByUserId, "IX_Plans_ApproveByUserId");
                entity.HasIndex(e => e.CreateByUserId, "IX_Plans_CreateByUserId");

                entity.Property(e => e.PlanId).HasColumnName("PlanID");
                entity.Property(e => e.ApproveDate).HasColumnType("datetime");
                entity.Property(e => e.CreateDate).HasColumnType("datetime");
                entity.Property(e => e.StartDatePlan).HasColumnType("datetime");
                entity.Property(e => e.StopDatePlan).HasColumnType("datetime");
            });

            modelBuilder.Entity<PlanDetail>(entity =>
            {
                entity.ToTable("PlanDetail");

                entity.HasIndex(e => e.OrderDetailId, "IX_PlanDetail_OrderDetailID");
                entity.HasIndex(e => e.PlanId, "IX_PlanDetail_PlanID");

                entity.Property(e => e.PlanDetailId).HasColumnName("PlanDetailID");
                entity.Property(e => e.OrderDetailId).HasColumnName("OrderDetailID");
                entity.Property(e => e.PlanId).HasColumnName("PlanID");

                entity.HasOne(d => d.OrderDetail).WithMany(p => p.PlanDetails)
                    .HasForeignKey(d => d.OrderDetailId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PlanDetail_OrderDetail");

                entity.HasOne(d => d.Plan).WithMany(p => p.PlanDetails)
                    .HasForeignKey(d => d.PlanId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PlanDetail_Plan");
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("Product");

                entity.HasIndex(e => e.CategoryId, "IX_Product_CategoryID");

                entity.Property(e => e.ProductId).HasColumnName("ProductID");
                entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
                entity.Property(e => e.Describe).HasMaxLength(500);
                entity.Property(e => e.ItemLink).HasMaxLength(200);
                entity.Property(e => e.ProductCode).HasMaxLength(50);
                entity.Property(e => e.ProductName).HasMaxLength(100);
                entity.Property(e => e.Template).HasMaxLength(500);

                entity.HasOne(d => d.Category).WithMany(p => p.Products)
                    .HasForeignKey(d => d.CategoryId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Product_Category");

                entity.HasMany(d => d.Tags).WithMany(p => p.Products)
                    .UsingEntity<Dictionary<string, object>>(
                        "TagsProduct",
                        r => r.HasOne<Tag>().WithMany()
                            .HasForeignKey("TagsId")
                            .HasConstraintName("FK_Tags_Product_Tags"),
                        l => l.HasOne<Product>().WithMany()
                            .HasForeignKey("ProductId")
                            .HasConstraintName("FK_Tags_Product_Product"),
                        j =>
                        {
                            j.HasKey("ProductId", "TagsId");
                            j.ToTable("Tags_Product");
                            j.IndexerProperty<int>("ProductId").HasColumnName("ProductID");
                            j.IndexerProperty<int>("TagsId").HasColumnName("TagsID");
                        });
            });

            modelBuilder.Entity<ProductVariant>(entity =>
            {
                entity.ToTable("ProductVariant");

                entity.HasIndex(e => e.ProductId, "IX_ProductVariant_ProductID");

                entity.Property(e => e.ProductVariantId).HasColumnName("ProductVariantID");
                entity.Property(e => e.BaseCost).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.CustomShape)
                    .IsRequired()
                    .HasMaxLength(200);
                entity.Property(e => e.ExtraShipping).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.HeightCm)
                    .HasColumnType("decimal(10, 2)")
                    .HasColumnName("HeightCM");
                entity.Property(e => e.Layer)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);
                entity.Property(e => e.LengthCm)
                    .HasColumnType("decimal(10, 2)")
                    .HasColumnName("LengthCM");
                entity.Property(e => e.ProductId).HasColumnName("ProductID");
                entity.Property(e => e.ShipCost).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.SizeInch)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false);
                entity.Property(e => e.Sku)
                    .HasMaxLength(500)
                    .HasColumnName("SKU");
                entity.Property(e => e.ThicknessMm)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("ThicknessMM");
                entity.Property(e => e.TotalCost).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.WeightGram).HasColumnType("decimal(10, 2)");
                entity.Property(e => e.WidthCm)
                    .HasColumnType("decimal(10, 2)")
                    .HasColumnName("WidthCM");

                entity.HasOne(d => d.Product).WithMany(p => p.ProductVariants)
                    .HasForeignKey(d => d.ProductId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Variant_Product");
            });

            modelBuilder.Entity<Qc>(entity =>
            {
                entity.HasKey(e => e.QccheckId);

                entity.ToTable("QC");

                entity.HasIndex(e => e.CheckedByUserId, "IX_QC_CheckedByUserId");
                entity.HasIndex(e => e.PlanDetailId, "IX_QC_PlanDetailID");

                entity.Property(e => e.QccheckId).HasColumnName("QCCheckID");
                entity.Property(e => e.CheckedDate)
                    .HasDefaultValueSql("(getdate())")
                    .HasAnnotation("Relational:DefaultConstraintName", "DF_QC_CheckedDate")
                    .HasColumnType("datetime");
                entity.Property(e => e.PlanDetailId).HasColumnName("PlanDetailID");
                entity.Property(e => e.Remark).HasMaxLength(400);

                entity.HasOne(d => d.PlanDetail).WithMany(p => p.Qcs)
                    .HasForeignKey(d => d.PlanDetailId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_QC_PlanDetail");
            });

            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(e => e.TagsId);

                entity.Property(e => e.TagsId).HasColumnName("TagsID");
                entity.Property(e => e.TagCode)
                    .HasMaxLength(150)
                    .IsUnicode(false);
                entity.Property(e => e.TagName).HasMaxLength(100);
            });

            // Nếu bạn từng có OnModelCreatingPartial trong scaffold, có thể bỏ qua hoặc giữ partial trống.
            OnModelCreatingPartial(modelBuilder);
        }

        // Tuỳ chọn: để tương thích với từ khóa 'partial' trong scaffold
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
