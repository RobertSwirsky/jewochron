using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jewochron.Models
{
    /// <summary>
    /// Represents a Yahrzeit (Jewish death anniversary) entry
    /// </summary>
    public class Yahrzeit
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Hebrew month (1-13, supporting leap years)
        /// </summary>
        [Required]
        [Range(1, 13)]
        public int HebrewMonth { get; set; }

        /// <summary>
        /// Hebrew day (1-30)
        /// </summary>
        [Required]
        [Range(1, 30)]
        public int HebrewDay { get; set; }

        /// <summary>
        /// Hebrew year
        /// </summary>
        [Required]
        public int HebrewYear { get; set; }

        /// <summary>
        /// Name of the deceased in English
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string NameEnglish { get; set; } = string.Empty;

        /// <summary>
        /// Name of the deceased in Hebrew
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string NameHebrew { get; set; } = string.Empty;

        /// <summary>
        /// When this record was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this record was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
