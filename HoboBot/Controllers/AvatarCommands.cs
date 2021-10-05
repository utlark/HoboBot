using Hangfire;
using HoboBot.Extensions;
using HoboBot.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using VkNet.Abstractions;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Model.Keyboard;
using VkNet.Model.RequestParams;
using VkNet.Model;

namespace HoboBot.Controllers
{
    public class AvatarCommands
    {
        private readonly long _userId;
        private readonly long _groupId;
        private readonly IVkApi _vkApi;
        private readonly VkBotDBContext _dbContext;
        private readonly CryptoRandom _cryptoRandom;

        private readonly IWebHostEnvironment _webHostEnvironment;

        public readonly static Dictionary<int, string> WorkType = new()
        {
            {1, "Отдыхает"},
            {2, "Щарит по помойкам"},
            {3, "Просит милостыню"},
        };
        public readonly static Dictionary<int, string> Mood = new()
        {
            {2, "Превосходное"},
            {1, "Хорошее"},
            {0, "Нейтральное"},
            {-1, "Плохое"},
            {-2, "Отвратительное"},
        };

        private VkAvatar _avatar = null;
        private readonly MessagesSendParams responce;

        public AvatarCommands(IServiceProvider serviceProvider, IWebHostEnvironment webHostEnvironment, Message vkMessage)
        {
            _userId = vkMessage.FromId.Value;
            _groupId = vkMessage.PeerId.Value;
            _webHostEnvironment = webHostEnvironment;
            IServiceScope scope = serviceProvider.CreateScope();
            _dbContext = scope.ServiceProvider.GetRequiredService<VkBotDBContext>();
            _vkApi = scope.ServiceProvider.GetRequiredService<IVkApi>();
            _cryptoRandom = scope.ServiceProvider.GetRequiredService<CryptoRandom>();
            responce = new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = _groupId
            };           
        }

        public bool IfAvatarExist()
        {
            _avatar = _dbContext.VkAvatars.FirstOrDefault(p => p.UserId == _userId && p.GroupId == _groupId);
            if (_avatar != null)
                return true;
            return false;
        }

        public void LocalAvatarUpdate()
        {
            VkAchivment achivments = _dbContext.VkAchivments.FirstOrDefault(p => p.UserId == _userId && p.GroupId == _groupId);
            while (_avatar.Exp >= _avatar.LevelUpExp)
            {
                _avatar.Level++;
                _avatar.LevelUpExp++;
                _avatar.Exp -= _avatar.LevelUpExp;
            }

            if (_avatar.Level >= 20)
                achivments.Achiv5 = 20;
            else if (_avatar.Level >= 10)
                achivments.Achiv5 = 10;
            else if (_avatar.Level >= 5)
                achivments.Achiv5 = 5;

            if (_avatar.Money >= 5000)
                achivments.Achiv8 = 5000;
            else if (_avatar.Money >= 2500)
                achivments.Achiv8 = 2500;
            else if (_avatar.Money >= 500)
                achivments.Achiv8 = 500;

            if (_avatar.Bottels >= 5000)
                achivments.Achiv9 = 5000;
            else if (_avatar.Bottels >= 2500)
                achivments.Achiv9 = 2500;
            else if (_avatar.Bottels >= 500)
                achivments.Achiv9 = 500;

            string jobId;
            if (_avatar.WorkType == 1)
            {
                jobId = _userId.ToString() + _groupId.ToString();
                RecurringJob.AddOrUpdate<AvatarUpdateController>("Feed" + jobId, x => x.FeedAvatarsHF(_userId, _groupId), "* */1 * * *");
                RecurringJob.AddOrUpdate<AvatarUpdateController>("Heal" + jobId, x => x.HealAvatarsHF(_userId, _groupId), "*/30 */2 * * *");
            }
            else
            {
                jobId = _userId.ToString() + _groupId.ToString();
                RecurringJob.RemoveIfExists("Feed" + jobId);
                RecurringJob.RemoveIfExists("Heal" + jobId);
            }

            if (_avatar.Mood < -500)
                _avatar.Mood = -500;
            else if (_avatar.Mood > 500)
                _avatar.Mood = 500;

            _dbContext.VkAchivments.Update(achivments);
            _dbContext.VkAvatars.Update(_avatar);
            _dbContext.SaveChanges();
        }

