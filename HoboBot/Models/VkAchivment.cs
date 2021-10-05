#nullable disable

namespace HoboBot
{
    public partial class VkAchivment
    {
        public long UserId { get; set; }
        public long GroupId { get; set; }
        public short Achiv1 { get; set; }
        public short Achiv2 { get; set; }
        public short Achiv3 { get; set; }
        public short Achiv4 { get; set; }
        public short Achiv5 { get; set; }
        public short Achiv6 { get; set; }
        public short Achiv7 { get; set; }
        public short Achiv8 { get; set; }
        public short Achiv9 { get; set; }

        public virtual VkUsersGroup VkUsersGroup { get; set; }
    }
}
