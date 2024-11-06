using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DogBettingRacing.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }
        [Required]
        public string Name { get; set; }
        [Column(TypeName = "decimal(15,2)")]
        public decimal WalletBalance { get; set; }

    }
}
