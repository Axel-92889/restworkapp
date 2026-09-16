using System;

namespace RestaurantWorkApp.Models
{
    public static class CurrentUser
    {
        public static int Id { get; private set; }
        public static string Login { get; private set; } = string.Empty;
        public static string FullName { get; private set; } = string.Empty;  // FIO_R / FIO_C
        public static int RoleId { get; private set; }                        // id_Rule
        public static string RoleName { get; private set; } = string.Empty;   // название роли
        public static int WorkerId { get; private set; }                      // id_Rabotnik (0 у клиента)
        public static int ClientId { get; private set; }                      // id_Client (0 у работника)

        public static void SetUserData(
            int id,
            string login,
            string fullName,
            int roleId,
            string roleName,
            int workerId = 0,
            int clientId = 0)
        {
            Id = id;
            Login = login;
            FullName = fullName;
            RoleId = roleId;
            RoleName = roleName;
            WorkerId = workerId;
            ClientId = clientId;
        }

        public static void Clear()
        {
            Id = 0;
            Login = string.Empty;
            FullName = string.Empty;
            RoleId = 0;
            RoleName = string.Empty;
            WorkerId = 0;
            ClientId = 0;
        }

        public static bool IsAuthenticated => !string.IsNullOrEmpty(Login) && RoleId > 0;

        public static bool IsAdmin => RoleId == 1;
        public static bool IsWorker => RoleId >= 1 && RoleId <= 4;   // персонал = роли 1–4
        public static bool IsClient => RoleId == 5;
        public static bool IsStaff => IsWorker;                     // синоним для читаемости
    }
}