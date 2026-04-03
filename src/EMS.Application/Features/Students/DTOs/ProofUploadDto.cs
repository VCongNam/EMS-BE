using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Application.Features.Students.DTOs
{
    public class ProofUploadDto
    {
        public IFormFile ProofImage { get; set; }
    }
}
