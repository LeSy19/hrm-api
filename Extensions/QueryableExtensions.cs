
using Microsoft.EntityFrameworkCore;
using BackendApp.DTOs.Common;

namespace BackendApp.Extensions
{
    public static class QueryableExtensions
    {
        public static async Task<PagedResult<T>> ToPagedListAsync<T>(
            this IQueryable<T> source, 
            int pageIndex, 
            int pageSize)
        {
            // 1. Đếm tổng số bản ghi
            var count = await source.CountAsync();

            // 2. Cắt dữ liệu theo trang
            var items = await source
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 3. Trả về kết quả đóng gói
            return new PagedResult<T>(items, count, pageIndex, pageSize);
        }
    }
}