
using BackendApp.Data;
using BackendApp.DTOs;
using BackendApp.DTOs.Common;
using BackendApp.Extensions;
using BackendApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendApp.Services;

public class LeaveTypeService
{
    private readonly AppDbContext _context;

    public LeaveTypeService(AppDbContext context)
    {
        _context = context;
    }

    // 1. Lấy danh sách tất cả các loại phép
    public async Task<PagedResult<LeaveTypeResponseDto>> GetAllLeaveTypesAsync(LeaveTypeFilterRequestDTO request)
    {
        var query = _context.LeaveTypes.AsNoTracking();
        var dtoQuery = query
            .OrderBy(lt => lt.Id)
            .Select(lt => new LeaveTypeResponseDto(
                lt.Id,
                lt.Name,
                lt.DaysAllowed,
                lt.IsPaid,
                lt.IsActive
            ));

        // Thực thi phân trang
        return await dtoQuery.ToPagedListAsync(request.PageIndex, request.PageSize);
    }

    // 2. Lấy loại phép theo Id
    public async Task<LeaveTypeResponseDto?> GetLeaveTypeByIdAsync(int id)
    {
        var leaveType = await _context.LeaveTypes.FindAsync(id);
        if (leaveType == null) return null;

        return MapToLeaveTypeDTO(leaveType);

    }

    // 3. Tạo mới loại phép
    public async Task<LeaveTypeResponseDto?> CreateLeaveTypeAsync(CreateLeaveTypeDto dto)
    {
        // Kiểm tra trùng tên loại phép
        if (await _context.LeaveTypes.AnyAsync(lt => lt.Name.ToLower() == dto.Name.ToLower()))
            return null;

        if (dto.DaysAllowed < 0)
        {
            throw new ArgumentException("Số ngày phép không được âm.");
        }

        var leaveType = new LeaveType
        {
            Name = dto.Name,
            DaysAllowed = dto.DaysAllowed,
            IsPaid = dto.IsPaid,
            IsActive = true
        };

        _context.LeaveTypes.Add(leaveType);
        await _context.SaveChangesAsync();

        return MapToLeaveTypeDTO(leaveType);
    }


    // 4. Cập nhật loại phép
    public async Task<LeaveTypeResponseDto?> UpdateLeaveTypeAsync(int id, UpdateLeaveTypeDto dto)
    {
        var leaveType = await _context.LeaveTypes.FindAsync(id);
        if (leaveType == null) return null;

        if (dto.DaysAllowed < 0)
        {
            throw new ArgumentException("Số ngày phép không được âm.");
        }

        // Check trùng tên với loại phép khác
        if (await _context.LeaveTypes.AnyAsync(lt => lt.Name.ToLower() == dto.Name.ToLower() && lt.Id != id))
            throw new ArgumentException("Tên loại phép này đã tồn tại.");

        leaveType.Name = dto.Name;
        leaveType.DaysAllowed = dto.DaysAllowed;
        leaveType.IsPaid = dto.IsPaid;
        leaveType.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        return MapToLeaveTypeDTO(leaveType);
    }


    // 5. Xóa loại phép (Chặn xóa nếu đã có dữ liệu Quỹ phép hoặc Đơn nghỉ liên kết)
    public async Task<(bool Success, string Message)> DeleteLeaveTypeAsync(int id)
    {
        var leaveType = await _context.LeaveTypes.FindAsync(id);
        if (leaveType == null)
            return (false, "Không tìm thấy loại phép.");

        // Kiểm tra ràng buộc dữ liệu
        bool hasBalances = await _context.LeaveBalances.AnyAsync(lb => lb.LeaveTypeId == id);
        bool hasRequests = await _context.LeaveRequests.AnyAsync(lr => lr.LeaveTypeId == id);

        if (hasBalances || hasRequests)
        {
            return (false, $"Không thể xóa loại phép '{leaveType.Name}' vì đã có lịch sử cấp quỹ phép hoặc đơn nghỉ phép sử dụng loại này.");
        }

        leaveType.IsActive = false;
        await _context.SaveChangesAsync();

        return (true, "Xóa loại phép thành công.");
    }

    public static LeaveTypeResponseDto MapToLeaveTypeDTO(LeaveType lt)
    {
        return new LeaveTypeResponseDto(
            lt.Id,
            lt.Name,
            lt.DaysAllowed,
            lt.IsPaid,
            lt.IsActive
        );
    }


}