        public MessagesSendParams CreateAvatar()
        {
            VkUsersGroup userGroup = _dbContext.VkUsersGroups.FirstOrDefault(p => p.UserId == _userId && p.GroupId == _groupId);
            if (userGroup == null)
            {
                VkUser user = _dbContext.VkUsers.FirstOrDefault(p => p.UserId == _userId);
                if (user == null)
                {
                    user = new VkUser
                    {
                        UserId = _userId,
                    };
                    _dbContext.VkUsers.Add(user);
                }
                VkGroup group = _dbContext.VkGroups.FirstOrDefault(p => p.GroupId == _groupId);
                if (group == null)
                {
                    group = new VkGroup
                    {
                        GroupId = _groupId
                    };
                    _dbContext.VkGroups.Add(group);
                }
                userGroup = new VkUsersGroup
                {
                    UserId = _userId,
                    GroupId = _groupId,
                };
                VkAchivment achivments = new()
                {
                    UserId = _userId,
                    GroupId = _groupId
                };
                _dbContext.VkUsersGroups.Add(userGroup);
                _dbContext.VkAchivments.Add(achivments);
            }

            _avatar = _dbContext.VkAvatars.FirstOrDefault(p => p.UserId == _userId && p.GroupId == _groupId);
            if (_avatar == null)
            {
                BotParameters parameters = JsonConvert.DeserializeObject<BotParameters>(File.ReadAllText(_webHostEnvironment.ContentRootPath + "\\botoptions.json"));
                _avatar = new VkAvatar
                {
                    UserId = _userId,
                    GroupId = _groupId,
                    Description = parameters.Descriptions[_cryptoRandom.Next(0, parameters.Descriptions.Count)]
                };
                _dbContext.VkAvatars.Add(_avatar);
                _dbContext.SaveChanges();
                responce.Message = "🎉|Бомж создан.";
            }
            else
            {
                responce.Message = "🗿|У тебя уже есть бомж.";
            }
            return responce;
        }

        public MessagesSendParams GetGroupAvatars()
        {
            List<VkAvatar> groupAvatars = _dbContext.VkAvatars.Where(p => p.GroupId == _groupId).ToList();
            if (groupAvatars.Count > 0)
            {
                string avatars = $"📖|Вот что у нас получается:\n";
                foreach (var avatar in groupAvatars)
                {
                    VkUsersGroup userGroup = _dbContext.VkUsersGroups.FirstOrDefault(p => p.GroupId == _groupId && p.UserId == avatar.UserId);
                    if (userGroup.UserNick != null)
                    {
                        avatars += $"{avatar.Name} хозяин [id{userGroup.UserId}|{userGroup.UserNick}]\n";
                    }
                    else
                    {
                        User user = _vkApi.Users.Get(new long[] { avatar.UserId }, ProfileFields.Domain).FirstOrDefault();
                        avatars += $"{avatar.Name} хозяин [id{userGroup.UserId}|@{user.Domain}]\n";
                    }
                };
                responce.Message = avatars;
            }
            else
            {
                responce.Message = "🚫📖|В данной беседе нет бомжей.";
            }
            return responce;
        }

