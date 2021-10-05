using HoboBot.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using VkNet.Abstractions;
using VkNet.Model.RequestParams;
using VkNet.Model;
using VkNet.Utils;

namespace HoboBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CallbackController : ControllerBase
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly VkBotDBContext _dbContext;
        private readonly IVkApi _vkApi;

        private MessagesSendParams _responce;

        public CallbackController(IServiceProvider serviceProvider, IVkApi vkApi, IConfiguration configuration, IWebHostEnvironment webHostEnvironment, VkBotDBContext dbContext)
        {            
            _vkApi = vkApi;
            _dbContext = dbContext;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _webHostEnvironment = webHostEnvironment;           
        }

        [HttpPost]
        public IActionResult Callback([FromBody] Updates updates)
        {
            if (updates.Secret == _configuration["Config:Secret"])
                switch (updates.Type)
                {
                    case "confirmation":
                        return Ok(_configuration["Config:Confirmation"]);

                    case "message_new":
                            ResponceToMessage(updates);
                        break;
                }
            return Ok("ok");
        }

        private void ResponceToMessage(Updates message)
        {
            string comandKey = null;
            Message vkMessage = Message.FromJson(new VkResponse(message.Object));
            vkMessage.Text = vkMessage.Text.VkTrim();

            List<string> avatarCommands = AvatarCommandsController.AllCommands.сommands;
            List<string> avatarCommandsArgument = AvatarCommandsController.AllCommands.сommandsArgument;
            List<string> avatarCommandsTwoArgument = AvatarCommandsController.AllCommands.commandsTwoArgument;
            List<string> avatarCommandsStringArgument = AvatarCommandsController.AllCommands.commandsStringArgument;
            List<string> buttonResponses = AvatarCommandsController.AllCommands.buttonResponses;

            List<string> baseCommands = CommandsController.AllCommands.baseCommands;
            List<string> groupCommands = new();
            _dbContext.VkGroupsCommands.Where(p => p.GroupId == vkMessage.PeerId).ToList().ForEach(x => groupCommands.Add(x.Command));
            List<string> otherCommands = CommandsController.AllCommands.commands;
            List<string> otherCommandsArgument = CommandsController.AllCommands.commandsArgument;
            List<string> otherCommandsStringArgument = CommandsController.AllCommands.commandsStringArgument;                              
            try
            {
                //Время обработки запроса 1
                long startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();                

                string[] vkMessageWords = null;
                byte groupMention = 0;
                byte specialStart = 0;
               
                if (vkMessage.Text.ToLower().Replace("|*", "|@").Replace("],", "]").StartsWith("[club194159195|@hobobot] "))
                {
                    groupMention = 1;
                    vkMessage.Text = vkMessage.Text[vkMessage.Text.IndexOf(' ')..].VkTrim();
                }

                List<string> allCommands = new();
                allCommands.AddRange(baseCommands);
                allCommands.AddRange(groupCommands);

                allCommands.AddRange(otherCommandsArgument);
                allCommands.AddRange(otherCommandsStringArgument);

                allCommands.AddRange(avatarCommandsArgument);
                allCommands.AddRange(avatarCommandsTwoArgument);
                allCommands.AddRange(avatarCommandsStringArgument);

                char[] specialStarts = new char[4] { '!', ':', '\\', '/' };
                foreach (char start in specialStarts)
                    if (vkMessage.Text.StartsWith(start))
                    {
                        vkMessage.Text = vkMessage.Text[1..];
                        specialStart = 1;
                    }

                //Получаем ключ команды, у которой не может быть аргументов или нет аргумента.
                if (avatarCommands.Contains(vkMessage.Text.ToLower()) || otherCommands.Contains(vkMessage.Text.ToLower()) ||
                    buttonResponses.Contains(vkMessage.Text.ToLower()) || baseCommands.Contains(vkMessage.Text.ToLower()) ||
                    groupCommands.Contains(vkMessage.Text.ToLower()))
                {
                    comandKey = vkMessage.Text.ToLower();
                    vkMessage.Text = "";
                }
                else
                {
                    foreach (string command in allCommands)
                    {
                        //Получаем ключ команды, у которой есть аргументы.
                        //Прибавляем пустую строку для того, чтобы не вырывать часть слов. Например: Пас и Пасиба, без пробела прошли бы обе. 
                        if (vkMessage.Text.ToLower().StartsWith(command + " ") || vkMessage.Text.ToLower().StartsWith(command + "\n"))
                        {
                            comandKey = command;
                            //Вырезаем команду
                            vkMessage.Text = vkMessage.Text[command.Length..].VkTrim();
                            vkMessageWords = vkMessage.Text.Split(new char[] { ' ' });
                            //Вылавливаем команды с аргументом. 
                            if (baseCommands.Contains(command) || groupCommands.Contains(command) || avatarCommandsStringArgument.Contains(command) || otherCommandsStringArgument.Contains(command))
                            {
                                //Вылавливаем команды к пользователю с аргументом.                                    
                                if (vkMessageWords[0].StartsWith("[id") || vkMessageWords[0].StartsWith("[club"))
                                {
                                    if (vkMessageWords[0].Contains(@"\n"))
                                        vkMessageWords[0] = new Regex(@"\\n").Replace(vkMessageWords[0], " ", 1).VkTrim();
                                    else if (vkMessageWords.Length > 1 && vkMessageWords[1].StartsWith(@"\n"))
                                        vkMessageWords[1] = new Regex(@"\\n").Replace(vkMessageWords[1], "", 1);
                                    vkMessage.Text = VkString.GetVkString(vkMessageWords);
                                }
                                break;
                            }
                            //Вылавливаем команды строго с одним аргументом. 
                            if (avatarCommandsArgument.Contains(command) && vkMessageWords.Length == 1 && !vkMessageWords[0].Contains("\n") ||
                                otherCommandsArgument.Contains(command) && vkMessageWords.Length == 1 && !vkMessageWords[0].Contains("\n"))
                            {
                                break;
                            }
                            //Вылавливаем команды, у которых 1 или 2 аргумета. 
                            if (avatarCommandsTwoArgument.Contains(command) && vkMessageWords.Length <= 2 && !vkMessageWords[0].Contains("\n") ||
                                avatarCommandsTwoArgument.Contains(command) && vkMessageWords.Length == 2 && !vkMessageWords[0].Contains("\n") && !vkMessageWords[1].Contains("\n"))
                            {
                                break;
                            }
                            comandKey = null;
                            break;
                        }
                    }
                }

                if (comandKey != null)
                {
                    if (!vkMessage.FromId.ToString().Contains('-'))
                    {
                        if (CommandsController.AllCommands.allCommands.Contains(comandKey) || groupCommands.Contains(comandKey))
                        {
                            CommandsController avatar = new(_serviceProvider, comandKey, vkMessage, groupMention, specialStart);
                            _responce = avatar.GetResponceMessage();
                        }
                        else if (AvatarCommandsController.AllCommands.allCommands.Contains(comandKey))
                        {
                            AvatarCommandsController avatar = new(_serviceProvider, _webHostEnvironment, comandKey, vkMessage, groupMention);
                            _responce = avatar.GetAvatarResponceMessage();
                        }

                        if (_responce != null && _responce.Message != null)
                            _vkApi.Messages.Send(_responce);

                        //Время обработки запроса 2
                        {
                            long endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                            string timeTest = "Сервер: " + (endTime - startTime).ToString();
                            timeTest += "\nВсего: " + (endTime - ((DateTimeOffset)vkMessage.Date).ToUnixTimeMilliseconds()).ToString();
                            _vkApi.Messages.Send(new MessagesSendParams
                            {
                                RandomId = new DateTime().Millisecond,
                                PeerId = 2000000005,
                                Message = $"Сообщение:\n{Message.FromJson(new VkResponse(message.Object)).Text}\n\n{timeTest}"
                            });
                        }
                    }
                    else
                    {
                        VkNet.Model.Group group = _vkApi.Groups.GetById(new string[] { vkMessage.FromId.ToString().Replace("-", "") }, vkMessage.FromId.ToString().Replace("-", ""), null).FirstOrDefault();
                        _responce.Message = $"Я не люблю других ботов, пусть [club{vkMessage.FromId.ToString().Replace("-", "")}|{group.Name}] меня не трогает.";
                        _vkApi.Messages.Send(_responce);
                    }
                }
            }
            catch (Exception e)
            {
                {
                    _responce.Message = $"По команде или боту показалось, что была команда, произошла ошибка.😨\n" +
                                    $"Все явки, пароли и приметы ошибки были отправлены администрации, скоро она с ней разбереться.👮\n\n" +
                                    $"Спасибо гражданнам за выявление злостного бага, хорошего вам дня.😇\n\n" +
                                    $"P.S.\nБудьте осторожны с\n{Message.FromJson(new VkResponse(message.Object)).Text}";
                    _vkApi.Messages.Send(_responce);
                }//Отправить ошибку в беседу
                {
                    StackTrace trace = new(e, true);
                    string error = $"Сообщение:\n{Message.FromJson(new VkResponse(message.Object)).Text}\n\nВызвало ошибку:\n{e.Message}\n\nПо команде: {comandKey}";
                    foreach (var frame in trace.GetFrames())
                    {
                        if (frame.GetFileLineNumber() != 0)
                        {
                            error += $"\n\nФайл: {frame.GetFileName()}";
                            error += $"\nСтрока: {frame.GetFileLineNumber()}";
                            error += $"\nСтолбец: {frame.GetFileColumnNumber()}";
                            error += $"\nМетод: {frame.GetMethod()}";
                        }
                    }
                    if (e.InnerException != null)
                    {
                        error += $"\n\nПродолжение ошибки:\n{e.InnerException.Message}";
                    }
                    _responce.RandomId = new DateTime().Millisecond;
                    _responce.PeerId = 2000000005;
                    _responce.Message = error;
                    _vkApi.Messages.Send(_responce);
                }//Отправить ошибку в вк разработчику
            }                   
        }
    }
}