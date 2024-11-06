using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace DogBettingRacing.Models
{
    public class Round
    {
        [Key]
        public int RoundId { get; set; }
        [Required]
        public DateTime StartTime { get; set; }
        //Scheduled, InProgress, Ended
        public string Status { get; set; }

        public ICollection<Dog> Dogs { get; set; } = new List<Dog>();

        [NotMapped]
        public int WinningDogId { get; set; }

    }
}