        public MessagesSendParams GetMyAvatar()
        {
            string achivs = null;
            VkAchivment achivments = _dbContext.VkAchivments.FirstOrDefault(p => p.UserId == _userId && p.GroupId == _groupId);
            /*Проверка ачивок*/
            {
                {
                    if (achivments.Achiv1 >= 100)
                        achivs += $"🍽|Гурман|🥇\n";
                    else if (achivments.Achiv1 >= 50)
                        achivs += $"🍽|Гурман|🥈\n";
                    else if (achivments.Achiv1 >= 10)
                        achivs += $"🍽|Гурман|🥉\n";                    
                }
                {
                    if (achivments.Achiv2 >= 100)
                        achivs += $"🧻|Стальной желудок|🥇\n";
                    else if (achivments.Achiv2 >= 50)
                        achivs += $"🧻|Стальной желудок|🥈\n";
                    else if (achivments.Achiv2 >= 10)
                        achivs += $"🧻|Стальной желудок|🥉\n";                    
                }
                {
                    if (achivments.Achiv3 >= 100)
                        achivs += $"🧙|Народный целитель|🥇\n";
                    else if (achivments.Achiv3 >= 50)
                        achivs += $"🧙|Народный целитель|🥈\n";
                    else if (achivments.Achiv3 >= 10)
                        achivs += $"🧙|Народный целитель|🥉\n";                    
                }
                {
                    if (achivments.Achiv4 >= 100)
                        achivs += $"🏥|Ну эти народные средства|🥇\n";
                    else if (achivments.Achiv4 >= 50)
                        achivs += $"🏥|Ну эти народные средства|🥈\n";
                    else if (achivments.Achiv4 >= 10)
                        achivs += $"🏥|Ну эти народные средства|🥉\n";
                }
                {
                    if (achivments.Achiv5 >= 20)
                        achivs += $"👓|Авторитет|🥇\n";
                    else if (achivments.Achiv5 >= 10)
                        achivs += $"👓|Авторитет|🥈\n";
                    else if (achivments.Achiv5 >= 5)
                        achivs += $"👓|Авторитет|🥉\n";
                }
                {
                    if (achivments.Achiv6 >= 500)
                        achivs += $"🗑|Дайвер|🥇\n";
                    else if (achivments.Achiv6 >= 150)
                        achivs += $"🗑|Дайвер|🥈\n";
                    else if (achivments.Achiv6 >= 50)
                        achivs += $"🗑|Дайвер|🥉\n";
                }
                {
                    if (achivments.Achiv7 >= 500)
                        achivs += $"🙌|Попращайка|🥇\n";
                    else if (achivments.Achiv7 >= 150)
                        achivs += $"🙌|Попращайка|🥈\n";
                    else if (achivments.Achiv7 >= 50)
                        achivs += $"🙌|Попращайка|🥉\n";
                }
                {
                    if (achivments.Achiv8 >= 5000)
                        achivs += $"📈|Поднялся|🥇\n";
                    else if (achivments.Achiv8 >= 2500)
                        achivs += $"📈|Поднялся|🥈\n";
                    else if (achivments.Achiv8 >= 500)
                        achivs += $"📈|Поднялся|🥉\n";
                }
                {
                    if (achivments.Achiv9 >= 5000)
                        achivs += $"🍺|При бутылке|🥇\n";
                    else if (achivments.Achiv9 >= 2500)
                        achivs += $"🍺|При бутылке|🥈\n";
                    else if (achivments.Achiv9 >= 500)
                        achivs += $"🍺|При бутылке|🥉\n";
                }
                if (achivs != null)
                    achivs = $"🏆|Ачивки:\n" + achivs + "\n";
            }

            responce.Message = $"🐵|Имя бомжа: {_avatar.Name}\n" +
                $"⭐|Уровень бомжа: {_avatar.Level}\n" +
                $"💡|Опыт: {_avatar.Exp}/{_avatar.LevelUpExp}\n\n" +
                $"🛌|Что делает: {WorkType[_avatar.WorkType]}\n" +
                $"🥪|Сытость: {_avatar.Satiety}\n" +
                $"❤|Здоровье: {_avatar.Health}\n" +
                $"🍷|Бутылки: {_avatar.Bottels}\n" +
                $"💰|Деньги: {_avatar.Money}\n" +
                $"🎭|Настроение: {Mood[_avatar.Mood / 250]}\n\n" + achivs +
                $"🤜🏻|Побед: {_avatar.WinCount}\n" +
                $"🤛🏻|Поражений: {_avatar.LoseCount}\n" +
                $"📖|Описание:\n{_avatar.Description}";

            responce.Keyboard = new KeyboardBuilder()
                    .AddButton("Инвентарь", "инвентарь", KeyboardButtonColor.Positive)
                    .AddButton("Работы", "работы", KeyboardButtonColor.Positive)
                    .AddButton("Ачивки", "ачивки", KeyboardButtonColor.Positive)
                    .SetInline(true)
                    .Build();

            return responce;
        }

