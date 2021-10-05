using System;
using System.Linq;
using System.Collections.Generic;
using Hangfire;
using VkNet.Model;
using VkNet.Model.RequestParams;
using Microsoft.AspNetCore.Hosting;
using HoboBot.Extensions;

namespace HoboBot.Controllers
{
    public class AvatarCommandsController
    {       
        private readonly IServiceProvider _serviceProvider;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string _command;
        private readonly Message _message;
        private readonly byte _groupMention;

        public AvatarCommandsController(IServiceProvider serviceProvider, IWebHostEnvironment webHostEnvironment, string command, Message message, byte groupMention)
        {
            _webHostEnvironment = webHostEnvironment;
            _serviceProvider = serviceProvider;
            _groupMention = groupMention;
            _command = command;
            _message = message;
            _responce = new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = _message.PeerId
            };
        }            

        private readonly static List<string> сommands = new()
        {
                "бомжи",
                "ачивки",
                "бомж дня",
                "мой бомж",
                "бомж как",
                "инвентарь",
                "убить бомжа",
                "создать бомжа",
                "завершить работу",
            };
        private readonly static List<string> сommandsArgument = new()
        {
                "дать бомжу имя",
                "купить книги",
                "купить аптечки",
                "обменять бутылки",
                "купить хорошую еду",
                "съесть плохую еду",
                "съесть хорошую еду",
                "использовать аптечку",
                "использовать санную тряпку",
            };
        private readonly static List<string> commandsTwoArgument = new()
        {
                "шарить по помойкам",
                "просить милостыню",
            };
        private readonly static List<string> commandsStringArgument = new()
        {
                "сменить описание",
            };
        private readonly static List<string> buttonResponses = new()
        {
                "не убивать",
                "магазин",
                "работы",
            };

        public static (List<string> allCommands, List<string> сommands, List<string> сommandsArgument, List<string> commandsTwoArgument, List<string> commandsStringArgument, List<string> buttonResponses) AllCommands
        {
            get
            {
                List<string> allCommands = new();
                allCommands.AddRange(сommands);
                allCommands.AddRange(сommandsArgument);
                allCommands.AddRange(commandsTwoArgument);
                allCommands.AddRange(commandsStringArgument);
                allCommands.AddRange(buttonResponses);
                return (allCommands, сommands, сommandsArgument, commandsTwoArgument, commandsStringArgument, buttonResponses);
            }
        }

        private MessagesSendParams _responce;       

        public MessagesSendParams GetAvatarResponceMessage() 
        {
            string[] messageWords = _message.Text.Split(new char[] { ' ' });
            AvatarCommands actions = new(_serviceProvider, _webHostEnvironment, _message);           
            switch (_command)
            {
                case "бомжи":
                    _responce = actions.GetGroupAvatars();
                    break;
                case "бомж как":
                    {
                        _responce.Message = "Как завести бомжа?\n" +
                             "@hobobot создать бомжа\n\n" +
                             "Как удалить бомжа?\n" +
                             "@hobobot убить бомжа\n" +
                             "Нажать кнопку Убить бомжа\n" +
                             "Или в течение 2 минут повторить команду\n\n" + "Как дать бомжу имя?\n" +
                             "@hobobot дать бомжу имя [имя_однимСловом]\n\n" +
                             "Как пользоваться магазином?\n" +
                             "@hobobot [команда магазина] [кол-во предмета]\n\n" +
                             "Как пользоваться инвентарем?\n" +
                             "@hobobot [предмет инвентаря] [кол-во предмета] или [макс]\n" +
                             "[макс] - максимальное восстановление использую эти предметы\n\n" +
                             "Как пойти на работу?\n" +
                             "@hobobot [работа] [кол-во/часы] и [вкл уведы]\n" +
                             "[кол-во] - количество мусорок, 20 минут на 1 мусорку, обязательно\n" +
                             "[часы] - часы милостыни, человек каждые 1-15 минут, минимум 1 час, обязательно\n" +
                             "[вкл уведы] - уведомлять о находках в каждой мусорке/каждом прошедшем человеке, не обязательно, по умолчанию нет, писать \"да\"";
                    }
                    break;
                case "создать бомжа":
                    _responce = actions.CreateAvatar();
                    break;
                case "ачивки":
                    _responce = actions.GetAchivments();
                    break;
                case "бомж дня":
                    _responce = actions.GetTopDayAvatar(((DateTimeOffset)_message.Date).ToUnixTimeSeconds());
                    break;
                default:
                    AvatarExistCommands(messageWords, actions);
                    break;
            }
            return _responce;
        }

