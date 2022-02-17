using SimpleMessengerApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace SimpleMessengerApi
{
    public class Messenger
    {
        private static string token;
        private static TelegramBotClient client;
        private static SimpleMessengerDbEntities db = new SimpleMessengerDbEntities();

        public Messenger()
        {
            token = File.ReadAllText(@"D:\\SimpleMessengerToken.txt");
            client = new TelegramBotClient(token);
            client.OnMessage += Client_OnMessage;
            client.StartReceiving();
        }

        public static void Send(long id, string text)
        {
            client.SendTextMessageAsync(id, text);
            return;
        }

        private void Client_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            long id = e.Message.Chat.Id;
            User user;
            try
            {
                user = db.User.FirstOrDefault(u => u.Id == id);
                if (user == null)
                {
                    db.User.Add(new User() { Id = id });
                    db.SaveChanges();
                    user = db.User.FirstOrDefault(u => u.Id == id);
                }
            }
            catch (Exception ex)
            {
                client.SendTextMessageAsync(id, "Ошибка в работе сервиса. Попробуйте позже.",
                    parseMode: ParseMode.Html);
                return;
            }
            if (e.Message.Type !=  MessageType.Text)
            {
                client.SendTextMessageAsync(id, "Отправка не текстовых сообщений пока не возможна");
                return;
            }
            string text = e.Message.Text;
            if (text == "/start")
            {
                client.SendTextMessageAsync(id, "Добро пожаловать в сервис <b>SimpleMessenger</b>\n\n" +
                    "Чтобы привязать устройство к аккаунту введите номер устройства\n\n" +
                    "Чтобы отправить сообщение введите текст в формате:\n" +
                    "<i>НомерУстройства Сообщение</i>\n\n" +
                    "Например:\n" +
                    "<i>1 Привет!</i>\n\n" +
                    "Чтобы удалить свои сообщения введите текст в формате:\n" +
                    "<i>НомерУстройства</i> clear\n\n" +
                    "Например:\n" +
                    "<i>1 clear</i>\n\n" +
                    "Для справки введите: /help", parseMode: ParseMode.Html);
                return;
            }
            else if (text == "/help")
            {
                client.SendTextMessageAsync(id,
                    "Чтобы привязать устройство к аккаунту введите номер устройства\n\n" +
                    "Чтобы отправить сообщение введите тескт в формате:\n" +
                    "<i>НомерУстройства Сообщение</i>\n\n" +
                    "Например:\n" +
                    "<i>1 Привет!</i>\n\n" +
                    "Чтобы удалить свои сообщения введите текст в формате:\n" +
                    "<i>НомерУстройства</i> clear\n\n" +
                    "Например:\n" +
                    "<i>1 clear</i>\n\n", parseMode: ParseMode.Html);
                return;
            }
            string[] splittedText = text.Split(' ');
            int i;
            if (!int.TryParse(splittedText[0], out i))
            {
                client.SendTextMessageAsync(id, "Cообщение должно начинаться с номера устройства\n\n" +
                    "Например:\n" +
                    "<i>1 Привет!</i>", parseMode: ParseMode.Html);
                return;
            }
            try
            {
                Device device = db.Device.FirstOrDefault(d => d.Id == i);
                if (device == null)
                {
                    client.SendTextMessageAsync(id, "Устройство с таким номером не найдено\n\n" +
                        "Возможно вам поможет /help?", parseMode: ParseMode.Html);
                    return;
                }
                if (splittedText.Length == 1)
                {
                    if(!device.User.Contains(user))
                    {
                        if (device.User.Count < device.UsersCount)
                        {
                            device.User.Add(user);
                            db.SaveChanges();
                            client.SendTextMessageAsync(id, $"Теперь ваш аккаунт привязан к устройству {i}\n" +
                                $"Теперь вы и владелец устройсва можете отправлять друг другу сообщения\n\n" +
                                "Чтобы отвязать аккаунт от устройства, снова введите его номер", parseMode: ParseMode.Html);
                        }
                        else
                        {
                            client.SendTextMessageAsync(id, $"Невозможно привязать аккаунт к устройству," +
                                $" так как к нему уже привязано максимальное количество аккаунтов", parseMode: ParseMode.Html);
                        }
                    }
                    else
                    {
                        device.User.Remove(user);
                        db.SaveChanges();
                        client.SendTextMessageAsync(id, $"Вы отвязали свой аккаунт от устройства {i}\n" +
                            $"Теперь его владелец не сможет отправлять вам сообщения\n\n" +
                            $"Чтобы привязать аккаунт к устройству, введите его номер", parseMode: ParseMode.Html);
                    }
                    return;
                }
                if (!device.User.Contains(user))
                {
                    client.SendTextMessageAsync(id, $"Вы не можете отправлять сообщения на это устройство," +
                        $" так как не привязаны к нему\n\n" +
                            $"Чтобы привязать аккаунт к устройству введите его номер", parseMode: ParseMode.Html);
                    return;
                }
                if (splittedText.Length == 2)
                {
                    if (splittedText[1].ToLower() == "clear")
                    {
                        db.Message.RemoveRange(db.Message.Where(m => m.UserId == id && m.DeviceId == i));
                        db.SaveChanges();
                        client.SendTextMessageAsync(id, $"Переписка с устройством №{i} удалена"
                        , parseMode: ParseMode.Html);
                        return;
                    }
                }
                db.Message.Add(new Message { DeviceId = i, Text = String.Join(" ", splittedText.Skip(1)), UserId = id });
                db.SaveChanges();
                client.SendTextMessageAsync(id, $"Сообщение успешно отправлено на устройство №{i}",
                    parseMode: ParseMode.Html);
            }
            catch (Exception ex)
            {
                client.SendTextMessageAsync(id, "Ошибка в работе сервиса. Попробуйте позже.",
                    parseMode: ParseMode.Html);
                return;
            }
        }
    }
}