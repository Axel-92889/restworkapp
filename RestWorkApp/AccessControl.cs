using System.Collections.Generic;
using System.Linq;
using RestaurantWorkApp.Models;

public static class AccessControl
{
    // Своего состояния НЕТ: роль всегда читается из CurrentUser
    private static int RoleId => CurrentUser.RoleId;

    // Оставлено для совместимости; делать ничего не нужно —
    // роль устанавливается через CurrentUser.SetUserData(...)
    public static void SetCurrentRole(int roleId) { }

    public static int GetCurrentRole() => RoleId;

    /// Клиент — роль 5 (или 0, если вход не выполнен)
    public static bool IsClient() => RoleId == 5 || RoleId == 0;

    /// Доступ в админ-панель: роли 1–4
    public static bool HasAdminAccess() => RoleId >= 1 && RoleId <= 4;

    /// Список доступных разделов для текущей роли
    public static List<string> GetAvailableSections()
    {
        var sections = new List<string>();
        switch (RoleId)
        {
            case 1:
                sections.Add("all");
                break;
            case 2: // Менеджер
                sections.AddRange(new[] { "rabotnik", "dolh", "client", "client_table", "zone",
                    "dish", "category", "ingredients", "reserv", "zakaz", "promotions", "feedback" });
                break;
            case 3: // Кассир
                sections.AddRange(new[] { "client", "client_table", "zakaz",
                    "zakaz_items", "checkzakaz", "delivery" });
                break;
            case 4: // Работник
                sections.AddRange(new[] { "dish", "zakaz_items", "delivery", "dish_ingredients" });
                break;
        }
        return sections;
    }

    /// Доступ к конкретному разделу
    public static bool HasSectionAccess(string sectionName)
    {
        if (!HasAdminAccess()) return false;
        var available = GetAvailableSections();
        if (available.Contains("all")) return true;
        return available.Contains(sectionName.ToLower());
    }

    /// Доступ к операции с таблицей (для DataGrid и т.п.)
    public static bool CanDoOperation(string tableName)
    {
        var tableToSection = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            ["Rule"] = "rule",
            ["Rabotnik"] = "rabotnik",
            ["Dolh"] = "dolh",
            ["Client"] = "client",
            ["Dish"] = "dish",
            ["Zakaz"] = "zakaz",
            ["Reserv"] = "reserv",
            ["Category"] = "category",
            ["Ingredients"] = "ingredients",
            ["Delivery"] = "delivery",
            ["Promotions"] = "promotions",
            ["Feedback"] = "feedback",
            ["CheckZakaz"] = "checkzakaz",
            ["Check"] = "checkzakaz",
            ["Zone"] = "zone",
            ["Client_table"] = "client_table",
            ["Client_Table"] = "client_table",
            ["Dish_Ingredients"] = "dish_ingredients",
            ["Dish_ingredients"] = "dish_ingredients",
            ["Zakaz_Items"] = "zakaz_items",
            ["ZakazItems"] = "zakaz_items",
        };
        if (tableToSection.TryGetValue(tableName, out var section))
            return HasSectionAccess(section);
        return false; // неизвестная таблица — доступа нет
    }
}