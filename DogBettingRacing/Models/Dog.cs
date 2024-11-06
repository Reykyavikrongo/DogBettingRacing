using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DogBettingRacing.Models
{
    public class Dog
    {
        [Key]
        public int DogId { get; set; }
        public string Name { get; set; }
        [Required]
        public int RoundId { get; set; }

        [ForeignKey("RoundId")]
        public Round Round { get; set; }

        [NotMapped]
        public decimal Odds { get; set; }
    }
}
