
Insert into Category (CategoryName,Status) Values ('Suncatcher',1),('Wooden',1),('Acrylic',1),('NightLight',1);


INSERT INTO Product (CategoryID, ProductName, ProductCode, Status, ItemLink, Describe, Template)
VALUES (1, N'Custom Shape Suncatcher', '', 1, N'https://example.com/itemA', N'- The wood ornament is made of environmental-friendly fiber wood, nontoxic, odor-free, exquisite craftsmanship & stylish design. The acrylic one is made from acrylic plexiglass.
- Printed in one side
- You can hang it on your rearview mirror as a car decoration, auto decoration, vehicle accessory, automobile accessory, or display it as home decoration wherever you like
', N'Template A'),
        (1, N'Stain Glass Suncatcher', '', 1, N'https://example.com/itemb', N'-High-graMaterialde 
0.5-cm thick glass. 
The material is sturdy with a unique texture, offering clarity and durability that stands the test of time.
-Hanging Chain Length: 38cm
', N'Template A'),
		(1, N'Custom Shape 2-LAYER Suncatcher', '', 1, N'https://example.com/itemc', N'in một mặt,1 LAYER gỗ , 1 LAYER mica, đóng hộp catton', N'Template A'),
		(1, N'Custom Shape 2-LAYER Suncatcher', '', 1, N'https://example.com/itemd', N'1 LAYER gỗ , 1 LAYER mica, đóng hộp catton', N'Template A'),
		(1, N'Custom Shape 2-LAYER Suncatcher', '', 1, N'https://example.com/iteme', N'1 LAYER gỗ , 1 LAYER mica, đóng hộp catton', N'Template A'),
		(1, N'Custom Shape Acrylic Suncatcher', '', 1, N'https://example.com/itemf', N'Tùy chỉnh được kích shape ,
1 LAYER Acrylic, đóng hộp catton
In Hologram: +1,5$', N'Template A'),
		(1, N'Flower Book Suncatcher ACRYLIC', '', 1, N'https://example.com/itemg', N'1 LAYER Acrylic, đóng hộp catton
In Hologram: +1,5$', N'Template A'),
(1, N'Custom Shape Acrylic Suncatcher Mix Tag', '', 1, N'https://example.com/itemg', N'1 LAYER Acrylic 3mm, đóng hộp catton
- In Hologram: +1,5$
- thêm 2 xích treo: + 1.0$
- thêm 3 xích treo: + 1.5$
- thêm 4 xích treo: + 2.0$
- thêm 5 xích treo: + 2.5$
- thêm 6 xích treo: + 3.0$', N'Template A');

-- insert values endcustomer
INSERT INTO EndCustomer (Name, Phone, Email, Address, Address1, ShipCity, ShipState, Zipcode, ShipCountry)
VALUES 
(N'Andres Madriz', '15593468693', NULL, N'2716 South Dollner Street', NULL, N'Visalia', N'California', '93277', 'US'),
(N'Danielle Weand', '14848244512', NULL, N'109 Sunrise Dr', NULL, N'Pottstown', N'Pennsylvania', '19464', 'US'),
(N'Jennifer Leavitt', '14356548244', NULL, N'135 E 400 S', NULL, N'Kamas', N'Utah', '84036', 'US'),
(N'Matt Bray', '17206249847', NULL, N'2503 S PAGOSA CT', NULL, N'Aurora', N'Colorado', '80013', 'US'),
(N'tippytappies89 Phillips', '19729655415', NULL, N'8309 Montecito Dr', NULL, N'Denton', N'Texas', '76210', 'US');

-- insert ProductVariant
INSERT INTO ProductVariant
(
    ProductID, LengthCM, HeightCM, WidthCM, WeightGram,
    ShipCost, BaseCost, ThicknessMM, SizeInch, Layer,
    CustomShape, SKU, ExtraShipping, TotalCost
)
VALUES
(1, 11.00, 11.00, 3.00, 72.60,
 6.30, 3.52, '6MM', '3.54IN', '2LAYER',
 'SUN-WOOD-ACR-MIX', 'CBG-SUN-WOOD-ACR-MIX-2LAYER-3.54IN-6MM', 2.8, 9.82),

(1, 12.00, 12.00, 3.00, 86.40,
 6.75, 3.85, '6MM', '4IN', '2LAYER',
 'SUN-WOOD-ACR-MIX', 'CBG-SUN-WOOD-ACR-MIX-2LAYER-4IN-6MM', 3.19, 10.60),

(1, 15.00, 15.00, 3.00, 135.00,
 7.36, 4.24, '6MM', '5IN', '2LAYER',
 'SUN-WOOD-ACR-MIX', 'CBG-SUN-WOOD-ACR-MIX-2LAYER-5IN-6MM', 3.83, 11.60),

(1, 17.00, 17.00, 3.00, 200.00,
 8.15, 5.50, '6MM', '6IN', '2LAYER',
 'SUN-WOOD-ACR-MIX', 'CBG-SUN-WOOD-ACR-MIX-2LAYER-6IN-6MM', 4.25, 13.65),

(1, 22.00, 22.00, 3.00, 300.00,
 9.85, 6.35, '6MM', '8IN', '2LAYER',
 'SUN-WOOD-ACR-MIX', 'CBG-SUN-WOOD-ACR-MIX-2LAYER-8IN-6MM', 4.9, 16.20),

(1, 27.00, 27.00, 3.00, 500.00,
 11.25, 8.20, '6MM', '10IN', '2LAYER',
 'SUN-WOOD-ACR-MIX', 'CBG-SUN-WOOD-ACR-MIX-2LAYER-10IN-6MM', 7.18, 19.45),

(1, 33.00, 33.00, 3.00, 700.00,
 13.95, 10.50, '6MM', '12IN', '2LAYER',
 'SUN-WOOD-ACR-MIX', 'CBG-SUN-WOOD-ACR-MIX-2LAYER-12IN-6MM', 8.2, 24.45);


 -- insert Order
 INSERT INTO [Order]
(
    OrderCode, OrderDate, EndCustomerID, SellerUserId,
    CreationDate, CostScan, ActiveTTS, TotalCost,
    ProductionStatus, PaymentStatus, Tracking, StatusOrder
)
VALUES
(
    'ORD0001',                -- OrderCode
    GETDATE(),                -- OrderDate
    1,                        -- EndCustomerID (ví dụ =1)
    '5a5d7116-dd55-40e8-b749-9d14f7143269',             -- SellerUserId
    GETDATE(),                -- CreationDate
    0.00,                     -- CostScan
    0,                        -- ActiveTTS (bit: 0/1)
    100.00,                   -- TotalCost
    'CREATED',                -- ProductionStatus
    'UNPAID',                 -- PaymentStatus
    NULL,                     -- Tracking
    1                         -- StatusOrder (map với StatusId: 1 = CREATED)
),
(
    'ORD0002',                -- OrderCode
    GETDATE(),                -- OrderDate
    1,                        -- EndCustomerID (ví dụ =1)
    '5a5d7116-dd55-40e8-b749-9d14f7143269',             -- SellerUserId
    GETDATE(),                -- CreationDate
    1,                     -- CostScan
    1,                        -- ActiveTTS (bit: 0/1)
    50,                   -- TotalCost
    'CREATED',                -- ProductionStatus
    'UNPAID',                 -- PaymentStatus
    NULL,                     -- Tracking
    1                         -- StatusOrder (map với StatusId: 1 = CREATED)
)
;

 --insert OrderDetail
 INSERT INTO OrderDetail
(
    OrderID, ProductVariantID, LinkImg, LinkThanksCard, LinkFileDesign,
    Accessory, Note, CreatedDate, Quantity, NeedDesign,
    AssignedDesignerUserId, AssignedAt, Price
)
VALUES
(
    1,                                 -- OrderID
    1,                                 -- ProductVariantID (variant bạn đã insert trước đó)
    'https://example.com/image.jpg',   -- LinkImg
    'https://example.com/thanks.jpg',  -- LinkThanksCard
    'https://example.com/design.psd',  -- LinkFileDesign
    'Keychain',                        -- Accessory
    'Urgent order, ship fast',         -- Note
    GETDATE(),                         -- CreatedDate
    2,                                 -- Quantity
    0,                                 -- NeedDesign (0 = không cần, 1 = cần)
    NULL,                              -- AssignedDesignerUserId
    NULL,                              -- AssignedAt
    19.45                              -- Price (theo TotalCost hoặc đơn giá bạn set)
),
(
    1,                                 -- OrderID
    2,                                 -- ProductVariantID (variant bạn đã insert trước đó)
    'https://example.com/image.jpg',   -- LinkImg
    'https://example.com/thanks.jpg',  -- LinkThanksCard
    'https://example.com/design.psd',  -- LinkFileDesign
    'Keychain',                        -- Accessory
    'Urgent order, ship fast',         -- Note
    GETDATE(),                         -- CreatedDate
    2,                                 -- Quantity
    0,                                 -- NeedDesign (0 = không cần, 1 = cần)
    NULL,                              -- AssignedDesignerUserId
    NULL,                              -- AssignedAt
    0                              -- Price (theo TotalCost hoặc đơn giá bạn set)
),
(
    2,                                 -- OrderID
    3,                                 -- ProductVariantID (variant bạn đã insert trước đó)
    'https://example.com/image.jpg',   -- LinkImg
    'https://example.com/thanks.jpg',  -- LinkThanksCard
    'https://example.com/design.psd',  -- LinkFileDesign
    'Keychain',                        -- Accessory
    'Urgent order, ship fast',         -- Note
    GETDATE(),                         -- CreatedDate
    1,                                 -- Quantity
    0,                                 -- NeedDesign (0 = không cần, 1 = cần)
    NULL,                              -- AssignedDesignerUserId
    NULL,                              -- AssignedAt
    0                              -- Price (theo TotalCost hoặc đơn giá bạn set)
);