        private void AvatarExistCommands(string[] messageWords, AvatarCommands actions)
        {
            if (actions.IfAvatarExist())
            {
                actions.LocalAvatarUpdate();
                switch (_command)
                {
                    case "мой бомж":
                        _responce = actions.GetMyAvatar();
                        break;
                    case "инвентарь":
                        _responce = actions.Inventory();
                        break;
                    case "убить бомжа":
                        _responce = actions.KillAvatar();
                        break;
                    case "дать бомжу имя":
                        _responce = actions.ChangeAvatarName(messageWords[0]);
                        break;
                    case "сменить описание":
                        {
                            string description = VkString.GetVkString(messageWords, true);
                            if (description.Length <= 1500)
                                _responce = actions.ChangeAvatarDescription(description);
                            else
                                _responce.Message = "🚫|Описание не должно привышать 1500 символов.";
                        }
                        break;
                    case "съесть плохую еду":
                    case "съесть хорошую еду":
                    case "использовать санную тряпку":
                    case "использовать аптечку":
                        {
                            int count = 0;
                            bool isMax = messageWords[0] == "макс";
                            if (messageWords[0].All(char.IsDigit))
                            {
                                count = int.Parse(messageWords[0]);
                                if (count < 1)
                                {
                                    _responce.Message = "🧠|Нужно указать количество еды, а не свой IQ.";
                                    break;
                                }
                            }
                            if (!isMax && count == 0)
                            {
                                _responce.Message = "🚫💬|Ошибка в аргуметах.";
                                break;
                            }
                            switch (_command)
                            {
                                case "съесть плохую еду":
                                    _responce = actions.FeetAvatar("bad", count, isMax);
                                    break;
                                case "съесть хорошую еду":
                                    _responce = actions.FeetAvatar("good", count, isMax);
                                    break;
                                case "использовать санную тряпку":
                                    _responce = actions.HealAvatar("bad", count, isMax);
                                    break;
                                case "использовать аптечку":
                                    _responce = actions.HealAvatar("good", count, isMax);
                                    break;
                            }
                        }
                        break;
                    case "купить хорошую еду":
                    case "купить аптечку":
                    case "купить аптечки":
                    case "купить книгу":
                    case "купить книги":
                    case "обменять бутылки":
                        {
                            int count = 0;
                            if (messageWords.Length == 0 && _command == "купить книгу" ||
                                messageWords.Length == 0 && _command == "купить аптечку")
                            {
                                count = 1;
                            }
                            else if (messageWords[0].All(char.IsDigit))
                            {
                                count = int.Parse(messageWords[0]);
                                if (count < 1)
                                {
                                    _responce.Message = "🧠|Нужно указать количество еды, а не свой IQ.";
                                    break;
                                }
                            }
                            if (count == 0)
                            {
                                _responce.Message = "🚫💬|Ошибка в аргуметах.";
                                break;
                            }
                            switch (_command)
                            {
                                case "купить хорошую еду":
                                    _responce = actions.Shop("купить еду", count);
                                    break;
                                case "купить аптечки":
                                    _responce = actions.Shop("купить аптечку", count);
                                    break;
                                case "купить книги":
                                    _responce = actions.Shop("купить книгу", count);
                                    break;
                                case "обменять бутылки":
                                    _responce = actions.Shop("обменять бутылки", count);
                                    break;
                            }
                        }
                        break;
                    case "просить милостыню":
                    case "шарить по помойкам":
                        {
                            int count = 0;
                            bool notify = messageWords.Length == 2 && messageWords[1] == "да";
                            if (messageWords[0].All(char.IsDigit))
                            {
                                count = int.Parse(messageWords[0]);
                                if (count < 1)
                                {
                                    _responce.Message = "🚫🕜|Нужно указать время в часах\\количество мусорок в штуках.";
                                    break;
                                }
                            }
                            switch (_command)
                            {
                                case "просить милостыню":
                                    _responce = actions.StartWork("alms", count, notify);
                                    break;
                                case "шарить по помойкам":
                                    _responce = actions.StartWork("trash", count, notify);
                                    break;
                            }
                        }
                        break;
                    case "завершить работу":
                        _responce = actions.StopWork();
                        break;
                    default:
                        ButtonsCommands();
                        break;
                }                
            }
            else
            {
                _responce.Message = "🚫|У тебя нет бомжа.";
            }
        }

        private void ButtonsCommands() 
        {
            if (_groupMention == 1 && buttonResponses.Contains(_command) && _message.Payload != null)
            {
                switch (_command)
                {
                    case "не убивать":
                        if (_message.Payload.Contains("не убивать"))
                        {
                            BackgroundJob.Enqueue<AvatarUpdateController>(x => x.DeleteKill(_message.FromId.Value, _message.PeerId.Value));
                        }
                        break;
                    case "магазин":
                        if (_message.Payload.Contains("магазин"))
                        {
                            _responce.Message = "🏪В магазине можно:\n" +
                                "@hobobot Купить книгу\n" +
                                "@hobobot Купить книги [кол-во]\n" +
                                "@hobobot Купить аптечку\n" +
                                "@hobobot Купить аптечки [кол-во]\n" +
                                "@hobobot Купить хорошую еду\n" +
                                "@hobobot Купить хорошую еду [кол-во]\n" +
                                "@hobobot Обменять бутылки [кол-во]";
                        }
                        break;
                    case "работы":
                        if (_message.Payload.Contains("работы"))
                        {
                            _responce.Message = "Доступные работы:\n" +
                                "@hobobot Шарить по помойкам [кол-во] [вкл уведы]\n" +
                                "@hobobot Просить милостыню [часов] [вкл уведы]";
                        }
                        break;
                }
            }
        }
    }
}
