using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using quản_lý_nhân_sự_doanh_nghiệp.Models;

namespace quản_lý_nhân_sự_doanh_nghiệp.Controllers
{
    [KiemTraQuyen]   // cả controller yêu cầu đăng nhập
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var today = DateTime.Today;
            var dauThang = new DateTime(today.Year, today.Month, 1);
            var han = today.AddDays(30);
            var br = PhanQuyenHelper.ThongTin().ChiNhanh;   // null = mọi chi nhánh

            var vm = new DashboardVM();
            using (var db = new Quản_lý_nhân_sựEntities3())
            {
                var nv = db.Employees.GioiHanPhamVi();
                vm.TongNhanVien = nv.Count();
                vm.MoiTrongThang = nv.Count(e => e.JoinDate >= dauThang);

                vm.SoChiNhanh = br == null ? db.Branches.Count() : br.Count;
                vm.SoPhongBan = br == null ? db.Departments.Count() : db.Departments.Count(d => br.Contains(d.BranchId));

                var hd = db.Contracts.GioiHanPhamVi(db)
                    .Where(c => c.EndDate != null && c.EndDate >= today && c.EndDate <= han);
                vm.HopDongSapHet = hd.Count();
                vm.DanhSachSapHet = hd.OrderBy(c => c.EndDate).Take(8)
                    .Select(c => new HopDongRow
                    {
                        ContractId = c.ContractId,
                        EmployeeId = c.EmployeeId,
                        Loai = c.ContractType,
                        BatDau = c.StartDate,
                        KetThuc = c.EndDate,
                        HoTen = db.Employees.Where(e => e.EmployeeId == c.EmployeeId)
                                  .Select(e => e.LastName + " " + e.FirstName).FirstOrDefault()
                    }).ToList();

                var pl = db.EmpPlacements.Where(p => p.IsCurrent == true);
                if (br != null) pl = pl.Where(p => br.Contains(p.Department.BranchId));
                vm.TheoPhongBan = pl.GroupBy(p => p.Department.DeptName)
                    .Select(g => new { Ten = g.Key, So = g.Count() })
                    .OrderByDescending(x => x.So).Take(8).ToList()
                    .Select(x => new KeyValuePair<string, int>(x.Ten, x.So)).ToList();
            }
            return View(vm);
        }

        public ActionResult KhongCoQuyen()
        {
            return View();
        }
    }
}