        public MessagesSendParams ChangeAvatarName(string name)
        {
            if (_avatar.Name == "Просто бомж")
            {
                _avatar.Name = name;
            }
            else
            {
                VkUser user = _dbContext.VkUsers.FirstOrDefault(p => p.UserId == _userId);
                if (user.Prime.Value)
                {
                    _avatar.Name = name;
                }
                else if (user.Money > 10)
                {
                    user.Money -= 10;
                    _avatar.Name = name;
                }
                else
                {
                    responce.Message = $"🚫|Смена имени стоит 10 рублей.";
                    return responce;
                }
                _dbContext.VkUsers.Update(user);
            }
            _dbContext.VkAvatars.Update(_avatar);
            _dbContext.SaveChanges();
            responce.Message = $"📝|Теперь твоего бомжа зовут {name}.";

            return responce;
        }//Объеденить 1

        public MessagesSendParams ChangeAvatarDescription(string description)
        {
            VkUser user = _dbContext.VkUsers.FirstOrDefault(p => p.UserId == _userId);
            if (user.Prime.Value)
            {
                _avatar.Description = description;
            }
            else if (user.Money > 20)
            {
                user.Money -= 20;
                _avatar.Description = description;
            }
            else
            {
                responce.Message = $"🚫|Смена описания стоит 20 рублей.";
                return responce;
            }
            _dbContext.VkUsers.Update(user);
            _dbContext.VkAvatars.Update(_avatar);
            _dbContext.SaveChanges();
            responce.Message = $"📝|Бомж, {_avatar.Name}, успешно сменил описание.";

            return responce;
        }//Объеденить 1

        public MessagesSendParams FeetAvatar(string foodType, int count, bool max = false)
        {
            if (_avatar.WorkType == 1)
            {
                if (max || foodType == "good" && _avatar.GoodFood >= count || foodType == "bad" && _avatar.BadFood >= count)
                {
                    int foodMinus = 0;
                    int achivePlus = 0;

                    string json = File.ReadAllText(_webHostEnvironment.ContentRootPath + "\\botoptions.json");
                    BotParameters parameters = JsonConvert.DeserializeObject<BotParameters>(json);

                    VkAchivment achivments = _dbContext.VkAchivments.FirstOrDefault(p => p.UserId == _userId && p.GroupId == _groupId);
                    switch (foodType)
                    {
                        case "good":
                            (foodMinus, achivePlus) = Feeting(count, parameters.Stats.SatietyMax, _avatar.GoodFood, parameters.ItemsAdd.GoodFood, parameters.ItemsAdd.GoodFoodMood, max);
                            _avatar.GoodFood -= (short)foodMinus;
                            achivments.Achiv1 += (short)achivePlus;
                            break;
                        case "bad":
                            (foodMinus, achivePlus) = Feeting(count, parameters.Stats.SatietyMax, _avatar.BadFood, parameters.ItemsAdd.BadFood, parameters.ItemsAdd.BadFoodMood, max);
                            _avatar.GoodFood -= (short)foodMinus;
                            achivments.Achiv2 += (short)achivePlus;
                            break;
                    }
                    _dbContext.VkAchivments.Update(achivments);
                    _dbContext.VkAvatars.Update(_avatar);
                    _dbContext.SaveChanges();

                    responce.Message = "🌯|Бомж покушал.";
                    return responce;
                }
                else
                {
                    responce.Message = "🚫|Недостаточно еды.";
                    return responce;
                }
            }
            responce.Message = "👷|Бомж на работе и не может покушать.";
            return responce;
        }//Объеденить 2

        private (int, int) Feeting(int count, int satietyMax, int foodCount, int foodAdd, int foodMood, bool max)
        {
            int foodMinus = 0;
            int achivePlus = 0;

            if (max)
            {
                while (_avatar.Satiety < satietyMax && foodCount > 0)
                {
                    _avatar.Satiety += (short)foodAdd;
                    foodMinus++;
                    achivePlus++;
                    _avatar.Mood += (short)foodMood;
                }
                _avatar.Satiety = (short)satietyMax;
            }
            else if (foodCount >= count)
            {
                for (int i = 0; i < count; i++)
                {
                    _avatar.Satiety += (short)foodAdd;
                    if (_avatar.Satiety > satietyMax)
                    {
                        _avatar.Satiety = (short)satietyMax;
                    }
                    _avatar.Mood += (short)foodMood;
                    achivePlus++;
                    foodMinus++;
                }
            }
            return (foodMinus, achivePlus);
        }//Объеденить 3

