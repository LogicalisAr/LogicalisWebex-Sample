using System;
using System.ComponentModel.DataAnnotations;

namespace SallyBot
{
    public class User
    {
        public long ID { get; set; }

        [Required]
        [StringLength(250)]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        [StringLength(250)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [StringLength(250)]
        [Display(Name = "WebexID")]
        public string WebexID { get; set; }

        [StringLength(250)]
        [Display(Name = "ConversationID")]
        public string ConversationID { get; set; }

        [StringLength(250)]
        [Display(Name = "TokenWebex")]
        public string TokenWebex { get; set; }

        public DateTime? TokenWebexExpires { get; set; }

        [StringLength(250)]
        [Display(Name = "TokenRefreshWebex")]
        public string TokenRefreshWebex { get; set; }

        public DateTime? TokenRefreshWebexExpires { get; set; }
    }
}
