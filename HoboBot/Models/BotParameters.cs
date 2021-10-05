using System.Collections.Generic;

namespace HoboBot.Models
{
    public class BotParameters
    {
        public Stats Stats { get; set; }
        public Recovery Recovery { get; set; }
        public ItemsAdd ItemsAdd { get; set; }
        public WorkRandom WorkRandom { get; set; }
        public ShopPrice ShopPrice { get; set; }
        public List<string> Descriptions { get; set; }
    }

    public class Stats
    {
        public int HealMax { get; set; }
        public int SatietyMax { get; set; }
    }

    public class Recovery
    {
        public int Heal { get; set; }
        public int Satiety { get; set; }
    }

    public class ItemsAdd
    {
        public int GoodMedecine { get; set; }
        public int BadMedecine { get; set; }
        public int GoodFood { get; set; }
        public int GoodFoodMood { get; set; }
        public int BadFood { get; set; }
        public int BadFoodMood { get; set; }
        public int TopMoney { get; set; }
        public int TopMoneyMood { get; set; }
    }

    public class WorkRandom
    {
        public int THCoust { get; set; }
        public int BACoust { get; set; }
        public int BAManInterval { get; set; }
        public int BottelsChance { get; set; }
        public int BottelsCount { get; set; }
        public int BadMedecineChance { get; set; }
        public int BadMedecineCount { get; set; }
        public int BadFoodChance { get; set; }
        public int BadFoodCount { get; set; }
        public int THGopnikChance { get; set; }
        public int GopnikDamage { get; set; }
        public int THHoboChance { get; set; }
        public int HoboDamage { get; set; }
        public int BAGopnikChance { get; set; }
        public int BAManChance { get; set; }
        public int BAManCount { get; set; }
        public int GopnikMood { get; set; }
        public int HoboMood { get; set; }
        public int ManMood { get; set; }
        public int TrashMood { get; set; }
    }

    public class ShopPrice
    {
        public int Medecine { get; set; }
        public int GoodFood { get; set; }
        public int Book { get; set; }
        public int Bottel { get; set; }
    }
}
