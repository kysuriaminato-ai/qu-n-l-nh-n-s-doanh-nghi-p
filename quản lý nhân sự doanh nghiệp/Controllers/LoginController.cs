using System.Linq;
using System.Web.Mvc;
using quản_lý_nhân_sự_doanh_nghiệp.Models;

namespace quản_lý_nhân_sự_doanh_nghiệp.Controllers
{
    public class LoginController : Controller
    {
        private Quản_lý_nhân_sựEntities3 db = new Quản_lý_nhân_sựEntities3();

        // GET: Login
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(string tenDangNhap, string matKhau)
        {
            var tk = db.Users.FirstOrDefault(u => u.Username == tenDangNhap && u.IsActive == true);

            if (tk == null || tk.PasswordHash != matKhau)
            {
                ViewBag.Loi = "Tên đăng nhập hoặc mật khẩu chưa đúng.";
                return View();
            }

            Session.Clear();
            Session["UserId"] = tk.UserId;
            Session["TenDangNhap"] = tk.Username;

            return RedirectToAction("Index", "Home");
        }

        public ActionResult DangXuat()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}