using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using VkNet;
using VkNet.Model;
using VkNet.Model.RequestParams;
using Newtonsoft.Json;
using System.IO;
using HoboBot.Models;
using Microsoft.AspNetCore.Hosting;
using HoboBot.Extensions;

namespace HoboBot.Controllers
{
    public class AvatarUpdateController
    {
        private readonly VkBotDBContext _dbContext;
        private readonly BotParameters _parameters;
        private readonly VkApi _vkApi = new();
        private readonly MessagesSendParams _responce;
        public AvatarUpdateController(IServiceProvider serviceProvider, IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            IServiceScope scope = serviceProvider.CreateScope();
            _dbContext = scope.ServiceProvider.GetRequiredService<VkBotDBContext>();
            string json = File.ReadAllText(webHostEnvironment.ContentRootPath + "\\botoptions.json");
            _parameters = JsonConvert.DeserializeObject<BotParameters>(json);
            _vkApi.Authorize(new ApiAuthParams { AccessToken = configuration["Config:AccessToken"] });
            _responce = new MessagesSendParams { RandomId = new DateTime().Millisecond };
        }

        public void HealAvatarsHF(long userId, long groupId)
        {
            VkAvatar avatar = _dbContext.VkAvatars.FirstOrDefault(p => p.UserId == userId && p.GroupId == groupId);
            if (avatar != null)
                if (avatar.WorkType == 1)
                {
                    avatar.Health += (short)_parameters.Recovery.Heal;
                    if (avatar.Health > _parameters.Stats.HealMax)
                        avatar.Health = (short)_parameters.Stats.HealMax;
                    _dbContext.VkAvatars.Update(avatar);
                    _dbContext.SaveChanges();
                }               
        }

        public void FeedAvatarsHF(long userId, long groupId)
        {
            VkAvatar avatar = _dbContext.VkAvatars.FirstOrDefault(p => p.UserId == userId && p.GroupId == groupId);
            if (avatar != null)
                if (avatar.WorkType == 1)
                {
                    avatar.Satiety += (short)_parameters.Recovery.Satiety;
                    if (avatar.Satiety > _parameters.Stats.SatietyMax)
                        avatar.Satiety = (short)_parameters.Stats.SatietyMax;
                    _dbContext.VkAvatars.Update(avatar);
                    _dbContext.SaveChanges();
                }
        }

        public void TrashHF(long userId, long groupId, int count, bool notify)
        {
            count--;
            CryptoRandom random = new();
            VkAvatar avatar = _dbContext.VkAvatars.FirstOrDefault(p => p.UserId == userId && p.GroupId == groupId);
            VkAchivment vkAchivment = _dbContext.VkAchivments.FirstOrDefault(p => p.UserId == userId && p.GroupId == groupId);
            string faced = null;           
            string responceMessage = null;
            int Bottels = avatar.Bottels;
            int Money = avatar.Money;
            int GoodFood = avatar.GoodFood;
            int BadFood = avatar.BadFood;
            int BadMedecine = avatar.BadMedecine;
            int GoodMedecine = avatar.GoodMedecine;

            if (random.Next(1, 101) <= GetMoodedInt(_parameters.WorkRandom.BottelsChance, avatar.Mood))
                avatar.Bottels += random.Next(1, GetMoodedInt(_parameters.WorkRandom.BottelsCount + 1, avatar.Mood));            
            if (random.Next(1, 101) <= GetMoodedInt(_parameters.WorkRandom.BadFoodChance, avatar.Mood))
                avatar.BadFood += (short)random.Next(1, GetMoodedInt(_parameters.WorkRandom.BadFoodCount + 1, avatar.Mood));           
            if (random.Next(1, 101) <= GetMoodedInt(_parameters.WorkRandom.BadMedecineChance, avatar.Mood))
                avatar.BadMedecine += (short)random.Next(1, GetMoodedInt(_parameters.WorkRandom.BadMedecineCount + 1, avatar.Mood));

            int badChance = random.Next(1, 101);
            if (badChance < _parameters.WorkRandom.THGopnikChance)
            {
                faced = "👊|Он столкнулся с гопниками.\n";
                if (avatar.Money > 0)
                    avatar.Money -= random.Next(1, avatar.Money);
                avatar.Health -= (short)random.Next(1, _parameters.WorkRandom.GopnikDamage + 1);               
                avatar.Mood -= (short)_parameters.WorkRandom.GopnikMood;
            }
            else if (badChance < _parameters.WorkRandom.THHoboChance)
            {
                faced = "👊|Он столкнулся со злым бомжом.\n";
                if (avatar.Money > 1)
                    avatar.Money -= random.Next(1, avatar.Money / 2);
                if (avatar.Bottels > 1)
                    avatar.Bottels -= random.Next(1, avatar.Bottels / 2);
                if (avatar.GoodFood > 1)
                    avatar.GoodFood -= (short)random.Next(1, avatar.GoodFood / 2);
                if (avatar.BadFood > 1)
                    avatar.BadFood -= (short)random.Next(1, avatar.BadFood / 2);
                if (avatar.GoodMedecine > 1)
                    avatar.GoodMedecine -= (short)random.Next(1, avatar.GoodMedecine / 2);
                if (avatar.BadMedecine > 1)
                    avatar.BadMedecine -= (short)random.Next(1, avatar.BadMedecine / 2);
                avatar.Health -= (short)random.Next(1, _parameters.WorkRandom.HoboDamage + 1);                
                avatar.Mood -= (short)_parameters.WorkRandom.HoboMood;                
            }

            avatar.Exp++;
            avatar.Satiety -= (short)_parameters.WorkRandom.THCoust;
            avatar.Mood += (short)_parameters.WorkRandom.TrashMood;
            if (avatar.Mood > 1000)
                avatar.Mood = 1000;
            if (avatar.Mood < 0)
                avatar.Mood = 0;
            if (avatar.Health <= 0)
            {
                avatar.Health = 0;
                count = 0;
            }
                                
            if (notify)
            {
                if (avatar.Bottels - Bottels != 0)
                    responceMessage += $"🍷|Бутылки: {avatar.Bottels - Bottels}\n";
                if (avatar.Money - Money != 0)
                    responceMessage += $"💰|Деньги: {avatar.Money - Money}\n";
                if (avatar.GoodFood - GoodFood != 0)
                    responceMessage += $"🍉|Хорошую еду: {avatar.GoodFood - GoodFood}\n";
                if (avatar.BadFood - BadFood != 0)
                    responceMessage += $"🌭|Плохую еду: {avatar.BadFood - BadFood}\n";
                if (avatar.GoodMedecine - GoodMedecine != 0)
                    responceMessage += $"💊|Аптечки: {avatar.GoodMedecine - GoodMedecine}\n";
                if (avatar.BadMedecine - BadMedecine != 0)
                    responceMessage += $"🧻|Санные тряпки: {avatar.BadMedecine - BadMedecine}\n\n";

                if (responceMessage != null)
                    responceMessage = $"📈|И в итоге собрал/потерял:|📉\n" + responceMessage;
                responceMessage = $"🐵|Бомж, {avatar.Name}, обшарил мусорку, осталось {count}.\n" + faced + responceMessage;
            }

            if (count >= 1 && avatar.Satiety > 0)
            {
                avatar.HfId = BackgroundJob.Schedule<AvatarUpdateController>(x => x.TrashHF(userId, groupId, count, notify), TimeSpan.FromMinutes(20));
            }
            else
            {
                avatar.WorkType = 1;
                avatar.Satiety = 0;
                responceMessage += $"🛋|Бомж, {avatar.Name}, вернулся в свое логово.";
            }

            if (responceMessage != null)
            {
                _responce.PeerId = groupId;
                _responce.Message = responceMessage;
                _vkApi.Messages.Send(_responce);
            }

            vkAchivment.Achiv6++;

            _dbContext.VkAchivments.Update(vkAchivment);
            _dbContext.VkAvatars.Update(avatar);
            _dbContext.SaveChanges();
        }