        public MessagesSendParams HealAvatar(string medecineType, int count, bool max = false)
        {
            if (_avatar.WorkType == 1)
            {
                if (max || medecineType == "good" && _avatar.GoodMedecine >= count || medecineType == "bad" && _avatar.BadMedecine >= count)
                {
                    int medicineMinus = 0;
                    int achivePlus = 0;

                    string json = File.ReadAllText(_webHostEnvironment.ContentRootPath + "\\botoptions.json");
                    BotParameters parameters = JsonConvert.DeserializeObject<BotParameters>(json);

                    VkAchivment achivments = _dbContext.VkAchivments.FirstOrDefault(p => p.UserId == _userId && p.GroupId == _groupId);
                    switch (medecineType)
                    {
                        case "good":
                            (medicineMinus, achivePlus) = Healing(count, parameters.Stats.HealMax, _avatar.GoodMedecine, parameters.ItemsAdd.GoodMedecine, max);
                            _avatar.GoodMedecine -= (short)medicineMinus;
                            achivments.Achiv4 += (short)achivePlus;
                            break;
                        case "bad":
                            (medicineMinus, achivePlus) = Healing(count, parameters.Stats.HealMax, _avatar.BadMedecine, parameters.ItemsAdd.BadMedecine, max);
                            _avatar.BadMedecine -= (short)medicineMinus;
                            achivments.Achiv3 += (short)achivePlus;
                            break;
                    }
                    _dbContext.VkAchivments.Update(achivments);
                    _dbContext.VkAvatars.Update(_avatar);
                    _dbContext.SaveChanges();

                    responce.Message = "❤|Бомж подлечился.";
                    return responce;

                }
                else
                {
                    responce.Message = "🚫|Недостаточно лечилок.";
                    return responce;
                }
            }
            responce.Message = "👷|Бомж на работе и не может подлечиться.";
            return responce;
        }//Объеденить 2

        private (int, int) Healing(int count, int healMax, int medecineCount, int medecineAdd, bool max)
        {
            int medicineMinus = 0;
            int achivePlus = 0;

            if (max)
            {
                while (_avatar.Health < healMax && medecineCount > 0)
                {
                    _avatar.Health += (short)medecineAdd;
                    medicineMinus++;
                    achivePlus++;
                }
                _avatar.Health = (short)healMax;
            }
            else if (medecineCount >= count)
            {
                for (int i = 0; i < count; i++)
                {
                    _avatar.Health += (short)medecineAdd;
                    if (_avatar.Health > healMax)
                    {
                        _avatar.Health = (short)healMax;
                    }
                    achivePlus++;
                    medicineMinus++;
                }
            }
            return (medicineMinus, achivePlus);
        }//Объеденить 3

        public MessagesSendParams KillAvatar()
        {
            if (_avatar.KillStatus)
            {
                VkAchivment achivments = _dbContext.VkAchivments.FirstOrDefault(p => p.UserId == _userId && p.GroupId == _groupId);
                _dbContext.VkAchivments.Remove(achivments);
                _dbContext.VkAvatars.Remove(_avatar);
                _dbContext.SaveChanges();
                responce.Message = "🔪|Ты убил своего бомжа.";
                return responce;
            }
            else
            {
                _avatar.KillStatus = true;
                _dbContext.SaveChanges();
                BackgroundJob.Schedule<AvatarUpdateController>(x => x.DeleteKill(_userId, _groupId), TimeSpan.FromMinutes(2));
                responce.Keyboard = new KeyboardBuilder()
                    .AddButton("Убить бомжа", "убить бомжа", KeyboardButtonColor.Negative)
                    .AddButton("Не убивать", "не убивать", KeyboardButtonColor.Positive)
                    .SetInline(true)
                    .Build();
                responce.Message = "😧|Ты точно хочешь убить своего бомжа?";
                return responce;
            }
        }

