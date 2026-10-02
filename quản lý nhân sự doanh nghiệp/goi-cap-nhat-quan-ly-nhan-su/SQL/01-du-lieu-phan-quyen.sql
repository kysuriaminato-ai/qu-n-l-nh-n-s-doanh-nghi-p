-- Chạy trong cửa sổ query của database [Quản lý nhân sự]
-- (SQL Server Object Explorer > MAITHU > Databases > chuột phải "Quản lý nhân sự" > New Query)
-- Script chỉ THÊM dữ liệu còn thiếu, chạy lại nhiều lần vẫn an toàn.
USE [Quản lý nhân sự];
GO

-- 1) Vai trò
INSERT INTO Roles (RoleCode, RoleName, Description)
SELECT v.Code, v.Ten, v.Mota
FROM (VALUES
  ('ADMIN',  N'Quản trị hệ thống', N'Toàn quyền hệ thống'),
  ('HR',     N'Nhân sự',           N'Quản lý hồ sơ nhân viên và hợp đồng'),
  ('VIEWER', N'Chỉ xem',           N'Chỉ xem danh sách nhân sự')
) v(Code, Ten, Mota)
WHERE NOT EXISTS (SELECT 1 FROM Roles r WHERE r.RoleCode = v.Code);

-- 2) Danh sách quyền mà hệ thống đang dùng
INSERT INTO Permissions (Code, Module, Description)
SELECT v.Code, v.Module, v.Mota
FROM (VALUES
  ('Employee.View',    'Employee', N'Xem danh sách và hồ sơ nhân viên'),
  ('Employee.Create',  'Employee', N'Thêm nhân viên'),
  ('Employee.Edit',    'Employee', N'Sửa hồ sơ nhân viên'),
  ('Contract.View',    'Contract', N'Xem hợp đồng lao động'),
  ('Permission.Manage','System',   N'Quản lý vai trò, quyền và tài khoản')
) v(Code, Module, Mota)
WHERE NOT EXISTS (SELECT 1 FROM Permissions p WHERE p.Code = v.Code);

-- 3) Quyền mẫu cho vai trò HR và VIEWER (ADMIN không cần vì luôn có toàn quyền)
INSERT INTO RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, p.PermissionId
FROM Roles r
JOIN Permissions p ON p.Code IN ('Employee.View','Employee.Create','Employee.Edit','Contract.View')
WHERE r.RoleCode = 'HR'
  AND NOT EXISTS (SELECT 1 FROM RolePermissions x WHERE x.RoleId = r.RoleId AND x.PermissionId = p.PermissionId);

INSERT INTO RolePermissions (RoleId, PermissionId)
SELECT r.RoleId, p.PermissionId
FROM Roles r
JOIN Permissions p ON p.Code = 'Employee.View'
WHERE r.RoleCode = 'VIEWER'
  AND NOT EXISTS (SELECT 1 FROM RolePermissions x WHERE x.RoleId = r.RoleId AND x.PermissionId = p.PermissionId);

-- 4) Tài khoản quản trị để đăng nhập lần đầu: quantri / Admin@123
--    (mật khẩu được băm SHA-256, đổi ngay sau khi đăng nhập)
DECLARE @pw VARCHAR(255) = CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'Admin@123'), 2);
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'quantri')
    INSERT INTO Users (Username, PasswordHash, IsActive, CreatedAt) VALUES ('quantri', @pw, 1, GETDATE());
ELSE
    UPDATE Users SET PasswordHash = @pw, IsActive = 1 WHERE Username = 'quantri';

INSERT INTO UserRoles (UserId, RoleId)
SELECT u.UserId, r.RoleId
FROM Users u, Roles r
WHERE u.Username = 'quantri' AND r.RoleCode = 'ADMIN'
  AND NOT EXISTS (SELECT 1 FROM UserRoles x WHERE x.UserId = u.UserId AND x.RoleId = r.RoleId);
GO

-- Kiểm tra kết quả
SELECT r.RoleCode, r.RoleName, COUNT(rp.PermissionId) AS SoQuyen
FROM Roles r LEFT JOIN RolePermissions rp ON rp.RoleId = r.RoleId
GROUP BY r.RoleCode, r.RoleName;

SELECT u.Username, u.IsActive, r.RoleCode
FROM Users u LEFT JOIN UserRoles ur ON ur.UserId = u.UserId LEFT JOIN Roles r ON r.RoleId = ur.RoleId;
