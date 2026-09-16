-- СОЗДАНИЕ БД
CREATE DATABASE IF NOT EXISTS Restaurant_db;
USE Restaurant_db;
-- ============================================
-- СОЗДАНИЕ ТАБЛИЦ С ON DELETE CASCADE
-- ============================================
-- 1. Таблица Rule (роли) - родительская
CREATE TABLE Rule(
id_Rule INT PRIMARY KEY AUTO_INCREMENT,
Name_R VARCHAR(20)
);
-- 2. Таблица Dolh (должности)
CREATE TABLE Dolh(
id_Dolh INT PRIMARY KEY AUTO_INCREMENT,
Name_Dolh VARCHAR(20) NOT NULL,
id_rule INT,
FOREIGN KEY (id_rule) REFERENCES Rule(id_Rule)
ON DELETE CASCADE ON UPDATE CASCADE
);
-- 3. Таблица Client (клиенты)
CREATE TABLE Client(
id_Client INT PRIMARY KEY AUTO_INCREMENT,
FIO_C VARCHAR(45) NOT NULL,
Tel_C VARCHAR(15) NOT NULL,
Mail VARCHAR(45),
Data_Reg DATE,
login VARCHAR(255),
password VARBINARY(255),
id_rule INT,
FOREIGN KEY (id_rule) REFERENCES Rule(id_Rule)
ON DELETE CASCADE ON UPDATE CASCADE
);
-- 4. Таблица Zone (зоны)
CREATE TABLE Zone(
id_Zone INT PRIMARY KEY AUTO_INCREMENT,
Name_Zone VARCHAR(30) NOT NULL
);
-- 5. Таблица Client_Table (столы)
CREATE TABLE Client_Table(
id_Table INT PRIMARY KEY AUTO_INCREMENT,
Number_T INT(25) NOT NULL,
Seats INT(10) NOT NULL,
Status_T VARCHAR(1) NOT NULL,
id_zone INT NOT NULL,
FOREIGN KEY (id_zone) REFERENCES Zone(id_Zone)
ON DELETE CASCADE ON UPDATE CASCADE
);
-- 6. Таблица Rabotnik (сотрудники)
CREATE TABLE Rabotnik(
id_Rabotnik INT PRIMARY KEY AUTO_INCREMENT,
FIO_R VARCHAR(45) NOT NULL,
Tel_R VARCHAR(15) NOT NULL,
login VARCHAR(255),
password VARBINARY(255),
id_dolh INT,
id_rule INT,
FOREIGN KEY (id_dolh) REFERENCES Dolh(id_Dolh)
ON DELETE CASCADE ON UPDATE CASCADE,
FOREIGN KEY (id_rule) REFERENCES Rule(id_Rule)
ON DELETE CASCADE ON UPDATE CASCADE
);
-- 7. Таблица Ingredients (ингредиенты)
CREATE TABLE Ingredients(
id_Ingredients INT PRIMARY KEY AUTO_INCREMENT,
Name_I VARCHAR(20) NOT NULL
);
-- 8. Таблица Category (категории блюд)
CREATE TABLE Category(
id_Category INT PRIMARY KEY AUTO_INCREMENT,
Name_C VARCHAR(20) NOT NULL
);
-- 9. Таблица Promotions (акции)
CREATE TABLE Promotions(
id_Promotion INT PRIMARY KEY AUTO_INCREMENT,
Name_P VARCHAR(100) NOT NULL,
Description TEXT,
Discount_Percent DECIMAL(5,2) NOT NULL,
Start_Date DATE NOT NULL,
End_Date DATE NOT NULL,
Is_Active BOOLEAN DEFAULT TRUE
);
-- 10. Таблица Dish (блюда)
CREATE TABLE Dish(
id_Dish INT PRIMARY KEY AUTO_INCREMENT,
Name_Dish VARCHAR(50) NOT NULL,
Price_D DECIMAL(10, 2) NOT NULL,
Status_D VARCHAR(1) DEFAULT 'A',
Image_URL VARCHAR(255),
id_category INT NOT NULL,
FOREIGN KEY (id_category) REFERENCES Category(id_Category)
ON DELETE CASCADE ON UPDATE CASCADE
);
-- 11. Таблица Dish_Ingredients (ингредиенты блюд)
CREATE TABLE Dish_Ingredients(
id_Dish_Ingredients INT PRIMARY KEY AUTO_INCREMENT,
quantity DECIMAL(10, 2),
unit VARCHAR(20),
id_ingredients INT NOT NULL,
id_dish INT NOT NULL,
FOREIGN KEY (id_ingredients) REFERENCES Ingredients(id_Ingredients)
ON DELETE CASCADE ON UPDATE CASCADE,
FOREIGN KEY (id_dish) REFERENCES Dish(id_Dish)
ON DELETE CASCADE ON UPDATE CASCADE
);
-- 12. Таблица Zakaz (заказы)
CREATE TABLE Zakaz(
id_Zakaz INT PRIMARY KEY AUTO_INCREMENT,
Price_Z DECIMAL(10, 2) NOT NULL,
Data_Z DATETIME NOT NULL,
Start_Time DATETIME,
End_Time DATETIME,
Status_Z VARCHAR(1) NOT NULL DEFAULT 'N',
id_promotion INT,
id_client INT,
id_rabotnik INT,
FOREIGN KEY (id_promotion) REFERENCES Promotions(id_Promotion)
ON DELETE SET NULL ON UPDATE CASCADE,
FOREIGN KEY (id_client) REFERENCES Client(id_Client)
ON DELETE CASCADE ON UPDATE CASCADE,
FOREIGN KEY (id_rabotnik) REFERENCES Rabotnik(id_Rabotnik)
ON DELETE SET NULL ON UPDATE CASCADE
);
-- 13. Таблица Zakaz_Items (позиции заказа)
CREATE TABLE Zakaz_Items(
id_Zakaz_Items INT PRIMARY KEY AUTO_INCREMENT,
quantity INT NOT NULL,
price_at_time DECIMAL(10, 2),
id_dish INT NOT NULL,
id_zakaz INT NOT NULL,
FOREIGN KEY (id_dish) REFERENCES Dish(id_Dish)
ON DELETE CASCADE ON UPDATE CASCADE,
FOREIGN KEY (id_zakaz) REFERENCES Zakaz(id_Zakaz)
ON DELETE CASCADE ON UPDATE CASCADE
);
-- 14. Таблица Reserv (бронирования)
CREATE TABLE Reserv(
id_Reserv INT PRIMARY KEY AUTO_INCREMENT,
Data_R DATE NOT NULL,
Number_Guests INT(100) NOT NULL,
Status_R VARCHAR(1) NOT NULL,
id_client INT,
id_table INT,
FOREIGN KEY (id_client) REFERENCES Client(id_Client)
ON DELETE CASCADE ON UPDATE CASCADE,
FOREIGN KEY (id_table) REFERENCES Client_Table(id_Table)
ON DELETE SET NULL ON UPDATE CASCADE
);
-- 15. Таблица Delivery (доставка)
CREATE TABLE Delivery(
id_Delivery INT PRIMARY KEY AUTO_INCREMENT,
Address VARCHAR(255) NOT NULL,
Delivery_Time DATETIME,
Status_Delivery VARCHAR(20) DEFAULT 'PENDING',
id_zakaz INT NOT NULL,
id_rabotnik INT,
FOREIGN KEY (id_zakaz) REFERENCES Zakaz(id_Zakaz)
ON DELETE CASCADE ON UPDATE CASCADE,
FOREIGN KEY (id_rabotnik) REFERENCES Rabotnik(id_Rabotnik)
ON DELETE SET NULL ON UPDATE CASCADE
);
-- 16. Таблица CheckZakaz (чеки заказов)
CREATE TABLE CheckZakaz(
id_Check INT PRIMARY KEY AUTO_INCREMENT,
Time_Check DATETIME NOT NULL,
Dishes_Info TEXT NOT NULL,
Total_Amount DECIMAL(10, 2) NOT NULL,
Payment VARCHAR(20) NOT NULL,
id_zakaz INT NOT NULL,
FOREIGN KEY (id_zakaz) REFERENCES Zakaz(id_Zakaz)
ON DELETE CASCADE ON UPDATE CASCADE
);
-- 17. Таблица Feedback (отзывы)
CREATE TABLE Feedback(
id_Feedback INT PRIMARY KEY AUTO_INCREMENT,
Rating INT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
Comment TEXT,
Date_Feedback DATETIME DEFAULT CURRENT_TIMESTAMP,
id_client INT NOT NULL,
id_zakaz INT,
FOREIGN KEY (id_client) REFERENCES Client(id_Client)
ON DELETE CASCADE ON UPDATE CASCADE,
FOREIGN KEY (id_zakaz) REFERENCES Zakaz(id_Zakaz)
ON DELETE SET NULL ON UPDATE CASCADE
);
-- ============================================
-- ТРИГГЕРЫ
-- ============================================
DELIMITER //
-- Триггер для обновления цены заказа
CREATE TRIGGER update_Zakaz
AFTER INSERT ON Zakaz_Items
FOR EACH ROW
BEGIN
UPDATE Zakaz
SET Price_Z = (
SELECT SUM(quantity * price_at_time)
FROM Zakaz_Items
WHERE id_zakaz = NEW.id_zakaz
)
WHERE id_Zakaz = NEW.id_zakaz;
END//
-- Триггер для автоматического обновления Dishes_Info в CheckZakaz
CREATE TRIGGER update_check_dishes_info
AFTER INSERT ON Zakaz_Items
FOR EACH ROW
BEGIN
DECLARE v_dishes_info TEXT;
DECLARE v_check_exists INT DEFAULT 0;
SELECT COUNT(*) INTO v_check_exists 
FROM CheckZakaz 
WHERE id_zakaz = NEW.id_zakaz;