        public MessagesSendParams GetTopDayAvatar(long Time)
        {
            List<VkAvatar> avatars = _dbContext.VkAvatars.Where(p => p.GroupId == _groupId).ToList();
            if (avatars.Count > 0)
            {
                if (avatars.Count > 1)
                {
                    VkGroup group = _dbContext.VkGroups.FirstOrDefault(p => p.GroupId == _groupId);

                    string json = File.ReadAllText(_webHostEnvironment.ContentRootPath + "\\botoptions.json");
                    BotParameters parameters = JsonConvert.DeserializeObject<BotParameters>(json);

                    if ((Time - group.LastTop) / (60 * 60 * 24) > 1)
                    {
                        group.LastTop = Time;
                        int id = _cryptoRandom.Next(0, avatars.Count);
                        avatars[id].Money += parameters.ItemsAdd.TopMoney;
                        avatars[id].TopCount += 1;
                        avatars[id].Mood += (short)parameters.ItemsAdd.TopMoneyMood;
                        _dbContext.VkGroups.Update(group);
                        _dbContext.VkAvatars.Update(avatars[id]);
                        _dbContext.SaveChanges();
                        VkUsersGroup user = _dbContext.VkUsersGroups.FirstOrDefault(p => p.GroupId == _groupId && p.UserId == avatars[id].UserId);
                        if (user.UserNick == null)
                        {
                            User userVk = _vkApi.Users.Get(new long[] { avatars[id].UserId }, ProfileFields.Domain).FirstOrDefault();
                            responce.Message = $"🎉|Бомж дня у [id{avatars[id].UserId}|@{userVk.Domain}]";
                        }
                        else
                        {
                            responce.Message = $"🎉|Бомж дня у [id{avatars[id].UserId}|{user.UserNick}]";
                        }
                        return responce;
                    }
                    responce.Message = $"⏱|День еще не прошел.";
                    return responce;
                }
                responce.Message = $"🤨|В беседе только 1 бомж.";
                return responce;
            }
            responce.Message = "🚫|В беседе нет бомжей.";
            return responce;
        }

        public MessagesSendParams GetAchivments()
        {
            responce.Message = "📖|Доступные ачивки:\n" +
                "🍽|Гурман - Съесть хорошую еду [10/50/100] раз\n" +
                "🧻|Стальной желудок - Съесть плохую еду [10/50/100] раз\n" +
                "🧙|Народный целитель - Использовать санную тряпку [10/50/100] раз\n" +
                "🏥|Ну эти народные средства - Использовать аптечку [10/50/100] раз\n" +
                "👓|Авторитет - Получить [5/10/20] уровень бомжа\n" +
                "🗑|Дайвер - Общарить [50/150/500] мусорок\n" +
                "🙌|Попращайка - Получить милостыню [50/150/500] раз\n" +
                "📈|Поднялся - Заработать [500/2500/5000] рублей\n" +
                "🍺|При бутылке - Собрать [500/2500/5000] бутылок";
            return responce;
        }

        public MessagesSendParams Inventory()
        {
            responce.Keyboard = new KeyboardBuilder()
                .AddButton("Магазин", "магазин", KeyboardButtonColor.Positive)
                .SetInline(true)
                .Build();
            responce.Message = $"🧺|Ваш инвентарь:\n" +
                          $"🌭|Плохая еда: {_avatar.BadFood}\n" +
                          $"🍉|Хорошая еда: {_avatar.GoodFood}\n" +
                          $"🧻|Санные тряпки: {_avatar.BadMedecine}\n" +
                          $"💊|Аптечки: {_avatar.GoodMedecine}\n";
            return responce;
        }

