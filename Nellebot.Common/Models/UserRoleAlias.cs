using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Common.Models
{
    public class UserRoleAlias
    {
        public Guid Id { get; set; }
        public Guid UserRoleId { get; set; }
        public UserRole UserRole { get; set; } = null!;
        public string Alias { get; set; } = null!;
    }
}
