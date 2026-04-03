using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Common.DTOs
{
    public class VietQRRequest
    {
        public string BankId { get; set; } // Mã BIN của ngân hàng
        public string AccountNo { get; set; } // Số tài khoản người nhận
        public string AccountName { get; set; } // Tên chủ tài khoản 
        public decimal Amount { get; set; } // Số tiền 
        public string Content { get; set; } // Nội dung chuyển khoản
    }
}
