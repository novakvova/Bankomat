using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPrivate.Data.Entitys
{
    [Table("tbl_Users")]
    public class UserEntity
    {
        [Key]
        public int Id { get; set; }
        [Required, StringLength(50)]
        public string FirstName { get; set; }
        [Required, StringLength(50)]
        public string LastName { get; set; }
        [Required, StringLength(50)]
        public string FatherName { get; set; }
        [Required]
        public long CardNumber { get; set; }
        [Required]
        public long PinCode { get; set; }
    }
}
