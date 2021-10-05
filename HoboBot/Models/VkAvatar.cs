using System.Collections.Generic;

#nullable disable

namespace HoboBot
{
    public partial class VkAvatar
    {
        public VkAvatar()
        {
            VkBattles = new HashSet<VkBattle>();
        }

        public long UserId { get; set; }
        public long GroupId { get; set; }
        public string Name { get; set; }
        public short Level { get; set; }
        public int Exp { get; set; }
        public int LevelUpExp { get; set; }
        public short Satiety { get; set; }
        public short Health { get; set; }
        public int Bottels { get; set; }
        public int Money { get; set; }
        public short Mood { get; set; }
        public short WinCount { get; set; }
        public short LoseCount { get; set; }
        public short BadMedecine { get; set; }
        public short GoodMedecine { get; set; }
        public short BadFood { get; set; }
        public short GoodFood { get; set; }
        public byte WorkType { get; set; }
        public string HfId { get; set; }
        public bool KillStatus { get; set; }
        public short TopCount { get; set; }
        public string Description { get; set; }

        public virtual VkUsersGroup VkUsersGroup { get; set; }
        public virtual ICollection<VkBattle> VkBattles { get; set; }
    }
}
