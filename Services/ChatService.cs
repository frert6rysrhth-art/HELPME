using BelovskayaMonitoring.Data;
using BelovskayaMonitoring.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BelovskayaMonitoring.Services
{
    public class ChatService
    {
        private readonly ApplicationDbContext _db;

        public ChatService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<ChatPreview>> GetUserChatPreviewsAsync(string userId)
        {
            var chatIds = await _db.ChatParticipants
                .Where(cp => cp.UserId == userId)
                .Select(cp => cp.ChatId)
                .ToListAsync();

            var pinnedChatIds = await _db.UserChatPins
                .Where(p => p.UserId == userId)
                .Select(p => p.ChatId)
                .ToListAsync();

            var previews = new List<ChatPreview>();

            foreach (var chatId in chatIds)
            {
                var chat = await _db.Chats.FindAsync(chatId);
                if (chat == null) continue;

                var lastMessage = await _db.Messages
                    .Where(m => m.ChatId == chatId)
                    .Include(m => m.Sender)
                    .OrderByDescending(m => m.Timestamp)
                    .FirstOrDefaultAsync();

                previews.Add(new ChatPreview
                {
                    ChatId = chat.Id,
                    ChatName = chat.Name,
                    CreatedAt = chat.CreatedAt,
                    IsSystem = chat.IsSystem,
                    IsPinned = pinnedChatIds.Contains(chat.Id),
                    LastSenderName = (lastMessage?.SenderId == null) ? "Система" :
                        (lastMessage.Sender != null ? $"{lastMessage.Sender.FirstName} {lastMessage.Sender.LastName}".Trim() : "Неизвестный"),
                    LastSenderEmail = lastMessage?.Sender?.Email ?? "",
                    LastMessageText = lastMessage?.Text,
                    LastMessageTime = lastMessage?.Timestamp
                });
            }

            return previews
                .OrderByDescending(p => p.IsSystem)
                .ThenByDescending(p => p.IsPinned)
                .ThenByDescending(p => p.LastMessageTime ?? p.CreatedAt)
                .ToList();
        }

        public async Task<List<Chat>> GetUserChats(string userId)
        {
            return await _db.ChatParticipants
                .Where(cp => cp.UserId == userId)
                .Include(cp => cp.Chat)
                .Select(cp => cp.Chat)
                .ToListAsync();
        }

        public async Task<List<Message>> GetChatMessages(int chatId, int count = 50)
        {
            return await _db.Messages
                .Where(m => m.ChatId == chatId)
                .Include(m => m.Sender)
                .OrderByDescending(m => m.Timestamp)
                .Take(count)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        public async Task<bool> IsParticipant(int chatId, string userId)
        {
            return await _db.ChatParticipants
                .AnyAsync(cp => cp.ChatId == chatId && cp.UserId == userId);
        }

        public async Task<Chat> CreateChat(string name, List<string> userIds)
        {
            var chat = new Chat { Name = name };
            _db.Chats.Add(chat);
            await _db.SaveChangesAsync();

            foreach (var userId in userIds)
            {
                _db.ChatParticipants.Add(new ChatParticipant { ChatId = chat.Id, UserId = userId });
            }
            await _db.SaveChangesAsync();
            return chat;
        }

        public async Task<(bool Success, string Error)> LeaveChat(int chatId, string userId)
        {
            var chat = await _db.Chats.FindAsync(chatId);
            if (chat == null) return (false, "Чат не найден.");
            if (chat.IsSystem) return (false, "Вы не можете покинуть этот чат.");

            var participant = await _db.ChatParticipants
                .FirstOrDefaultAsync(cp => cp.ChatId == chatId && cp.UserId == userId);
            if (participant != null)
            {
                _db.ChatParticipants.Remove(participant);
                await _db.SaveChangesAsync();

                bool hasParticipants = await _db.ChatParticipants.AnyAsync(cp => cp.ChatId == chatId);
                if (!hasParticipants)
                {
                    var messages = _db.Messages.Where(m => m.ChatId == chatId);
                    _db.Messages.RemoveRange(messages);
                    _db.Chats.Remove(chat);
                    await _db.SaveChangesAsync();
                }
                return (true, "");
            }
            return (false, "Участник не найден.");
        }

        public async Task<string?> ExportChatAsTextAsync(int chatId, string userId)
        {
            var isParticipant = await IsParticipant(chatId, userId);
            if (!isParticipant) return null;

            var chat = await _db.Chats.FindAsync(chatId);
            if (chat == null) return null;

            var messages = await _db.Messages
                .Where(m => m.ChatId == chatId)
                .Include(m => m.Sender)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine($"Чат: {chat.Name}");
            sb.AppendLine($"Экспортировано: {DateTime.UtcNow.ToLocalTime():dd.MM.yyyy HH:mm}");
            sb.AppendLine(new string('-', 40));

            foreach (var msg in messages)
            {
                var senderName = msg.SenderId == null
                    ? "Система"
                    : (msg.Sender != null ? $"{msg.Sender.FirstName} {msg.Sender.LastName}".Trim() : "Неизвестный");
                sb.AppendLine($"[{msg.Timestamp.ToLocalTime():HH:mm}] {senderName}: {msg.Text}");
            }

            return sb.ToString();
        }

        public async Task<bool> TogglePinChat(int chatId, string userId)
        {
            var isParticipant = await IsParticipant(chatId, userId);
            if (!isParticipant) return false;

            var pin = await _db.UserChatPins
                .FirstOrDefaultAsync(p => p.ChatId == chatId && p.UserId == userId);
            if (pin != null)
            {
                _db.UserChatPins.Remove(pin);
            }
            else
            {
                _db.UserChatPins.Add(new UserChatPin { ChatId = chatId, UserId = userId });
            }
            await _db.SaveChangesAsync();
            return true;
        }

        // Очистить историю сообщений чата
        public async Task<(bool Success, string Error)> ClearChatHistoryAsync(int chatId)
        {
            var chat = await _db.Chats.FindAsync(chatId);
            if (chat == null) return (false, "Чат не найден.");

            var messages = _db.Messages.Where(m => m.ChatId == chatId);
            _db.Messages.RemoveRange(messages);
            await _db.SaveChangesAsync();

            return (true, "");
        }

        // Полностью удалить чат (только не-системный)
        public async Task<(bool Success, string Error)> DeleteChatAsync(int chatId)
        {
            var chat = await _db.Chats.FindAsync(chatId);
            if (chat == null) return (false, "Чат не найден.");
            if (chat.IsSystem) return (false, "Нельзя удалить системный чат.");

            // Удаляем закрепления
            var pins = _db.UserChatPins.Where(p => p.ChatId == chatId);
            _db.UserChatPins.RemoveRange(pins);

            // Удаляем сообщения
            var messages = _db.Messages.Where(m => m.ChatId == chatId);
            _db.Messages.RemoveRange(messages);

            // Удаляем участников
            var participants = _db.ChatParticipants.Where(cp => cp.ChatId == chatId);
            _db.ChatParticipants.RemoveRange(participants);

            // Удаляем сам чат
            _db.Chats.Remove(chat);

            await _db.SaveChangesAsync();
            return (true, "");
        }
    }
}