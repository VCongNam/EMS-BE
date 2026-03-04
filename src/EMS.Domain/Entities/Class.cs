using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EMS.Domain.Entities
{
    public class Class
    {
        public Guid ClassId { get; set; } 
        public Guid TeacherId { get; set; } 

        public string ClassName { get; set; } = string.Empty; 
        public string? Room { get; set; }     

        public DateTime StartDate { get; set; } 
        public DateTime EndDate { get; set; }   

        public decimal TuitionFee { get; set; } 

        public string Status { get; set; } = "Scheduled"; 
        public bool IsDeleted { get; set; }     
        public DateTime CreatedAt { get; set; }   
        public DateTime? UpdatedAt { get; set; }  

        // public virtual Teacher Teacher { get; set; }
        // public virtual ICollection<Student> Students { get; set; }
    }

}
