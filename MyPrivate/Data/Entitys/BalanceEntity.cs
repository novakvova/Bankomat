using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPrivate.Data.Entitys
{
    [Table("tbl_Balances")]
    public class BalanceEntity
    {
        [Key]
        public int Id { get; set; }
        [Required, ForeignKey("User")]
        public int UserId { get; set; }
        [Required]
        public decimal Amount { get; set; }
        public virtual UserEntity User { get; set; }
    }
}
