using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using quản_lý_nhân_sự_doanh_nghiệp.Models;

namespace quản_lý_nhân_sự_doanh_nghiệp.Controllers
{
    [KiemTraQuyen("Permission.Manage")]
    public class PhanQuyenController : Controller
    {
        private Quản_lý_nhân_sựEntities3 db = new Quản_lý_nhân_sựEntities3();

        // ================= Vai trò, quyền, phạm vi =================
        public ActionResult Index(int? id)
        {
            var vaiTros = db.Roles.OrderBy(r => r.RoleName).ToList();
            var chon = vaiTros.FirstOrDefault(r => r.RoleId == id) ?? vaiTros.FirstOrDefault();

            var vm = new PhanQuyenViewModel
            {
                VaiTros = vaiTros,
                VaiTroChon = chon,
                Quyens = db.Permissions.OrderBy(p => p.Module).ThenBy(p => p.Code).ToList(),
                ChiNhanhs = db.Branches.OrderBy(b => b.BranchName).ToList(),
                DaCap = new HashSet<int>(),
                PhamVi = new HashSet<int>()
            };

            if (chon != null)
            {
                int rid = chon.RoleId;
                vm.DaCap = new HashSet<int>(db.Roles.Where(r => r.RoleId == rid)
                    .SelectMany(r => r.Permissions).Select(p => p.PermissionId));
                vm.PhamVi = new HashSet<int>(db.DataScopes.Where(s => s.RoleId == rid).Select(s => s.BranchId));
                vm.Khoa = chon.RoleCode == PhanQuyenHelper.MaQuanTri;
                vm.SoNguoi = db.Users.Count(u => u.Roles.Any(r => r.RoleId == rid));
            }
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Luu(int maVaiTro, int[] quyen)
        {
            var vt = db.Roles.Include("Permissions").FirstOrDefault(r => r.RoleId == maVaiTro);
            if (vt == null) return HttpNotFound();
            if (vt.RoleCode == PhanQuyenHelper.MaQuanTri)
            {
                TempData["Loi"] = "Vai trò quản trị luôn có toàn quyền và không thể chỉnh sửa.";
                return RedirectToAction("Index", new { id = maVaiTro });
            }

            var ids = (quyen ?? new int[0]).Distinct().ToList();
            vt.Permissions.Clear();
            foreach (var p in db.Permissions.Where(p => ids.Contains(p.PermissionId)).ToList())
                vt.Permissions.Add(p);
            db.SaveChanges();

            TempData["Ok"] = "Đã lưu quyền cho vai trò " + vt.RoleName + ".";
            return RedirectToAction("Index", new { id = maVaiTro });
        }

        // Giới hạn dữ liệu theo chi nhánh. Không tick chi nhánh nào = xem tất cả.
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult LuuPhamVi(int maVaiTro, int[] chiNhanh)
        {
            var vt = db.Roles.Find(maVaiTro);
            if (vt == null) return HttpNotFound();
            if (vt.RoleCode == PhanQuyenHelper.MaQuanTri)
            {
                TempData["Loi"] = "Vai trò quản trị luôn xem được mọi chi nhánh.";
                return RedirectToAction("Index", new { id = maVaiTro });
            }

            var ids = (chiNhanh ?? new int[0]).Distinct().ToList();
            var hopLe = db.Branches.Where(b => ids.Contains(b.BranchId)).Select(b => b.BranchId).ToList();

            db.DataScopes.RemoveRange(db.DataScopes.Where(s => s.RoleId == maVaiTro).ToList());
            foreach (var b in hopLe) db.DataScopes.Add(new DataScope { RoleId = maVaiTro, BranchId = b });
            db.SaveChanges();

            TempData["Ok"] = hopLe.Count == 0
                ? "Vai trò " + vt.RoleName + " xem được tất cả chi nhánh."
                : "Đã giới hạn vai trò " + vt.RoleName + " trong " + hopLe.Count + " chi nhánh.";
            return RedirectToAction("Index", new { id = maVaiTro });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult TaoVaiTro(string roleCode, string roleName, string moTa)
        {
            roleCode = (roleCode ?? "").Trim().ToUpperInvariant();
            roleName = (roleName ?? "").Trim();
            if (roleCode == "" || roleName == "")
            {
                TempData["Loi"] = "Nhập mã và tên vai trò.";
                return RedirectToAction("Index");
            }
            if (roleCode.Length > 50 || roleName.Length > 100)
            {
                TempData["Loi"] = "Mã tối đa 50 ký tự, tên tối đa 100 ký tự.";
                return RedirectToAction("Index");
            }
            if (db.Roles.Any(r => r.RoleCode == roleCode || r.RoleName == roleName))
            {
                TempData["Loi"] = "Đã có vai trò cùng mã hoặc cùng tên. Hãy chọn tên khác.";
                return RedirectToAction("Index");
            }

            var vt = new Role { RoleCode = roleCode, RoleName = roleName, Description = string.IsNullOrWhiteSpace(moTa) ? null : moTa.Trim() };
            db.Roles.Add(vt);
            db.SaveChanges();
            TempData["Ok"] = "Đã tạo vai trò " + roleName + ". Hãy tick quyền cho vai trò này.";
            return RedirectToAction("Index", new { id = vt.RoleId });
        }

        // ================= Người dùng =================
        public ActionResult NguoiDung()
        {
            var users = db.Users.Include("Roles").Include("Employee").OrderBy(u => u.Username).ToList();

            var nvChuaCoTk = db.Employees.Where(e => !e.Users.Any()).OrderBy(e => e.EmpCode).Take(500).ToList()
                .Select(e => new SelectListItem { Value = e.EmployeeId.ToString(), Text = e.EmpCode + " - " + e.LastName + " " + e.FirstName })
                .ToList();
            ViewBag.NhanVien = nvChuaCoTk;
            ViewBag.VaiTros = db.Roles.OrderBy(r => r.RoleName).ToList();
            ViewBag.MeId = PhanQuyenHelper.UserIdHienTai();
            return View(users);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult TaoTaiKhoan(string username, string matKhau, int? employeeId, int? roleId)
        {
            username = (username ?? "").Trim();
            if (!Regex.IsMatch(username, @"^[A-Za-z0-9._-]{3,50}$"))
            {
                TempData["Loi"] = "Tên đăng nhập gồm 3 đến 50 ký tự: chữ không dấu, số, dấu chấm, gạch dưới hoặc gạch ngang.";
                return RedirectToAction("NguoiDung");
            }
            if ((matKhau ?? "").Length < 6)
            {
                TempData["Loi"] = "Mật khẩu cần ít nhất 6 ký tự.";
                return RedirectToAction("NguoiDung");
            }
            if (db.Users.Any(u => u.Username == username))
            {
                TempData["Loi"] = "Tên đăng nhập đã tồn tại.";
                return RedirectToAction("NguoiDung");
            }

            var u2 = new User
            {
                Username = username,
                PasswordHash = MatKhauHelper.Bam(matKhau),
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            if (employeeId != null && db.Employees.Any(e => e.EmployeeId == employeeId.Value)) u2.EmployeeId = employeeId;
            if (roleId != null)
            {
                var r = db.Roles.Find(roleId.Value);
                if (r != null) u2.Roles.Add(r);
            }
            db.Users.Add(u2);
            db.SaveChanges();

            TempData["Ok"] = "Đã tạo tài khoản " + username + ".";
            return RedirectToAction("NguoiDung");
        }

        public ActionResult GanVaiTro(int id)
        {
            var u = db.Users.Include("Roles").FirstOrDefault(x => x.UserId == id);
            if (u == null) return HttpNotFound();

            return View(new GanVaiTroViewModel
            {
                NguoiDung = u,
                VaiTros = db.Roles.OrderBy(r => r.RoleName).ToList(),
                DangCo = new HashSet<int>(u.Roles.Select(r => r.RoleId))
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult GanVaiTro(int id, int[] vaiTro)
        {
            var u = db.Users.Include("Roles").FirstOrDefault(x => x.UserId == id);
            if (u == null) return HttpNotFound();

            var ids = (vaiTro ?? new int[0]).Distinct().ToList();
            var moi = db.Roles.Where(r => ids.Contains(r.RoleId)).ToList();

            bool dangQuanTri = u.Roles.Any(r => r.RoleCode == PhanQuyenHelper.MaQuanTri);
            bool seQuanTri = moi.Any(r => r.RoleCode == PhanQuyenHelper.MaQuanTri);
            if (dangQuanTri && !seQuanTri)
            {
                if (PhanQuyenHelper.UserIdHienTai() == u.UserId)
                {
                    TempData["Loi"] = "Bạn không thể tự gỡ vai trò quản trị của mình.";
                    return RedirectToAction("GanVaiTro", new { id = id });
                }
                if (u.IsActive == true && SoQuanTriDangHoatDong() <= 1)
                {
                    TempData["Loi"] = "Hệ thống cần ít nhất một quản trị viên đang hoạt động.";
                    return RedirectToAction("GanVaiTro", new { id = id });
                }
            }

            u.Roles.Clear();
            foreach (var r in moi) u.Roles.Add(r);
            db.SaveChanges();

            TempData["Ok"] = "Đã lưu vai trò của " + u.Username + ".";
            return RedirectToAction("NguoiDung");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult DoiTrangThai(int id)
        {
            var u = db.Users.Include("Roles").FirstOrDefault(x => x.UserId == id);
            if (u == null) return HttpNotFound();

            bool dangHoatDong = u.IsActive == true;
            if (dangHoatDong)
            {
                if (PhanQuyenHelper.UserIdHienTai() == u.UserId)
                {
                    TempData["Loi"] = "Bạn không thể tự khóa tài khoản của mình.";
                    return RedirectToAction("NguoiDung");
                }
                if (u.Roles.Any(r => r.RoleCode == PhanQuyenHelper.MaQuanTri) && SoQuanTriDangHoatDong() <= 1)
                {
                    TempData["Loi"] = "Hệ thống cần ít nhất một quản trị viên đang hoạt động.";
                    return RedirectToAction("NguoiDung");
                }
            }

            u.IsActive = !dangHoatDong;
            db.SaveChanges();
            TempData["Ok"] = (dangHoatDong ? "Đã khóa" : "Đã mở khóa") + " tài khoản " + u.Username + ".";
            return RedirectToAction("NguoiDung");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult DatLaiMatKhau(int id, string matKhauMoi)
        {
            var u = db.Users.Find(id);
            if (u == null) return HttpNotFound();
            if ((matKhauMoi ?? "").Length < 6)
            {
                TempData["Loi"] = "Mật khẩu mới cần ít nhất 6 ký tự.";
                return RedirectToAction("NguoiDung");
            }
            u.PasswordHash = MatKhauHelper.Bam(matKhauMoi);
            db.SaveChanges();
            TempData["Ok"] = "Đã đặt lại mật khẩu cho " + u.Username + ".";
            return RedirectToAction("NguoiDung");
        }

        private int SoQuanTriDangHoatDong()
        {
            return db.Users.Count(x => x.IsActive == true && x.Roles.Any(r => r.RoleCode == PhanQuyenHelper.MaQuanTri));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
