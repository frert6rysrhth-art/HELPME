using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BelovskayaMonitoring.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BelovskayaMonitoring.Data
{
    public static class SeedData
    {
        public static async Task Initialize(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            string[] roleNames = { "Administrator", "User", "Guest" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            string adminEmail = "admin@belovskaya.ru";
            string adminPassword = "Admin123!";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "Администратор",
                    LastName = "",
                    AvatarColor = GetColorFromEmail(adminEmail) // новый цвет
                };
                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(adminUser, "Administrator");
            }
            else
            {
                if (!await userManager.IsInRoleAsync(adminUser, "Administrator"))
                    await userManager.AddToRoleAsync(adminUser, "Administrator");
                // Обновим цвет, если ещё не задан
                if (string.IsNullOrEmpty(adminUser.AvatarColor))
                {
                    adminUser.AvatarColor = GetColorFromEmail(adminUser.Email);
                    await userManager.UpdateAsync(adminUser);
                }
            }

            // Системный чат "Система"
            if (!context.Chats.Any(c => c.Name == "Система"))
            {
                // Удалим старый чат с именем "Дежурная смена", если остался (на всякий случай)
                var oldChat = context.Chats.FirstOrDefault(c => c.Name == "Дежурная смена");
                if (oldChat != null)
                {
                    context.Chats.Remove(oldChat);
                    await context.SaveChangesAsync();
                }

                var chat = new Chat { Name = "Система", IsSystem = true };
                context.Chats.Add(chat);
                await context.SaveChangesAsync();

                var adminId = adminUser?.Id ?? (await userManager.FindByEmailAsync(adminEmail))?.Id;
                if (adminId != null)
                {
                    context.ChatParticipants.Add(new ChatParticipant { ChatId = chat.Id, UserId = adminId });
                    await context.SaveChangesAsync();
                }
            }

            // Системный чат "Объявления"
            if (!context.Chats.Any(c => c.Name == "Объявления"))
            {
                var chat = new Chat { Name = "Объявления", IsSystem = true };
                context.Chats.Add(chat);
                await context.SaveChangesAsync();

                var adminId = adminUser?.Id ?? (await userManager.FindByEmailAsync(adminEmail))?.Id;
                if (adminId != null)
                {
                    context.ChatParticipants.Add(new ChatParticipant { ChatId = chat.Id, UserId = adminId });
                    await context.SaveChangesAsync();
                }
            }

            // Тестовые устройства
            if (!context.Devices.Any())
            {
                var devices = new List<Device>
                {
                    new Device { Name = "Коммутатор ЦТЩ-1", IPAddress = "192.168.1.10", Status = Models.DeviceStatus.Online, LastChecked = DateTime.UtcNow },
                    new Device { Name = "Маршрутизатор РЗА-3", IPAddress = "192.168.1.20", Status = Models.DeviceStatus.Online, LastChecked = DateTime.UtcNow },
                    new Device { Name = "Сервер АСУ ТП-2", IPAddress = "192.168.1.30", Status = Models.DeviceStatus.Online, LastChecked = DateTime.UtcNow },
                    new Device { Name = "Коммутатор РЩУ-4", IPAddress = "192.168.2.10", Status = Models.DeviceStatus.Online, LastChecked = DateTime.UtcNow }
                };
                context.Devices.AddRange(devices);
                await context.SaveChangesAsync();
            }
        }

        // Генерация цвета на основе email
        private static string GetColorFromEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return "#3366CC";
            int hash = email.GetHashCode();
            int r = (hash >> 16 & 0xFF) % 156 + 100; // яркие, но не слишком светлые
            int g = (hash >> 8 & 0xFF) % 156 + 100;
            int b = (hash & 0xFF) % 156 + 100;
            return $"#{r:X2}{g:X2}{b:X2}";
        }
    }
}