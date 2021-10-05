#nullable disable

namespace HoboBot
{
    public partial class VkBattle
    {
        public int BattleId { get; set; }
        public long UserId { get; set; }
        public long GroupId { get; set; }
        public byte Type { get; set; }
        public bool? Member { get; set; }
        public byte Side { get; set; }
        public int Rate { get; set; }
        public bool? Ready { get; set; }

        public virtual VkAvatar VkAvatar { get; set; }
    }
}
