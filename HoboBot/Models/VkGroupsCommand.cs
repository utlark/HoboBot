#nullable disable

namespace HoboBot
{
    public partial class VkGroupsCommand
    {
        public long GroupId { get; set; }
        public string Command { get; set; }
        public string Answer { get; set; }
        public string Prefix { get; set; }

        public virtual VkGroup Group { get; set; }
    }
}
