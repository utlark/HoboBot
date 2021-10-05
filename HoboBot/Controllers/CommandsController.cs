using System;
using System.Linq;
using System.Collections.Generic;
using VkNet.Model;
using VkNet.Model.RequestParams;
using VkNet.Enums.Filters;
using VkNet.Abstractions;
using HoboBot.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace HoboBot.Controllers
{
    public class CommandsController
    {
        private readonly IVkApi _vkApi;
        private readonly VkBotDBContext _dbContext;
        private readonly CryptoRandom _cryptoRandom;

        private readonly string _command;
        private readonly Message _message;
        private readonly byte _groupMention;
        private readonly byte _specialStart;
        private readonly MessagesSendParams _responce;

        public CommandsController(IServiceProvider serviceProvider, string command, Message message, byte groupMention, byte specialStart)
        {
            IServiceScope scope = serviceProvider.CreateScope();
            _dbContext = scope.ServiceProvider.GetRequiredService<VkBotDBContext>();
            _vkApi = scope.ServiceProvider.GetRequiredService<IVkApi>();
            _cryptoRandom = scope.ServiceProvider.GetRequiredService<CryptoRandom>();
            _groupMention = groupMention;
            _specialStart = specialStart;
            _command = command;
            _message = message;
            _responce = new MessagesSendParams
            {
                RandomId = new DateTime().Millisecond,
                PeerId = _message.PeerId
            };
            groupCommands = _dbContext.VkGroupsCommands.Where(p => p.GroupId == _message.PeerId).ToList();
        }

        private readonly static List<VkGroupsCommand> baseCommands = new()
        {
                new VkGroupsCommand
                {
                    GroupId = 0,
                    Command = "дать леща",
                    Answer = "дал леща",
                    Prefix = "👋🏻💥|"
                },
                new VkGroupsCommand
                {
                    GroupId = 0,
                    Command = "укусить",
                    Answer = "укусил",
                    Prefix = "😬|"
                },
                new VkGroupsCommand
                {
                    GroupId = 0,
                    Command = "ударить",
                    Answer = "ударил",
                    Prefix = "👊🏻💥|"
                },
                new VkGroupsCommand
                {
                    GroupId = 0,
                    Command = "дать пять",
                    Answer = "дал пять",
                    Prefix = "🤝|"
                },
                new VkGroupsCommand
                {
                    GroupId = 0,
                    Command = "обнять",
                    Answer = "обнял",
                    Prefix = "🤗|"
                },
                new VkGroupsCommand
                {
                    GroupId = 0,
                    Command = "погладить",
                    Answer = "погладил",
                    Prefix = "👋|"
                },
                new VkGroupsCommand
                {
                    GroupId = 0,
                    Command = "поцеловать",
                    Answer = "поцеловал",
                    Prefix = "😘|"
                }
        };
        private readonly static List<string> commands = new()
        {
                "имена",
                "команды",
                "наши команды",
                "команды бомжа",
                "команды другие",
        };
        private readonly static List<string> commandsArgument = new()
        {
                "имя",                
        };
        private static readonly List<string> list = new()
        {
            "скажи",
            "кто здесь",
            "удалить команду",
            "добавить команду",
        };
        private static readonly List<string> commandsStringArgument = list;
        private static List<VkGroupsCommand> groupCommands;

        public static (List<string> allCommands, List<string> commands, List<string> commandsArgument, List<string> commandsStringArgument, List<string> baseCommands) AllCommands
        {
            get
            {
                List<string> baseCommandsKey = new();
                baseCommands.ForEach(x => baseCommandsKey.Add(x.Command));
                List<string> allCommands = new();
                allCommands.AddRange(commands);
                allCommands.AddRange(commandsArgument);
                allCommands.AddRange(commandsStringArgument);
                allCommands.AddRange(baseCommandsKey);
                return (allCommands, commands, commandsArgument, commandsStringArgument, baseCommandsKey);
            }
        }        

        public MessagesSendParams GetResponceMessage()
        {
            string[] messageWords = _message.Text.Split(new char[] { ' ' });
            switch (_command)
            {
                case "скажи":
                    {
                        _responce.Message = "«" + VkString.GetVkString(messageWords) + "»";
                        VkUsersGroup user = _dbContext.VkUsersGroups.FirstOrDefault(p => p.UserId == _message.FromId && p.GroupId == _message.PeerId);
                        if (user != null && user.UserNick != null)
                        {
                            _responce.Message = $"💬|[id{_message.FromId}|{user.UserNick}] попросил меня сказать:\n{_responce.Message}";
                        }
                        else
                        {
                            User userVK = _vkApi.Users.Get(new long[] { (long)_message.FromId }, ProfileFields.Domain).FirstOrDefault();
                            _responce.Message = $"💬|[id{_message.FromId}|@{userVK.Domain}] попросил меня сказать:\n{_responce.Message}";
                        }
                    }
                    break;
                case "имя":
                    {
                        if (messageWords.Length == 1 && !messageWords[0].Contains("\n"))
                        {
                            VkUsersGroup userGroup = _dbContext.VkUsersGroups.FirstOrDefault(p => p.UserId == _message.FromId && p.GroupId == _message.PeerId);
                            if (userGroup != null)
                            {
                                userGroup.UserNick = messageWords[0];
                                _dbContext.VkUsersGroups.Update(userGroup);
                                _dbContext.SaveChanges();
                                _responce.Message = "📝|Теперь ты " + messageWords[0];
                            }
                            else
                            {
                                VkUser vkUser = _dbContext.VkUsers.FirstOrDefault(p => p.UserId == _message.FromId);
                                if (vkUser == null)
                                {
                                    vkUser = new VkUser
                                    {
                                        UserId = _message.FromId.Value,
                                    };
                                    _dbContext.VkUsers.Add(vkUser);
                                }
                                VkGroup vkGroup = _dbContext.VkGroups.FirstOrDefault(p => p.GroupId == _message.PeerId);
                                if (vkGroup == null)
                                {
                                    vkGroup = new VkGroup
                                    {
                                        GroupId = _message.PeerId.Value
                                    };
                                    _dbContext.VkGroups.Add(vkGroup);
                                }
                                VkAchivment vkAchivment = new()
                                {
                                    UserId = _message.FromId.Value,
                                    GroupId = _message.PeerId.Value
                                };
                                userGroup = new VkUsersGroup
                                {
                                    UserId = _message.FromId.Value,
                                    GroupId = _message.PeerId.Value,
                                    UserNick = messageWords[0]
                                };
                                _dbContext.VkUsersGroups.Add(userGroup);
                                _dbContext.VkAchivments.Add(vkAchivment);
                                _dbContext.SaveChanges();
                                _responce.Message = "📝|Теперь ты " + messageWords[0];
                            }
                        }
                        else
                        {
                            _responce.Message = "🚫💬|‍Имя должно состоять из одного слова.";
                        }
                    }
                    break;
                case "кто здесь":
                    {                        
                        try
                        {
                            string whoWords = VkString.GetVkString(messageWords, true);
                            GetConversationMembersResult chatUsers = _vkApi.Messages.GetConversationMembers(_message.PeerId.Value, new string[] { "domain" });
                            int randomId = _cryptoRandom.Next(0, chatUsers.Profiles.Count);
                            VkUsersGroup user = _dbContext.VkUsersGroups.FirstOrDefault(p => p.UserId == chatUsers.Profiles[randomId].Id && p.GroupId == _message.PeerId);
                            if (user != null && user.UserNick != null)
                                _responce.Message = $"💈|{whoWords} здесь [id{chatUsers.Profiles[randomId].Id}|{user.UserNick}]";
                            else
                                _responce.Message = $"💈|{whoWords} здесь [id{chatUsers.Profiles[randomId].Id}|@{chatUsers.Profiles[randomId].Domain}]";
                        }
                        catch (Exception e)
                        {
                            _responce.Message = e.Message;
                        }
                    }
                    break;
                case "удалить команду":
                    {
                        if (_groupMention == 1)
                        {
                            VkUser user = _dbContext.VkUsers.FirstOrDefault(p => p.UserId == _message.FromId);
                            if (user.Prime.Value)
                            {
                                string command = VkString.GetVkString(messageWords);

                                VkGroupsCommand groupsCommands = _dbContext.VkGroupsCommands.FirstOrDefault(p =>
                                     p.GroupId == _message.PeerId &&
                                     p.Command == command.VkTrim().Replace("\\n", "").ToLower());
                                if (groupsCommands != null)
                                {
                                    try
                                    {
                                        _dbContext.VkGroupsCommands.Remove(groupsCommands);
                                        _dbContext.SaveChanges();
                                    }
                                    catch (Exception)
                                    {
                                        _responce.Message = "🚫|Не удалось удалить команду, с ней что-то не так.";
                                        return _responce;
                                    }
                                    _responce.Message = "✅|Команда удалена.";
                                }
                                else
                                {
                                    _responce.Message = "🚫|Такой команды не существует.";
                                }
                            }
                            else
                            {
                                _responce.Message = "🚫|Пользовательские команды - прайм функция.";
                            }
                        }
                    }
                    break;
                case "добавить команду":
                    {
                        if (_groupMention == 1)
                        {
                            List<VkGroupsCommand> groupsCommands = _dbContext.VkGroupsCommands.Where(p => p.GroupId == _message.PeerId).ToList();
                            if (groupsCommands.Count <= 30)
                            {
                                VkUser user = _dbContext.VkUsers.FirstOrDefault(p => p.UserId == _message.FromId);
                                if (user.Prime.Value)
                                {
                                    string command = VkString.GetVkString(messageWords);

                                    if (command.Contains("[id") || command.Contains("[club"))
                                    {
                                        _responce.Message = "🚫|Команда не может содержать упоминаний.";
                                        return _responce;
                                    }
                                    string[] commandWords = command.Split(new char[] { '|' });

                                    if (commandWords.Length == 3 && !string.IsNullOrWhiteSpace(commandWords[0]) && !string.IsNullOrWhiteSpace(commandWords[1]))
                                    {
                                        VkGroupsCommand newCommand = new()
                                        {
                                            GroupId = _message.PeerId.Value,
                                            Command = commandWords[0].VkTrim().Replace("\\n", "").ToLower(),
                                            Answer = commandWords[1].VkTrim().Replace("\\n", "").ToLower(),
                                            Prefix = commandWords[2].VkTrim().Replace("\\n", "").ToLower(),
                                        };
                                        try
                                        {
                                            _dbContext.VkGroupsCommands.Add(newCommand);
                                            _dbContext.SaveChanges();
                                        }
                                        catch (Exception)
                                        {
                                            _responce.Message = "🚫|Не удалось добавить команду, с ней что-то не так.";
                                            return _responce;
                                        }
                                        _responce.Message = "✅|Команда успешно добавлена.";
                                    }
                                    else
                                    {
                                        _responce.Message = "🚫|Ошибка в аргументах.";
                                    }
                                }
                                else
                                {
                                    _responce.Message = "🚫|Пользовательские команды - прайм функция.";
                                }
                            }
                            else
                            {
                                _responce.Message = "🚫|Максимальное количество пользовательских команд в беседе - 30 штук.";
                            }
                        }
                    }
                    break;
                case "команды":
                    {
                        _responce.Message += $"📕|Все доступные команды:\n";
                        baseCommands.ForEach(
                            x => _responce.Message += $"{x.Command} [кого] => " + $"{x.Prefix} [ты] {x.Answer} [кого]\n");
                    }
                    break;
                case "команды бомжа":
                    {
                        _responce.Message += $"📗|Все доступные команды:\n";
                        AvatarCommandsController.AllCommands.сommands.ForEach(
                            x => _responce.Message += $"[club194159195|@hobobot] {char.ToUpper(x[0]) + x[1..]}\n");
                        AvatarCommandsController.AllCommands.сommandsArgument.ForEach(
                            x => _responce.Message += $"[club194159195|@hobobot] {char.ToUpper(x[0]) + x[1..]} [параметр]\n");
                        AvatarCommandsController.AllCommands.commandsTwoArgument.ForEach(
                            x => _responce.Message += $"[club194159195|@hobobot] {char.ToUpper(x[0]) + x[1..]} [параметр] [параметр]\n");
                        AvatarCommandsController.AllCommands.commandsStringArgument.ForEach(
                            x => _responce.Message += $"[club194159195|@hobobot] {char.ToUpper(x[0]) + x[1..]} [cтрока]\n");
                    }
                    break;
                case "команды другие":
                    {
                        _responce.Message += $"📘|Все доступные команды:\n";
                        commands.ForEach(
                            x => _responce.Message += $"[club194159195|@hobobot] {char.ToUpper(x[0]) + x[1..]}\n");
                        commandsArgument.ForEach(
                            x => _responce.Message += $"[club194159195|@hobobot] {char.ToUpper(x[0]) + x[1..]} [параметр]\n");
                        commandsStringArgument.ForEach(
                            x => _responce.Message += $"[club194159195|@hobobot] {char.ToUpper(x[0]) + x[1..]} [строка]\n");
                    }
                    break;
                case "наши команды":
                    {
                        List<VkGroupsCommand> customCommand = _dbContext.VkGroupsCommands.Where(p => p.GroupId == _message.PeerId).ToList();
                        if (customCommand.Count == 0)
                        {
                            _responce.Message = "🚫📖|В данной беседе нет пользовательских команд.";
                        }
                        else
                        {
                            _responce.Message += $"📖|Вот что у нас получается:\n";
                            customCommand.ForEach(
                                x => _responce.Message += $"{x.Command} [кого] => " + $"{x.Prefix} [ты] {x.Answer} [кого]\n");
                        }
                    }
                    break;
                case "имена":
                    {
                        List<VkUsersGroup> usersGroup = _dbContext.VkUsersGroups.Where(p => p.GroupId == _message.PeerId).ToList();
                        if (usersGroup.Count == 0)
                        {
                            _responce.Message = "🚫📖|В данной беседе нет имен.";
                        }
                        else
                        {
                            _responce.Message += $"📖|Вот что у нас получается:\n";
                            usersGroup.ForEach(x =>
                            {
                                if (x.UserNick != null)
                                {
                                    User user = _vkApi.Users.Get(new long[] { x.UserId }).FirstOrDefault();
                                    _responce.Message += $"{user.FirstName} {user.LastName} у нас [id{x.UserId}|{x.UserNick}]\n";
                                }
                            });
                        }
                    }
                    break;
                default:
                    UserCommands(messageWords);
                    break;
            };
            return _responce;
        }

        private void UserCommands(string[] messageWords)
        {
            VkGroupsCommand command = null;
            if (groupCommands.FirstOrDefault(x => x.Command == _command) != null)
            {
                VkUser user = _dbContext.VkUsers.FirstOrDefault(p => p.UserId == _message.FromId);
                if (user != null && !user.Prime.Value)
                {
                    _responce.Message = "🚫|Пользовательские команд - прайм функция.";
                    return;
                }
                command = groupCommands.FirstOrDefault(x => x.Command == _command);
            }
            else
            {
                command = baseCommands.FirstOrDefault(x => x.Command == _command);
            }

            string userToId = null;
            VkUsersGroup userFrom = null;
            VkUsersGroup userTo = null;
            string toType = null;//Кому адресовано, группе или человеку
            int withStart = 0;//Исключяет упоминание пользователя из слов ответа
            string[] types = new string[2] { "[id", "[club" };
          
            if (messageWords.Length >= 1 && types.Any(s => messageWords[0].Contains(s)) && messageWords[0].Contains("|"))
            {
                int startIndex = messageWords[0].IndexOfAny("0123456789".ToCharArray());
                userToId = messageWords[0][startIndex..messageWords[0].IndexOf('|')];
                if (messageWords[0].Contains("id"))
                    toType = "id";
                else if (messageWords[0].Contains("club"))
                    toType = "club";
                withStart = 1;
            }//Если мы упоминали получателя.            
            else if (_message.ReplyMessage != null)
            {
                userToId = _message.ReplyMessage.FromId.ToString();
                if (_message.ReplyMessage.FromId.ToString().Contains("-"))
                {
                    toType = "club";
                    userToId = userToId.Replace("-", "");
                }
                else
                {
                    toType = "id";
                }
            }//Если мы не упоминали получателя, но прикрепили сообщение.            
            else if (_groupMention == 1)
            {
                userToId = _message.FromId.ToString();
                toType = "id";
            }//Если мы не упоминали получателя, не прикрепили сообщение и упоминули бота.            
            else
            {
                return;
            }//Иначе мы идем лесом.

            if (!(userToId != null && userToId.All(char.IsDigit)))
                return;

            userFrom = _dbContext.VkUsersGroups.FirstOrDefault(p => p.UserId == _message.FromId && p.GroupId == _message.PeerId);
            userTo = _dbContext.VkUsersGroups.FirstOrDefault(p => p.UserId == long.Parse(userToId) && p.GroupId == _message.PeerId);

            if (messageWords.Length > withStart && !string.IsNullOrEmpty(messageWords[0]))
                _responce.Message = "\n💬|Со словами: «" + VkString.GetVkString(messageWords, false, withStart) + "»";

            if (userFrom != null && userFrom.UserNick != null && userTo != null && userTo.UserNick != null)
            {
                _responce.Message = $"{command.Prefix}|[id{_message.FromId}|{userFrom.UserNick}] {command.Answer} [{toType}{userToId}|{userTo.UserNick}]{_responce.Message}";
            }
            else if (userFrom == null && userTo != null && userTo.UserNick != null || 
                userFrom != null && userFrom.UserNick == null && userTo != null && userTo.UserNick != null)
            {
                User userFromVk = _vkApi.Users.Get(new long[] { Convert.ToInt64(_message.FromId) }, ProfileFields.Domain).FirstOrDefault();
                _responce.Message = $"{command.Prefix}|[id{_message.FromId}|@{userFromVk.Domain}] {command.Answer} [{toType}{userToId}|{userTo.UserNick}]{_responce.Message}";
            }
            else if (userFrom != null && userFrom.UserNick != null && userTo == null ||
                userFrom != null && userFrom.UserNick != null && userTo != null && userTo.UserNick == null)
            {
                if (toType == "id")
                {
                    User userToVk = _vkApi.Users.Get(new long[] { Convert.ToInt64(userToId) }, ProfileFields.Domain).FirstOrDefault();
                    _responce.Message = $"{command.Prefix}|[id{_message.FromId}|{userFrom.UserNick}] {command.Answer} [{toType}{userToVk.Id}|@{userToVk.Domain}]{_responce.Message}";
                }
                else if (toType == "club")
                {
                    Group group = _vkApi.Groups.GetById(new string[] { userToId }, userToId, null).FirstOrDefault();
                    _responce.Message = $"{command.Prefix}|[id{_message.FromId}|{userFrom.UserNick}] {command.Answer} [{toType}{group.Id}|{group.Name}]{_responce.Message}";
                }
            }
            else if (userFrom == null && userTo == null ||
                userFrom == null && userTo != null && userTo.UserNick == null ||
                userFrom != null && userFrom.UserNick == null && userTo == null ||
                userFrom != null && userFrom.UserNick == null && userTo != null && userTo.UserNick == null)
            {
                User userFromVk = _vkApi.Users.Get(new long[] { Convert.ToInt64(_message.FromId) }, ProfileFields.Domain).FirstOrDefault();
                if (toType == "id")
                {
                    User userToVk = _vkApi.Users.Get(new long[] { Convert.ToInt64(userToId) }, ProfileFields.Domain).FirstOrDefault();
                    _responce.Message = $"{command.Prefix}|[id{_message.FromId}|@{userFromVk.Domain}] {command.Answer} [{toType}{userToVk.Id}|@{userToVk.Domain}]{_responce.Message}";
                }
                else if (toType == "club")
                {
                    Group group = _vkApi.Groups.GetById(new string[] { userToId }, userToId, null).FirstOrDefault();
                    _responce.Message = $"{command.Prefix}|[id{_message.FromId}|@{userFromVk.Domain}] {command.Answer} [{toType}{group.Id}|{group.Name}]{_responce.Message}";
                }
            }
        }
    }
}
