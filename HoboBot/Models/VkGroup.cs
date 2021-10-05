using System.Collections.Generic;

#nullable disable

namespace HoboBot
{
    public partial class VkGroup
    {
        public VkGroup()
        {
            VkGroupsCommands = new HashSet<VkGroupsCommand>();
            VkUsersGroups = new HashSet<VkUsersGroup>();
        }

        public long GroupId { get; set; }
        public long LastTop { get; set; }

        public virtual ICollection<VkGroupsCommand> VkGroupsCommands { get; set; }
        public virtual ICollection<VkUsersGroup> VkUsersGroups { get; set; }
    }
}
