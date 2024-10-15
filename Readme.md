# DB Name: University

## Postgres - Npgsql
```
-- Создание таблицы Студенты
CREATE TABLE Students (
    Id SERIAL PRIMARY KEY,  -- Идентификатор студента
    FirstName VARCHAR(50) NOT NULL,  -- Имя студента
    LastName VARCHAR(50) NOT NULL,   -- Фамилия студента
    DateOfBirth DATE NOT NULL        -- Дата рождения
);

-- Создание таблицы Оценки студентов
CREATE TABLE StudentGrades (
    Id SERIAL PRIMARY KEY,  -- Идентификатор записи об оценке
    StudentId INT NOT NULL,  -- Внешний ключ на студента
    Subject VARCHAR(100) NOT NULL,  -- Название предмета
    Grade INT CHECK (Grade BETWEEN 1 AND 5),  -- Оценка (1-5)
    DateReceived DATE NOT NULL,  -- Дата получения оценки
    CONSTRAINT fk_student FOREIGN KEY (StudentId) REFERENCES Students(Id) ON DELETE CASCADE
);

-- Вставка данных в таблицу Студенты
INSERT INTO Students (FirstName, LastName, DateOfBirth) VALUES
('John', 'Doe', '2000-03-15'),
('Jane', 'Smith', '1999-06-23'),
('Michael', 'Brown', '2001-12-05'),
('Emily', 'Jones', '1998-11-12'),
('Daniel', 'Williams', '2002-07-29');

-- Вставка данных в таблицу Оценки студентов
INSERT INTO StudentGrades (StudentId, Subject, Grade, DateReceived) VALUES
(1, 'Mathematics', 5, '2024-01-15'),
(2, 'Mathematics', 4, '2024-01-16'),
(3, 'History', 3, '2024-01-17'),
(4, 'Physics', 5, '2024-01-18'),
(5, 'Chemistry', 2, '2024-01-19'),
(1, 'Physics', 4, '2024-01-20'),
(2, 'Chemistry', 5, '2024-01-21');
```

## MS SQL Server - Microsoft.Data.SqlClient
```
-- Создание таблицы Студенты
CREATE TABLE Students (
    Id INT IDENTITY(1,1) PRIMARY KEY,  -- Идентификатор студента
    FirstName NVARCHAR(50) NOT NULL,  -- Имя студента
    LastName NVARCHAR(50) NOT NULL,   -- Фамилия студента
    DateOfBirth DATE NOT NULL         -- Дата рождения
);

-- Создание таблицы Оценки студентов
CREATE TABLE StudentGrades (
    Id INT IDENTITY(1,1) PRIMARY KEY,  -- Идентификатор записи об оценке
    StudentId INT NOT NULL,  -- Внешний ключ на студента
    Subject NVARCHAR(100) NOT NULL,  -- Название предмета
    Grade INT CHECK (Grade BETWEEN 1 AND 5),  -- Оценка (1-5)
    DateReceived DATE NOT NULL,  -- Дата получения оценки
    CONSTRAINT fk_student FOREIGN KEY (StudentId) REFERENCES Students(Id) ON DELETE CASCADE
);


-- Вставка данных в таблицу Студенты
INSERT INTO Students (FirstName, LastName, DateOfBirth) VALUES
('John', 'Doe', '2000-03-15'),
('Jane', 'Smith', '1999-06-23'),
('Michael', 'Brown', '2001-12-05'),
('Emily', 'Jones', '1998-11-12'),
('Daniel', 'Williams', '2002-07-29');

-- Вставка данных в таблицу Оценки студентов
INSERT INTO StudentGrades (StudentId, Subject, Grade, DateReceived) VALUES
(1, 'Mathematics', 5, '2024-01-15'),
(2, 'Mathematics', 4, '2024-01-16'),
(3, 'History', 3, '2024-01-17'),
(4, 'Physics', 5, '2024-01-18'),
(5, 'Chemistry', 2, '2024-01-19'),
(1, 'Physics', 4, '2024-01-20'),
(2, 'Chemistry', 5, '2024-01-21');

```