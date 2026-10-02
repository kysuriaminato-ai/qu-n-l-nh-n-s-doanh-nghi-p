using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web.Mvc;
using quản_lý_nhân_sự_doanh_nghiệp.Models;

namespace quản_lý_nhân_sự_doanh_nghiệp.Controllers
{
    [KiemTraQuyen("Employee.View")]
    public class NhanSuController : Controller
    {
        private const int KichThuocTrang = 20;
        private Quản_lý_nhân_sựEntities3 db = new Quản_lý_nhân_sựEntities3();

        // ---------------- Danh sách ----------------
        public ActionResult Index(string q, int? phongBan, string trangThai, int trang = 1)
        {
            var qy = db.Employees.GioiHanPhamVi();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                qy = qy.Where(e => e.EmpCode.Contains(q) || e.FirstName.Contains(q) || e.LastName.Contains(q)
                                || (e.LastName + " " + e.FirstName).Contains(q));
            }
            if (phongBan != null)
            {
                int pb = phongBan.Value;
                qy = qy.Where(e => e.EmpPlacements.Any(p => p.IsCurrent == true && p.DepartmentId == pb));
            }
            if (!string.IsNullOrEmpty(trangThai))
                qy = qy.Where(e => e.Status == trangThai);

            int tong = qy.Count();
            int tongTrang = Math.Max(1, (int)Math.Ceiling(tong / (double)KichThuocTrang));
            trang = Math.Min(Math.Max(1, trang), tongTrang);

            var rows = qy.OrderBy(e => e.EmpCode)
                .Skip((trang - 1) * KichThuocTrang).Take(KichThuocTrang)
                .Select(e => new NhanSuRow
                {
                    Id = e.EmployeeId,
                    Ma = e.EmpCode,
                    Ho = e.LastName,
                    Ten = e.FirstName,
                    GioiTinh = e.Gender,
                    NgayVao = e.JoinDate,
                    TrangThai = e.Status,
                    PhongBan = e.EmpPlacements.Where(p => p.IsCurrent == true).Select(p => p.Department.DeptName).FirstOrDefault(),
                    ChucVu = e.EmpPlacements.Where(p => p.IsCurrent == true).Select(p => p.Position.PositionName).FirstOrDefault(),
                    Sdt = e.EmployeeDetail.Phone
                }).ToList();

            var f = new NhanSuForm();
            NapDanhMuc(f);

            return View(new NhanSuListVM
            {
                Rows = rows, Q = q, PhongBan = phongBan, TrangThai = trangThai,
                Trang = trang, TongTrang = tongTrang, Tong = tong,
                PhongBans = f.PhongBans, TrangThais = f.TrangThais
            });
        }

        // ---------------- Chi tiết ----------------
        public ActionResult Details(int id)
        {
            if (!db.Employees.GioiHanPhamVi().Any(e => e.EmployeeId == id)) return HttpNotFound();

            var nv = db.Employees.Include("EmployeeDetail").FirstOrDefault(e => e.EmployeeId == id);
            if (nv == null) return HttpNotFound();

            var lichSu = db.EmpPlacements.Where(p => p.EmployeeId == id)
                .OrderByDescending(p => p.EffectiveDate)
                .Select(p => new NhaSuPlacementRow
                {
                    NgayHieuLuc = p.EffectiveDate,
                    PhongBan = p.Department.DeptName,
                    ChucVu = p.Position.PositionName,
                    HienTai = p.IsCurrent == true
                }).ToList();

            var hopDong = db.Contracts.Where(c => c.EmployeeId == id)
                .OrderByDescending(c => c.StartDate)
                .Select(c => new HopDongRow
                {
                    ContractId = c.ContractId, EmployeeId = c.EmployeeId,
                    Loai = c.ContractType, BatDau = c.StartDate, KetThuc = c.EndDate
                }).ToList();

            return View(new NhanSuChiTietVM { NhanVien = nv, ChiTiet = nv.EmployeeDetail, LichSu = lichSu, HopDongs = hopDong });
        }

