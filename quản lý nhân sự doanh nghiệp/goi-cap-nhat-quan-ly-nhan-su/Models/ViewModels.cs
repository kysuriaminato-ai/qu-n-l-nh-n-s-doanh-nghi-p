using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace quản_lý_nhân_sự_doanh_nghiệp.Models
{
    // ---------- Tổng quan ----------
    public class DashboardVM
    {
        public int TongNhanVien { get; set; }
        public int MoiTrongThang { get; set; }
        public int SoChiNhanh { get; set; }
        public int SoPhongBan { get; set; }
        public int HopDongSapHet { get; set; }
        public List<KeyValuePair<string, int>> TheoPhongBan { get; set; }
        public List<HopDongRow> DanhSachSapHet { get; set; }
    }

    // ---------- Hợp đồng ----------
    public class HopDongRow
    {
        public int ContractId { get; set; }
        public int EmployeeId { get; set; }
        public string HoTen { get; set; }
        public string Loai { get; set; }
        public DateTime BatDau { get; set; }
        public DateTime? KetThuc { get; set; }

        public int? SoNgayCon
        {
            get { return KetThuc.HasValue ? (int?)(KetThuc.Value.Date - DateTime.Today).Days : null; }
        }
    }

    // ---------- Nhân sự ----------
    public class NhanSuRow
    {
        public int Id { get; set; }
        public string Ma { get; set; }
        public string Ho { get; set; }
        public string Ten { get; set; }
        public string GioiTinh { get; set; }
        public DateTime NgayVao { get; set; }
        public string TrangThai { get; set; }
        public string PhongBan { get; set; }
        public string ChucVu { get; set; }
        public string Sdt { get; set; }
    }

    public class NhanSuListVM
    {
        public List<NhanSuRow> Rows { get; set; }
        public string Q { get; set; }
        public int? PhongBan { get; set; }
        public string TrangThai { get; set; }
        public int Trang { get; set; }
        public int TongTrang { get; set; }
        public int Tong { get; set; }
        public List<SelectListItem> PhongBans { get; set; }
        public List<string> TrangThais { get; set; }
    }

    public class NhanSuForm
    {
        public int? EmployeeId { get; set; }

        [Required(ErrorMessage = "Nhập mã nhân viên."), StringLength(50, ErrorMessage = "Tối đa 50 ký tự.")]
        public string EmpCode { get; set; }

        [Required(ErrorMessage = "Nhập họ."), StringLength(50, ErrorMessage = "Tối đa 50 ký tự.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Nhập tên."), StringLength(50, ErrorMessage = "Tối đa 50 ký tự.")]
        public string FirstName { get; set; }

        [StringLength(10, ErrorMessage = "Tối đa 10 ký tự.")]
        public string Gender { get; set; }

        public DateTime? BirthDate { get; set; }

        [Required(ErrorMessage = "Nhập ngày vào làm.")]
        public DateTime? JoinDate { get; set; }

        [StringLength(20, ErrorMessage = "Tối đa 20 ký tự.")]
        public string Status { get; set; }

        [Required(ErrorMessage = "Nhập số CCCD/CMND."), StringLength(20, ErrorMessage = "Tối đa 20 ký tự.")]
        public string IdentityCard { get; set; }

        [StringLength(500, ErrorMessage = "Tối đa 500 ký tự.")]
        public string Address { get; set; }

        [StringLength(20, ErrorMessage = "Tối đa 20 ký tự.")]
        public string Phone { get; set; }

        [StringLength(100, ErrorMessage = "Tối đa 100 ký tự."), EmailAddress(ErrorMessage = "Email chưa đúng định dạng.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Chọn phòng ban.")]
        public int? DepartmentId { get; set; }

        [Required(ErrorMessage = "Chọn chức vụ.")]
        public int? PositionId { get; set; }

        // Nguồn cho ô chọn (không nhận từ form)
        public List<SelectListItem> PhongBans { get; set; }
        public List<SelectListItem> ChucVus { get; set; }
        public List<string> GioiTinhs { get; set; }
        public List<string> TrangThais { get; set; }
    }

    public class NhaSuPlacementRow
    {
        public DateTime NgayHieuLuc { get; set; }
        public string PhongBan { get; set; }
        public string ChucVu { get; set; }
        public bool HienTai { get; set; }
    }

    public class NhanSuChiTietVM
    {
        public Employee NhanVien { get; set; }
        public EmployeeDetail ChiTiet { get; set; }
        public List<NhaSuPlacementRow> LichSu { get; set; }
        public List<HopDongRow> HopDongs { get; set; }
    }

    // ---------- Phân quyền ----------
    public class PhanQuyenViewModel
    {
        public List<Role> VaiTros { get; set; }
        public Role VaiTroChon { get; set; }
        public List<Permission> Quyens { get; set; }
        public HashSet<int> DaCap { get; set; }
        public List<Branch> ChiNhanhs { get; set; }
        public HashSet<int> PhamVi { get; set; }
        public bool Khoa { get; set; }
        public int SoNguoi { get; set; }
    }

    public class GanVaiTroViewModel
    {
        public User NguoiDung { get; set; }
        public List<Role> VaiTros { get; set; }
        public HashSet<int> DangCo { get; set; }
    }
}
