using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Students.DTOs
{
    public class PaymentQrDto
    {
        public string QrCodeBase64 { get; set; } 
        public string BankName { get; set; } // Tên Ngân hàng 
        public string AccountNo { get; set; } // Số tài khoản
        public string AccountName { get; set; } // Tên chủ tài khoản
        public decimal Amount { get; set; } // Số tiền
        public string TransferContent { get; set; } // Nội dung chuyển khoản
    }
}