        public void AlmsHF(long userId, long groupId, int minutes, bool notify)
        {            
            CryptoRandom _random = new();
            VkAvatar avatar = _dbContext.VkAvatars.FirstOrDefault(p => p.UserId == userId && p.GroupId == groupId);
            VkAchivment vkAchivment = _dbContext.VkAchivments.FirstOrDefault(p => p.UserId == userId && p.GroupId == groupId);
            string faced = null;
            string responceMessage = null;
            int Money = avatar.Money;

            int chance = _random.Next(1, 101);
            if (chance < _parameters.WorkRandom.BAGopnikChance)
            {
                if (avatar.Money > 1)
                {
                    int change = _random.Next(1, avatar.Money / 2);
                    avatar.Money -= change;
                    avatar.Health -= (short)_random.Next(1, _parameters.WorkRandom.GopnikDamage + 1);                    
                    avatar.Mood -= (short)_parameters.WorkRandom.GopnikMood;
                    faced = $"👊|К {avatar.Name} подошли гопники и отжали рублей: {change}\n";
                }
            }
            else if (chance < GetMoodedInt(_parameters.WorkRandom.BAManChance, avatar.Mood))
            {
                int change = _random.Next(1, GetMoodedInt(_parameters.WorkRandom.BAManCount + 1, avatar.Mood));
                avatar.Money += change;
                avatar.Exp++;
                avatar.Mood += (short)_parameters.WorkRandom.ManMood;
                faced = $"💸|Мимо {avatar.Name} прошел человек и кинул рублей: {change}\n";
                vkAchivment.Achiv7++;
            }

            if (avatar.Mood > 500)
                avatar.Mood = 500;

            if (minutes > 0 && minutes % 60 == 0)
                avatar.Satiety -= (short)_parameters.WorkRandom.BACoust;

            int nextMan = _random.Next(1, _parameters.WorkRandom.BAManInterval + 1);
            minutes -= nextMan;
            if (notify)
                responceMessage += faced;

            if (minutes > 0 && avatar.Satiety > 0)
            {
                avatar.HfId = BackgroundJob.Schedule<AvatarUpdateController>(x => x.AlmsHF(userId, groupId, minutes, notify), TimeSpan.FromMinutes(nextMan));
            }
            else
            {
                avatar.WorkType = 1;
                avatar.Satiety = 0;
                responceMessage += $"🛋|Бомж, {avatar.Name}, вернулся в свое логово.";
            }

            if (responceMessage != null)
            {
                _responce.PeerId = groupId;
                _responce.Message = responceMessage;
                _vkApi.Messages.Send(_responce);
            }

            _dbContext.VkAchivments.Update(vkAchivment);
            _dbContext.VkAvatars.Update(avatar);
            _dbContext.SaveChanges();
        }

        public void DeleteKill(long userId, long groupId)
        {
            VkAvatar avatar = _dbContext.VkAvatars.FirstOrDefault(p => p.UserId == userId && p.GroupId == groupId);
            if (avatar != null)
            {
                avatar.KillStatus = false;
                _dbContext.VkAvatars.Update(avatar);
                _dbContext.SaveChanges();
            }
        }

        private static int GetMoodedInt(int chance, int mood) 
        {
            return chance + (chance * (mood / 2000));
        } 
    }
}
