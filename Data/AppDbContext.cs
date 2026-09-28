using BackendApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<JobTitle> JobTitles => Set<JobTitle>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Cấu hình Bảng Role
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).ValueGeneratedOnAdd();
            entity.Property(r => r.Name).HasMaxLength(50).IsRequired();
            entity.Property(r => r.Description).HasMaxLength(250);

            entity.HasIndex(r => r.Name).IsUnique(); // Tên Role không trùng nhau
        });

        // 2. Department
        modelBuilder.Entity<Department>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Code).HasMaxLength(20).IsRequired();
            e.Property(d => d.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(d => d.Code).IsUnique();

            /* Department (1) - Employee (1) [Trưởng phòng ban]
            * - Mỗi phòng ban có 0 hoặc 1 Trưởng phòng (Manager) trỏ tới ID của nhân viên (ManagerId).
            * - OnDelete(DeleteBehavior.SetNull): Nếu nhân viên làm Trưởng phòng nghỉ việc/bị xóa, 
            *   cột 'ManagerId' trong bảng phòng ban sẽ tự động gán về NULL chứ KHÔNG xóa mất Phòng ban.
            */
            e.HasOne(d => d.Manager)
             .WithMany()
             .HasForeignKey(d => d.ManagerId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // 3. JobTitle
        modelBuilder.Entity<JobTitle>(e =>
        {
            e.HasKey(j => j.Id);
            e.Property(j => j.TitleName).HasMaxLength(100).IsRequired();
        });

        // 4. Employee
        modelBuilder.Entity<Employee>(e =>
        {
            e.HasKey(emp => emp.Id);
            e.Property(emp => emp.EmployeeCode).HasMaxLength(20).IsRequired();
            e.Property(emp => emp.Username).HasMaxLength(50).IsRequired();
            e.Property(emp => emp.Email).HasMaxLength(100).IsRequired();

            // RÀNG BUỘC UNIQUE: Mã nhân viên, Username và Email phải là duy nhất trên toàn hệ thống
            e.HasIndex(emp => emp.EmployeeCode).IsUnique();
            e.HasIndex(emp => emp.Username).IsUnique();
            e.HasIndex(emp => emp.Email).IsUnique();

            /*  Department (1) - Employee (N) [Nhân viên thuộc Phòng ban]
             * - Một phòng ban có nhiều Nhân viên. Một nhân viên thuộc một Phòng ban (DepartmentId).
             * - OnDelete(DeleteBehavior.Restrict): BẢO VỆ DỮ LIỆU - Chống xóa phòng ban nếu vẫn còn 
             *   nhân viên đang thuộc phòng ban đó.
             */
            e.HasOne(emp => emp.Department)
             .WithMany(d => d.Employees)
             .HasForeignKey(emp => emp.DepartmentId)
             .OnDelete(DeleteBehavior.Restrict);

            /* Employee (Manager) (1) - Employee (Subordinate) (N) [Quản lý trực tiếp]
             * - Mối quan hệ tự tham chiếu (Self-Referencing / Self-Join) giữa Nhân viên với Quản lý của mình.
             * - Một Manager quản lý danh sách nhiều nhân viên cấp dưới (DirectReports).
             * - OnDelete(DeleteBehavior.Restrict): Chặn xóa tài khoản Manager nếu vẫn còn nhân viên cấp dưới 
             *   đang gắn với ManagerId này.
             */
            e.HasOne(emp => emp.Manager)
             .WithMany(m => m.DirectReports)
             .HasForeignKey(emp => emp.ManagerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LeaveType>(lt =>
        {
            lt.Property(x => x.DaysAllowed)
            .HasPrecision(4, 1);
        });


        // 5. LeaveBalance
        modelBuilder.Entity<LeaveBalance>(e =>
        {
            e.HasKey(lb => lb.Id);
            e.Property(lb => lb.TotalDays).HasPrecision(4, 1);
            e.Property(lb => lb.UsedDays).HasPrecision(4, 1);
            e.Property(lb => lb.RemainingDays).HasPrecision(4, 1);
            //Mỗi nhân viên chỉ có DUY NHẤT 1 bản ghi quỹ phép cho từng Loại phép trong cùng 1 Năm
            e.HasIndex(lb => new { lb.EmployeeId, lb.LeaveTypeId, lb.Year }).IsUnique();
        });

        // 6. LeaveRequest
        modelBuilder.Entity<LeaveRequest>(e =>
        {
            e.HasKey(lr => lr.Id);
            e.Property(lr => lr.TotalRequestedDays).HasPrecision(4, 1);

            /*Employee (Approver) (1) - LeaveRequest (N) [Người phê duyệt đơn]
            * - Một đơn nghỉ phép có cột 'ApprovedBy' trỏ tới Id của Nhân viên duyệt đơn (Manager/HR).
            * - OnDelete(DeleteBehavior.Restrict): Chặn xóa tài khoản Người duyệt nếu đang được lưu 
            *   trong lịch sử duyệt đơn của hệ thống.
            */
            e.HasOne(lr => lr.Approver)
             .WithMany()
             .HasForeignKey(lr => lr.ApprovedBy)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}