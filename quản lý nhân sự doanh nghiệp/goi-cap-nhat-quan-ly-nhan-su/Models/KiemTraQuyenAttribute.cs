using System;
using System.Web.Mvc;
using System.Web.Routing;

namespace quản_lý_nhân_sự_doanh_nghiệp.Models
{
    // [KiemTraQuyen]                   chỉ cần đã đăng nhập
    // [KiemTraQuyen("Employee.View")]  cần có mã quyền đó (quản trị luôn được qua)
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class KiemTraQuyenAttribute : AuthorizeAttribute
    {
        private readonly string _maQuyen;
        public KiemTraQuyenAttribute(string maQuyen = null) { _maQuyen = maQuyen; }

        protected override bool AuthorizeCore(System.Web.HttpContextBase httpContext)
        {
            if (!PhanQuyenHelper.DaDangNhap()) return false;
            return string.IsNullOrEmpty(_maQuyen) || PhanQuyenHelper.CoQuyen(_maQuyen);
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            var route = PhanQuyenHelper.DaDangNhap()
                ? new RouteValueDictionary(new { controller = "Home", action = "KhongCoQuyen" })
                : new RouteValueDictionary(new { controller = "Login", action = "Index" });
            filterContext.Result = new RedirectToRouteResult(route);
        }
    }
}