        // ---------------- Thêm ----------------
        [KiemTraQuyen("Employee.Create")]
        public ActionResult Create()
        {
            var f = new NhanSuForm { JoinDate = DateTime.Today };
            NapDanhMuc(f);
            return View(f);
        }

        [HttpPost, ValidateAntiForgeryToken, KiemTraQuyen("Employee.Create")]
        public ActionResult Create(NhanSuForm f)
        {
            string ma = f.EmpCode == null ? null : f.EmpCode.Trim();
            if (ma != null && db.Employees.Any(e => e.EmpCode == ma))
                ModelState.AddModelError("EmpCode", "Mã nhân viên này đã tồn tại.");
            KiemTraPhongBan(f);

            if (!ModelState.IsValid) { NapDanhMuc(f); return View(f); }

            var nv = new Employee
            {
                EmpCode = f.EmpCode.Trim(),
                LastName = f.LastName.Trim(),
                FirstName = f.FirstName.Trim(),
                Gender = f.Gender,
                BirthDate = f.BirthDate,
                JoinDate = f.JoinDate.Value,
                Status = f.Status
            };
            nv.EmployeeDetail = new EmployeeDetail
            {
                IdentityCard = f.IdentityCard.Trim(),
                Address = f.Address,
                Phone = f.Phone,
                Email = f.Email
            };
            nv.EmpPlacements.Add(new EmpPlacement
            {
                DepartmentId = f.DepartmentId.Value,
                PositionId = f.PositionId.Value,
                EffectiveDate = f.JoinDate.Value,
                IsCurrent = true
            });

            try
            {
                db.Employees.Add(nv);
                db.SaveChanges();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Không lưu được. Có thể số CCCD/CMND hoặc mã nhân viên đã tồn tại.");
                NapDanhMuc(f);
                return View(f);
            }

            TempData["Ok"] = "Đã thêm nhân viên " + nv.LastName + " " + nv.FirstName + ".";
            return RedirectToAction("Details", new { id = nv.EmployeeId });
        }

        // ---------------- Sửa ----------------
        [KiemTraQuyen("Employee.Edit")]
        public ActionResult Edit(int id)
        {
            if (!db.Employees.GioiHanPhamVi().Any(e => e.EmployeeId == id)) return HttpNotFound();
            var nv = db.Employees.Include("EmployeeDetail").FirstOrDefault(e => e.EmployeeId == id);
            if (nv == null) return HttpNotFound();

            var pl = db.EmpPlacements.Where(p => p.EmployeeId == id && p.IsCurrent == true)
                       .OrderByDescending(p => p.EffectiveDate).FirstOrDefault();
            var d = nv.EmployeeDetail;

            var f = new NhanSuForm
            {
                EmployeeId = id,
                EmpCode = nv.EmpCode, LastName = nv.LastName, FirstName = nv.FirstName,
                Gender = nv.Gender, BirthDate = nv.BirthDate, JoinDate = nv.JoinDate, Status = nv.Status,
                IdentityCard = d != null ? d.IdentityCard : null,
                Address = d != null ? d.Address : null,
                Phone = d != null ? d.Phone : null,
                Email = d != null ? d.Email : null,
                DepartmentId = pl != null ? (int?)pl.DepartmentId : null,
                PositionId = pl != null ? (int?)pl.PositionId : null
            };
            NapDanhMuc(f);
            return View(f);
        }

