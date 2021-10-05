using System.Collections.Generic;

#nullable disable

namespace HoboBot
{
    public partial class VkUser
    {
        public VkUser()
        {
            VkUsersGroups = new HashSet<VkUsersGroup>();
        }

        public long UserId { get; set; }
        public bool? Prime { get; set; }
        public short Money { get; set; }

        public virtual ICollection<VkUsersGroup> VkUsersGroups { get; set; }
    }
}
