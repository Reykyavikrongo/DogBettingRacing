using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DogBettingRacing.Models
{
    public class Bet
    {
        [Key]
        public int BetId { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public int DogId { get; set; }
        [Required]
        public decimal Amount { get; set; }
        //Won, Lost, Pending
        public string Status { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }
        [ForeignKey("DogId")]
        public Dog Dog { get; set; }

        [NotMapped]
        public decimal Payout { get; set; }
    }
}
