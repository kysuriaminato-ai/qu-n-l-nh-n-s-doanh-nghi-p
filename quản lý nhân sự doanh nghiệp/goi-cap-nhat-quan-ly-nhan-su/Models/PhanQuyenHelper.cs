using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace quản_lý_nhân_sự_doanh_nghiệp.Models
{
    public class ThongTinQuyen
    {
        public bool HopLe;                 // tài khoản còn tồn tại và đang hoạt động
        public bool QuanTri;               // có vai trò quản trị
        public HashSet<string> Quyen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public List<int> ChiNhanh;         // null = xem được mọi chi nhánh
    }

    public static class PhanQuyenHelper
    {
        // RoleCode của vai trò quản trị trong bảng Roles
        public const string MaQuanTri = "ADMIN";

        public static int? UserIdHienTai()
        {
            var ctx = HttpContext.Current;
            if (ctx == null || ctx.Session == null) return null;
            return ctx.Session["UserId"] as int?;
        }

        public static bool DaDangNhap()
        {
            return UserIdHienTai() != null && ThongTin().HopLe;
        }

        public static bool CoQuyen(string maQuyen)
        {
            var t = ThongTin();
            return t.HopLe && (t.QuanTri || t.Quyen.Contains(maQuyen));
        }

        public static bool LaQuanTri() { return ThongTin().QuanTri; }

        // Đọc CSDL một lần cho mỗi request nên quyền luôn mới nhất
        public static ThongTinQuyen ThongTin()
        {
            var ctx = HttpContext.Current;
            var cache = ctx.Items["thongTinQuyen"] as ThongTinQuyen;
            if (cache != null) return cache;

            var kq = new ThongTinQuyen();
            var uid = UserIdHienTai();
            if (uid != null)
            {
                using (var db = new Quản_lý_nhân_sựEntities3())
                {
                    int id = uid.Value;
                    var u = db.Users.Include("Roles.Permissions").Include("Roles.DataScopes")
                        .FirstOrDefault(x => x.UserId == id && x.IsActive == true);
                    if (u != null)
                    {
                        kq.HopLe = true;
                        kq.QuanTri = u.Roles.Any(r => r.RoleCode == MaQuanTri);
                        foreach (var r in u.Roles)
                            foreach (var p in r.Permissions) kq.Quyen.Add(p.Code);

                        if (!kq.QuanTri)
                        {
                            // Vai trò nào chưa cấu hình phạm vi thì không bị giới hạn chi nhánh
                            var ids = u.Roles.SelectMany(r => r.DataScopes).Select(s => s.BranchId).Distinct().ToList();
                            if (ids.Count > 0) kq.ChiNhanh = ids;
                        }
                    }
                }
            }
            ctx.Items["thongTinQuyen"] = kq;
            return kq;
        }

        // Chỉ giữ nhân viên thuộc chi nhánh mà người dùng được xem (theo phòng ban hiện tại)
        public static IQueryable<Employee> GioiHanPhamVi(this IQueryable<Employee> q)
        {
            var br = ThongTin().ChiNhanh;
            if (br == null) return q;
            return q.Where(e => e.EmpPlacements.Any(p => p.IsCurrent == true && br.Contains(p.Department.BranchId)));
        }

        public static IQueryable<Contract> GioiHanPhamVi(this IQueryable<Contract> q, Quản_lý_nhân_sựEntities3 db)
        {
            var br = ThongTin().ChiNhanh;
            if (br == null) return q;
            return q.Where(c => db.EmpPlacements.Any(p => p.EmployeeId == c.EmployeeId
                                                       && p.IsCurrent == true
                                                       && br.Contains(p.Department.BranchId)));
        }
    }
}
