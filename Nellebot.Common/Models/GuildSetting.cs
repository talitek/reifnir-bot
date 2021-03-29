using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Common.Models
{
    public class GuildSetting
    {
        [Key]
        public Guid Id { get; set; }
        public ulong GuildId { get; set; }
        [MaxLength(256)]
        public string Key { get; set; } = null!;
        [MaxLength(1024)]
        public string Value { get; set; } = null!;
    }
}
