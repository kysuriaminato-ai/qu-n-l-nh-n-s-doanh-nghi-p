using System;
using System.Security.Cryptography;
using System.Text;

namespace quản_lý_nhân_sự_doanh_nghiệp.Models
{
    public static class MatKhauHelper
    {
        // Băm SHA-256, trả về chuỗi hex chữ hoa (trùng với HASHBYTES trong SQL Server)
        public static string Bam(string matKhau)
        {
            using (var sha = SHA256.Create())
            {
                var b = sha.ComputeHash(Encoding.UTF8.GetBytes(matKhau ?? ""));
                return BitConverter.ToString(b).Replace("-", "");
            }
        }

        // Chấp nhận: SHA-256 hex, SHA-256 base64, hoặc mật khẩu thường (dữ liệu cũ).
        // Khi mọi tài khoản đã dùng SHA-256, hãy bỏ dòng "daLuu == nhap".
        public static bool KiemTra(string nhap, string daLuu)
        {
            if (string.IsNullOrEmpty(nhap) || string.IsNullOrEmpty(daLuu)) return false;
            if (daLuu == nhap) return true;

            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(nhap));
                var hex = BitConverter.ToString(bytes).Replace("-", "");
                if (string.Equals(hex, daLuu, StringComparison.OrdinalIgnoreCase)) return true;
                if (Convert.ToBase64String(bytes) == daLuu) return true;
            }
            return false;
        }
    }
}