        [HttpPost, ValidateAntiForgeryToken, KiemTraQuyen("Employee.Edit")]
        public ActionResult Edit(int id, NhanSuForm f)
        {
            f.EmployeeId = id;
            if (!db.Employees.GioiHanPhamVi().Any(e => e.EmployeeId == id)) return HttpNotFound();
            var nv = db.Employees.Include("EmployeeDetail").FirstOrDefault(e => e.EmployeeId == id);
            if (nv == null) return HttpNotFound();

            string ma = f.EmpCode == null ? null : f.EmpCode.Trim();
            if (ma != null && db.Employees.Any(e => e.EmpCode == ma && e.EmployeeId != id))
                ModelState.AddModelError("EmpCode", "Mã nhân viên này đã tồn tại.");
            KiemTraPhongBan(f);

            if (!ModelState.IsValid) { NapDanhMuc(f); return View(f); }

            nv.EmpCode = f.EmpCode.Trim();
            nv.LastName = f.LastName.Trim();
            nv.FirstName = f.FirstName.Trim();
            nv.Gender = f.Gender;
            nv.BirthDate = f.BirthDate;
            nv.JoinDate = f.JoinDate.Value;
            nv.Status = f.Status;

            if (nv.EmployeeDetail == null) nv.EmployeeDetail = new EmployeeDetail();
            nv.EmployeeDetail.IdentityCard = f.IdentityCard.Trim();
            nv.EmployeeDetail.Address = f.Address;
            nv.EmployeeDetail.Phone = f.Phone;
            nv.EmployeeDetail.Email = f.Email;

            // Đổi phòng ban hoặc chức vụ: đóng bản ghi hiện tại, mở bản ghi mới (giữ lịch sử)
            var hienTai = db.EmpPlacements.Where(p => p.EmployeeId == id && p.IsCurrent == true).ToList();
            bool doi = !hienTai.Any(p => p.DepartmentId == f.DepartmentId.Value && p.PositionId == f.PositionId.Value);
            if (doi)
            {
                foreach (var p in hienTai) p.IsCurrent = false;
                db.EmpPlacements.Add(new EmpPlacement
                {
                    EmployeeId = id,
                    DepartmentId = f.DepartmentId.Value,
                    PositionId = f.PositionId.Value,
                    EffectiveDate = DateTime.Today,
                    IsCurrent = true
                });
            }

            try { db.SaveChanges(); }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Không lưu được. Có thể số CCCD/CMND hoặc mã nhân viên đã tồn tại.");
                NapDanhMuc(f);
                return View(f);
            }

            TempData["Ok"] = "Đã lưu hồ sơ " + nv.LastName + " " + nv.FirstName + ".";
            return RedirectToAction("Details", new { id = id });
        }

        // ---------------- Tiện ích ----------------
        // Phòng ban chọn phải tồn tại và nằm trong phạm vi chi nhánh của người dùng
        private void KiemTraPhongBan(NhanSuForm f)
        {
            if (f.DepartmentId == null) return;
            var br = PhanQuyenHelper.ThongTin().ChiNhanh;
            int dep = f.DepartmentId.Value;
            bool ok = br == null
                ? db.Departments.Any(d => d.DepartmentId == dep)
                : db.Departments.Any(d => d.DepartmentId == dep && br.Contains(d.BranchId));
            if (!ok) ModelState.AddModelError("DepartmentId", "Phòng ban không hợp lệ hoặc ngoài phạm vi của bạn.");

            if (f.PositionId != null)
            {
                int pos = f.PositionId.Value;
                if (!db.Positions.Any(p => p.PositionId == pos))
                    ModelState.AddModelError("PositionId", "Chức vụ không hợp lệ.");
            }
        }

        private void NapDanhMuc(NhanSuForm f)
        {
            var br = PhanQuyenHelper.ThongTin().ChiNhanh;
            var dq = db.Departments.AsQueryable();
            if (br != null) dq = dq.Where(d => br.Contains(d.BranchId));

            f.PhongBans = dq.OrderBy(d => d.Branch.BranchName).ThenBy(d => d.DeptName)
                .Select(d => new { d.DepartmentId, d.DeptName, d.Branch.BranchName }).ToList()
                .Select(d => new SelectListItem
                {
                    Value = d.DepartmentId.ToString(),
                    Text = d.DeptName + " (" + d.BranchName + ")"
                }).ToList();

            f.ChucVus = db.Positions.OrderBy(p => p.Level).ThenBy(p => p.PositionName).ToList()
                .Select(p => new SelectListItem { Value = p.PositionId.ToString(), Text = p.PositionName }).ToList();

            var gt = db.Employees.Select(e => e.Gender).Where(g => g != null).Distinct().ToList();
            f.GioiTinhs = new[] { "Nam", "Nữ" }.Union(gt).ToList();
            f.TrangThais = db.Employees.Select(e => e.Status).Where(s => s != null).Distinct().ToList();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