        public MessagesSendParams Shop(string Command, int Count)
        {
            if (_avatar.WorkType == 1)
            {
                string json = File.ReadAllText(_webHostEnvironment.ContentRootPath + "\\botoptions.json");
                BotParameters parameters = JsonConvert.DeserializeObject<BotParameters>(json);
                switch (Command)
                {
                    case "купить еду":
                        if (_avatar.Money >= Count * parameters.ShopPrice.GoodFood)
                        {
                            _avatar.GoodFood += (short)Count;
                            _avatar.Money -= Count * parameters.ShopPrice.GoodFood;
                            _dbContext.VkAvatars.Update(_avatar);
                            _dbContext.SaveChanges();
                            responce.Message = $"👍🏻🍉|Вы купили хорошую еду в количестве: {Count}";
                            return responce;
                        }
                        responce.Message = "🚫|Недостаточно денег.";
                        return responce;
                    case "купить аптечку":
                        if (_avatar.Money >= Count * parameters.ShopPrice.Medecine)
                        {
                            _avatar.GoodMedecine += (short)Count;
                            _avatar.Money -= Count * parameters.ShopPrice.Medecine;
                            _dbContext.VkAvatars.Update(_avatar);
                            _dbContext.SaveChanges();
                            responce.Message = $"👍🏻💊|Вы купили аптечки в количестве: {Count}";
                            return responce;
                        }
                        responce.Message = "🚫|Недостаточно денег.";
                        return responce;
                    case "купить книгу":
                        if (_avatar.Money >= Count * parameters.ShopPrice.Book)
                        {
                            _avatar.Exp += Count;
                            _avatar.Money -= Count * parameters.ShopPrice.Book;
                            _dbContext.VkAvatars.Update(_avatar);
                            _dbContext.SaveChanges();
                            responce.Message = $"🧠📚|Вы купили кнги в количестве: {Count}";
                            return responce;
                        }
                        responce.Message = "🚫|Недостаточно денег.";
                        return responce;
                    case "обменять бутылки":
                        if (_avatar.Bottels > Count)
                        {
                            _avatar.Money += Count * parameters.ShopPrice.Bottel;
                            _avatar.Bottels -= Count;
                            _dbContext.VkAvatars.Update(_avatar);
                            _dbContext.SaveChanges();
                            responce.Message = $"🍷|Вы обменяли бутылки в количестве: {Count}\n" +
                                   $"💱|На {Count * parameters.ShopPrice.Bottel} денег.";
                            return responce;
                        }
                        responce.Message = "🚫|Недостаточно бутылок.";
                        return responce;
                }
            }
            responce.Message = "👷|Бомж на работе и не может ничего купить.";
            return responce;
        }

        public MessagesSendParams StartWork(string workType, int count, bool notify = false)
        {
            if (_avatar.WorkType == 1)
            {
                string json = File.ReadAllText(_webHostEnvironment.ContentRootPath + "\\botoptions.json");
                BotParameters parameters = JsonConvert.DeserializeObject<BotParameters>(json);

                if (workType == "trash" && _avatar.Satiety >= count * parameters.WorkRandom.THCoust ||
                    workType == "alms" && _avatar.Satiety >= count * parameters.WorkRandom.BACoust)
                {
                    RecurringJob.RemoveIfExists("Feed" + _userId.ToString() + _groupId.ToString());
                    RecurringJob.RemoveIfExists("Heal" + _userId.ToString() + _groupId.ToString());
                    string jobId = null;
                    if (workType == "trash")
                    {
                        jobId = BackgroundJob.Schedule<AvatarUpdateController>(p => p.TrashHF(_userId, _groupId, count, notify), TimeSpan.FromMinutes(20));
                        _avatar.WorkType = 2;
                        responce.Message = "🚮|Бомж пошел рыскать по помойкам.";
                    }
                    else if (workType == "alms")
                    {
                        jobId = BackgroundJob.Schedule<AvatarUpdateController>(p => p.AlmsHF(_userId, _groupId, count * 60, notify), TimeSpan.FromMinutes(_cryptoRandom.Next(1, 15)));
                        _avatar.WorkType = 3;
                        responce.Message = "🙏|Бомж пошел просить милостыню.";
                    }
                    _avatar.HfId = jobId;
                    _dbContext.VkAvatars.Update(_avatar);
                    _dbContext.SaveChanges();
                    return responce;
                }
                responce.Message = "🚫|Слишком много, бомж устанет.";
                return responce;
            }
            responce.Message = "👷|Бомж уже чем-то занят.";
            return responce;
        }

        public MessagesSendParams StopWork()
        {
            switch (_avatar.WorkType)
            {
                case 1:
                    responce.Message = "🛌|Бомж ничем не занят.";
                    break;
                case 2:
                case 3:
                    BackgroundJob.Delete(_avatar.HfId);
                    _avatar.WorkType = 1;
                    _dbContext.VkAvatars.Update(_avatar);
                    _dbContext.SaveChanges();
                    responce.Message = $"🛋|Бомж, {_avatar.Name}, вернулся в свое логово.";
                    break;
            }
            return responce;
        }
    }
}