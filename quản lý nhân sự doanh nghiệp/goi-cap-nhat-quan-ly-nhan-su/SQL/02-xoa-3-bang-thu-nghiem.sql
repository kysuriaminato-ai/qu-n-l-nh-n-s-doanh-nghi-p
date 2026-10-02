-- CHỈ chạy khi hệ thống mới đã chạy ổn.
-- Xóa 3 bảng thử nghiệm VaiTro, ChucNang, PhanQuyen tạo ở giai đoạn đầu (hệ thống mới không dùng).
-- Sau đó: mở Model1.edmx > chuột phải vào sơ đồ > Update Model from Database
--         > tab Refresh > Finish > lưu (Ctrl+S) > Build.
USE [Quản lý nhân sự];
GO
IF OBJECT_ID('dbo.PhanQuyen') IS NOT NULL DROP TABLE dbo.PhanQuyen;
IF OBJECT_ID('dbo.ChucNang')  IS NOT NULL DROP TABLE dbo.ChucNang;
IF OBJECT_ID('dbo.VaiTro')    IS NOT NULL DROP TABLE dbo.VaiTro;
GO
