using System;
using System.Collections.Generic;

#nullable disable

namespace HoboBot
{
    public partial class VkUsersGroup
    {
        public long UserId { get; set; }
        public long GroupId { get; set; }
        public string UserNick { get; set; }
        public bool? PrimePermision { get; set; }

        public virtual VkGroup Group { get; set; }
        public virtual VkUser User { get; set; }
        public virtual VkAchivment VkAchivment { get; set; }
        public virtual VkAvatar VkAvatar { get; set; }
    }
}
