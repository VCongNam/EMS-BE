using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace EMS.Application.Common.Pagination
{
    public class PagedResultDto<T>
    {
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public IEnumerable<T> Items { get; set; } = new List<T>();

        public PagedResultDto(IEnumerable<T> items, int totalItems, int pageIndex, int pageSize)
        {
            TotalItems = totalItems;
            CurrentPage = pageIndex;
            PageSize = pageSize;
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            Items = items;
        }
    }
}
