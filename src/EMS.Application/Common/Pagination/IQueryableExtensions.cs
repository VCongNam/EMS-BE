using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace EMS.Application.Common.Pagination
{
    public static class IQueryableExtensions
    {
        public static async Task<PagedResultDto<T>> ToPagedListAsync<T>(
            this IQueryable<T> query,
            int pageIndex,
            int pageSize)
        {
            // 1. Đếm tổng số bản ghi thỏa mãn điều kiện
            var totalItems = await query.CountAsync();

            // 2. Cắt lấy đúng số dòng của trang hiện tại
            var items = await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

            // 3. Trả về object phân trang chuẩn
            return new PagedResultDto<T>(items, totalItems, pageIndex, pageSize);
        }
    }
}
