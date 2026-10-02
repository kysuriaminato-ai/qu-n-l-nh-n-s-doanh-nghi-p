using System;
using System.Linq;
using System.Web.Mvc;
using quản_lý_nhân_sự_doanh_nghiệp.Models;

namespace quản_lý_nhân_sự_doanh_nghiệp.Controllers
{
    [KiemTraQuyen("Contract.View")]
    public class HopDongController : Controller
    {
        // loc: sapHet (còn ≤ 30 ngày) | daHet | khongThoiHan | tatCa
        public ActionResult Index(string loc)
        {
            if (string.IsNullOrEmpty(loc)) loc = "sapHet";
            var today = DateTime.Today;
            var han = today.AddDays(30);

            using (var db = new Quản_lý_nhân_sựEntities3())
            {
                IQueryable<Contract> q = db.Contracts.GioiHanPhamVi(db);
                bool theoHan = false;

                switch (loc)
                {
                    case "sapHet":
                        q = q.Where(c => c.EndDate != null && c.EndDate >= today && c.EndDate <= han);
                        theoHan = true; break;
                    case "daHet":
                        q = q.Where(c => c.EndDate != null && c.EndDate < today); break;
                    case "khongThoiHan":
                        q = q.Where(c => c.EndDate == null); break;
                }

                var ds = (theoHan ? q.OrderBy(c => c.EndDate) : q.OrderByDescending(c => c.StartDate))
                    .Take(300)
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

                ViewBag.Loc = loc;
                return View(ds);
            }
        }
    }
}
