using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Common.Pagination
{
    public abstract class PaginationParams
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