SELECT GROUP_CONCAT(
    CONCAT(d.Name_Dish, ' (', zi.quantity, 'x', zi.price_at_time, ') = ', 
           zi.quantity * zi.price_at_time, '₽') 
    SEPARATOR ', '
) INTO v_dishes_info
FROM Zakaz_Items zi
JOIN Dish d ON zi.id_dish = d.id_Dish
WHERE zi.id_zakaz = NEW.id_zakaz;

IF v_check_exists > 0 THEN
    UPDATE CheckZakaz 
    SET Dishes_Info = v_dishes_info,
        Total_Amount = (SELECT Price_Z FROM Zakaz WHERE id_Zakaz = NEW.id_zakaz)
    WHERE id_zakaz = NEW.id_zakaz;
ELSE
    INSERT INTO CheckZakaz (Time_Check, Dishes_Info, Total_Amount, Payment, id_zakaz)
    SELECT 
        Data_Z,
        v_dishes_info,
        Price_Z,
        'NOT_SELECTED',
        id_Zakaz
    FROM Zakaz
    WHERE id_Zakaz = NEW.id_zakaz;
END IF;
END//
DELIMITER ;
-- ============================================
-- ЗАПОЛНЕНИЕ ТАБЛИЦ ДАННЫМИ
-- ============================================
INSERT INTO Rule (Name_R) VALUES
('Администратор'),
('Менеджер'),
('Кассир'),
('Работник'),
('Клиент');
INSERT INTO Dolh (Name_Dolh, id_rule) VALUES
('Администратор', 1),
('Менеджер зала', 2),
('Кассир', 3),
('Повар', 4),
('Курьер', 4);
INSERT INTO Client (FIO_C, Tel_C, Mail, Data_Reg, login, password, id_rule) VALUES
('Иванов Петр Сидорович', '79151234567', 'ivanov@mail.ru', '2024-01-15', 'client1', AES_ENCRYPT('client123', 'nfkuser8w4'), 5),
('Смирнова Анна Владимировна', '79159876543', 'smirnova@mail.ru', '2024-02-20', 'client2', AES_ENCRYPT('client456', 'nfkuser8w4'), 5),
('Уткин Дмитрий Валерьевич', '79151112233', 'utkin@mail.ru', '2024-03-10', 'client3', AES_ENCRYPT('client789', 'nfkuser8w4'), 5),
('Петрова Мария Сергеевна', '79154445566', 'petrova@mail.ru', '2024-01-05', 'client4', AES_ENCRYPT('client111', 'nfkuser8w4'), 5),
('Сидоров Алексей Петрович', '79157778899', 'sidorov@mail.ru', '2024-04-01', 'client5', AES_ENCRYPT('client222', 'nfkuser8w4'), 5);
INSERT INTO Rabotnik (FIO_R, Tel_R, login, password, id_dolh, id_rule) VALUES
('Питер Паркер', '79150001122', 'admin', AES_ENCRYPT('admin123', 'nfkuser8w4'), 1, 1),
('Ковалева Елена Викторовна', '79150003344', 'manager1', AES_ENCRYPT('mng123', 'nfkuser8w4'), 2, 2),
('Николаев Сергей Олегович', '79150005566', 'cassir1', AES_ENCRYPT('csr123', 'nfkuser8w4'), 3, 3),
('Орлова Ирина Дмитриевна', '79150007788', 'worker1', AES_ENCRYPT('worker123', 'nfkuser8w4'), 4, 4),
('Федоров Артем Ильич', '79150009900', 'worker2', AES_ENCRYPT('worker456', 'nfkuser8w4'), 4, 4),
('Соколов Иван Петрович', '79150011122', 'courier1', AES_ENCRYPT('courier123', 'nfkuser8w4'), 4, 4);
INSERT INTO Category (Name_C) VALUES
('Закуски'),
('Первое'),
('Десерты'),
('Напитки'),
('Салаты'),
('Второе'),
('Пицца'),
('Суши'),
('Бургеры');
INSERT INTO Promotions (Name_P, Description, Discount_Percent, Start_Date, End_Date, Is_Active) VALUES
('Летняя скидка', 'Скидка на все блюда в летний период', 10.00, '2024-06-01', '2024-08-31', TRUE),
('Счастливые часы', 'Скидка 20% на напитки с 14:00 до 17:00', 20.00, '2024-05-01', '2024-12-31', TRUE),
('Первая покупка', 'Скидка для новых клиентов', 15.00, '2024-01-01', '2024-12-31', TRUE),
('День рождения', 'Специальная скидка в день рождения', 25.00, '2024-01-01', '2024-12-31', TRUE);
INSERT INTO Ingredients (Name_I) VALUES
('Куриное филе'),
('Говядина'),
('Лосось'),
('Овощи'),
('Сыр пармезан'),
('Сливочное масло'),
('Спагетти'),
('Томатный соус'),
('Шоколад'),
('Ванильное мороженое'),
('Фрукты'),
('Тесто для пиццы'),
('Сыр моцарелла'),
('Соус для пиццы'),
('Бекон'),
('Грибы'),
('Помидоры'),
('Лук'),
('Картофель'),
('Сметана'),
('Сливочный сыр'),
('Чайные листья'),
('Рис для суши');
INSERT INTO Dish (Name_Dish, Price_D, Status_D, Image_URL, id_category) VALUES
('Цезарь с курицей', 450.00, 'A', 'img/cesar.jpg', 5),
('Стейк из говядины', 1200.00, 'A', 'img/steak.jpg', 6),
('Лосось на гриле', 950.00, 'A', 'img/losos.jpg', 6),
('Спагетти Карбонара', 580.00, 'A', 'img/spaghetti.jpg', 6),
('Брускетта с томатами', 320.00, 'A', 'img/brusketta.jpg', 1),
('Шоколадный фондан', 350.00, 'A', 'img/fondan.jpg', 3),
('Мороженое пломбир', 200.00, 'A', 'img/icecream.jpg', 3),
('Кофе латте', 180.00, 'A', 'img/latte.jpg', 4),
('Сок апельсиновый', 150.00, 'A', 'img/juice.jpg', 4),
('Пицца Маргарита', 450.00, 'A', 'img/margarita.jpg', 7),
('Пицца Пепперони', 550.00, 'A', 'img/pepperoni.jpg', 7),
('Чизбургер', 380.00, 'A', 'img/cheeseburger.jpg', 9),
('Ролл Калифорния', 420.00, 'A', 'img/california.jpg', 8),
('Суп Том Ям', 380.00, 'A', 'img/tomyam.jpg', 2),
('Крокеты', 450.00, 'A', 'img/croquettes.jpg', 1),
('Тарталетки', 300.00, 'A', 'img/tartalettes.jpg', 1),
('Борщ', 350.00, 'A', 'img/borsch.jpg', 2),
('Солянка', 380.00, 'A', 'img/solyanka.jpg', 2),
('Чизкейк', 400.00, 'A', 'img/cheesecake.jpg', 3),
('Чай зеленый', 120.00, 'A', 'img/tea.jpg', 4),
('Греческий салат', 380.00, 'A', 'img/greek.jpg', 5),
('Оливье', 350.00, 'A', 'img/olivier.jpg', 5),
('Пицца 4 Сыра', 600.00, 'A', 'img/4cheese.jpg', 7),
('Филадельфия', 550.00, 'A', 'img/philadelphia.jpg', 8),
('Маки с огурцом', 350.00, 'A', 'img/maki.jpg', 8),
('Чикенбургер', 400.00, 'A', 'img/chicken_burger.jpg', 9),
('Веганбургер', 350.00, 'A', 'img/vege_burger.jpg', 9);
INSERT INTO Dish_Ingredients (id_dish, id_ingredients, quantity, unit) VALUES
(1, 1, 200, 'г'),
(1, 5, 50, 'г'),
(1, 4, 100, 'г'),
(2, 2, 300, 'г'),
(2, 6, 20, 'г'),
(3, 3, 250, 'г'),
(4, 7, 200, 'г'),
(4, 8, 100, 'мл'),
(5, 4, 150, 'г'),
(6, 9, 100, 'г'),
(7, 10, 100, 'г'),
(8, 4, 50, 'мл'),
(9, 4, 200, 'мл'),
(10, 12, 300, 'г'),
(10, 13, 150, 'г'),
(10, 14, 50, 'мл'),
(11, 12, 300, 'г'),
(11, 13, 150, 'г'),
(11, 14, 50, 'мл'),
(11, 15, 100, 'г'),
(12, 2, 150, 'г'),
(12, 5, 50, 'г'),
(12, 4, 100, 'г'),
(13, 3, 150, 'г'),
(13, 4, 100, 'г'),
(14, 4, 200, 'г'),
(14, 16, 50, 'г'),
(14, 17, 100, 'г'),
(14, 18, 30, 'г'),
(15, 1, 150, 'г'),
(15, 4, 50, 'г'),
(16, 4, 100, 'г'),
(16, 5, 30, 'г'),
(17, 2, 200, 'г'),
(17, 19, 150, 'г'),
(17, 4, 100, 'г'),
(18, 2, 150, 'г'),
(18, 20, 50, 'г'),
(18, 18, 50, 'г'),
(19, 21, 200, 'г'),
(19, 9, 50, 'г'),
(19, 11, 50, 'г'),
(20, 22, 10, 'г'),
(20, 11, 50, 'г'),
(21, 4, 150, 'г'),
(21, 5, 50, 'г'),
(21, 17, 100, 'г'),
(22, 1, 100, 'г'),
(22, 19, 100, 'г'),
(22, 4, 100, 'г'),
(23, 12, 300, 'г'),
(23, 13, 100, 'г'),
(23, 5, 50, 'г'),
(23, 21, 50, 'г'),
(24, 3, 150, 'г'),
(24, 21, 50, 'г'),
(24, 23, 100, 'г'),
(25, 3, 100, 'г'),
(25, 4, 50, 'г'),
(25, 23, 100, 'г'),
(26, 1, 150, 'г'),
(26, 13, 50, 'г'),
(26, 4, 100, 'г'),
(27, 16, 150, 'г'),
(27, 13, 50, 'г'),
(27, 4, 100, 'г');
INSERT INTO Zone (Name_Zone) VALUES
('Зал'),
('Летняя веранда'),
('Барная стойка'),
('VIP зал');
INSERT INTO Client_Table (Number_T, Seats, Status_T, id_zone) VALUES
(1, 4, 'R', 1),
(2, 4, 'F', 2),
(3, 2, 'F', 1),
(4, 6, 'B', 2),
(5, 8, 'F', 1),
(6, 4, 'F', 2),
(7, 2, 'F', 3),
(8, 6, 'F', 4),
(9, 4, 'F', 1),
(10, 10, 'F', 4);
INSERT INTO Zakaz (Price_Z, Data_Z, Start_Time, End_Time, Status_Z, id_promotion, id_client, id_rabotnik) VALUES
(0, '2024-05-15 14:30:00', '2024-05-15 14:35:00', '2024-05-15 15:20:00', 'C', NULL, 1, 2),
(0, '2024-05-15 18:45:00', '2024-05-15 18:50:00', '2024-05-15 19:30:00', 'C', 1, 1, 2),
(0, '2024-05-16 19:15:00', '2024-05-16 19:20:00', '2024-05-16 20:05:00', 'P', NULL, 2, 3),
(0, '2024-05-16 20:30:00', '2024-05-16 20:35:00', '2024-05-16 21:25:00', 'P', NULL, 2, 3),
(0, '2024-05-17 13:45:00', '2024-05-17 13:50:00', NULL, 'N', 2, 3, 2),
(0, '2024-05-17 14:20:00', '2024-05-17 14:25:00', NULL, 'N', NULL, 3, 2),
(0, '2024-05-18 19:00:00', '2024-05-18 19:05:00', NULL, 'N', NULL, 4, 3),
(0, '2024-05-18 20:15:00', '2024-05-18 20:20:00', '2024-05-18 21:10:00', 'C', NULL, 5, 2);
INSERT INTO Zakaz_Items (id_zakaz, id_dish, quantity, price_at_time) VALUES
(1, 1, 2, 450.00),
(2, 2, 1, 1200.00),
(2, 3, 1, 950.00),
(3, 3, 1, 950.00),
(3, 4, 1, 580.00),
(4, 4, 1, 580.00),
(5, 5, 2, 320.00),
(6, 6, 1, 350.00),
(7, 10, 1, 450.00),
(7, 11, 1, 550.00),
(8, 12, 2, 380.00),
(8, 8, 2, 180.00);
INSERT INTO Reserv (Data_R, Number_Guests, Status_R, id_client, id_table) VALUES
('2024-05-20', 4, 'A', 1, 1),
('2024-05-20', 2, 'A', 2, 2),
('2024-05-21', 2, 'C', 3, 3),
('2024-05-22', 8, 'A', 4, 4),
('2024-05-23', 6, 'A', 5, 8),
('2024-05-24', 10, 'A', 1, 10);
INSERT INTO Delivery (Address, Delivery_Time, Status_Delivery, id_zakaz, id_rabotnik) VALUES
('ул. Ленина, д. 10, кв. 5', '2024-05-15 16:00:00', 'DELIVERED', 2, 6),
('пр. Мира, д. 25, кв. 12', '2024-05-16 21:00:00', 'IN_PROGRESS', 4, 6),
('ул. Советская, д. 8, кв. 3', '2024-05-18 21:30:00', 'PENDING', 8, NULL);
INSERT INTO Feedback (Rating, Comment, Date_Feedback, id_client, id_zakaz) VALUES
(5, 'Отличный сервис и вкусная еда!', '2024-05-15 16:30:00', 1, 1),
(4, 'Вкусно, но ждали долго', '2024-05-16 20:00:00', 2, 3),
(5, 'Лучшая пицца в городе!', '2024-05-18 22:00:00', 5, 8),
(3, 'Нормально, но можно лучше', '2024-05-17 15:00:00', 3, 5);
-- ============================================
-- ИНДЕКСЫ ДЛЯ ОПТИМИЗАЦИИ
-- ============================================
CREATE INDEX idx_client_fio ON Client(FIO_C);
CREATE INDEX idx_zakaz_date ON Zakaz(Data_Z);
CREATE INDEX idx_dish_price ON Dish(Price_D);
CREATE INDEX idx_reserv_date ON Reserv(Data_R);
CREATE INDEX idx_delivery_status ON Delivery(Status_Delivery);
CREATE INDEX idx_feedback_rating ON Feedback(Rating);
CREATE INDEX idx_promotion_active ON Promotions(Is_Active, End_Date);