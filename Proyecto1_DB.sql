-- CREAR LA BASE DE DATOS
IF DB_ID('Proyecto1_DB') IS NULL
BEGIN
    CREATE DATABASE Proyecto1_DB;
END
GO

USE Proyecto1_DB;
GO

-- CREAR LAS TABLAS
CREATE TABLE Docentes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Usuario NVARCHAR(50) NOT NULL,
    Contrasena NVARCHAR(255) NOT NULL,
    Activo BIT DEFAULT 1
);

CREATE TABLE Cuatrimestre (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(100) NOT NULL
);

CREATE TABLE Curso (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(150) NOT NULL,
    CuatrimestreId INT NOT NULL,
    FOREIGN KEY (CuatrimestreId) REFERENCES dbo.Cuatrimestre(Id)
);

CREATE TABLE Estudiante (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(100) NOT NULL,
    Apellidos NVARCHAR(100) NOT NULL,
    Identificacion NVARCHAR(50) NOT NULL UNIQUE,
    FechaNacimiento DATE NOT NULL,
    Provincia NVARCHAR(100) NOT NULL,
    Canton NVARCHAR(100) NOT NULL,
    Distrito NVARCHAR(100) NOT NULL,
    Correo NVARCHAR(150) NOT NULL UNIQUE,
    CuatrimestreId INT NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (CuatrimestreId) REFERENCES dbo.Cuatrimestre(Id)
);

CREATE TABLE Matricula (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EstudianteId INT NOT NULL,
    CursoId INT NOT NULL,
    FechaMatricula DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (EstudianteId) REFERENCES dbo.Estudiante(Id) ON DELETE CASCADE,
    FOREIGN KEY (CursoId) REFERENCES dbo.Curso(Id) ON DELETE CASCADE
);

CREATE TABLE Evaluacion (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EstudianteId INT NOT NULL,
    CursoId INT NOT NULL,
    CuatrimestreId INT NOT NULL,
    Nota DECIMAL(5,2) NOT NULL CHECK (Nota >= 0 AND Nota <= 100),
    Observaciones NVARCHAR(500) NULL,
    Participacion NVARCHAR(50) NOT NULL,
    Estado NVARCHAR(20) NOT NULL CHECK (Estado IN ('Aprobado','Reprobado')),
    FechaEvaluacion DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (EstudianteId) REFERENCES dbo.Estudiante(Id),
    FOREIGN KEY (CursoId) REFERENCES dbo.Curso(Id),
    FOREIGN KEY (CuatrimestreId) REFERENCES dbo.Cuatrimestre(Id),
    CONSTRAINT UQ_Evaluacion UNIQUE (EstudianteId, CursoId, CuatrimestreId)
);

-- INSERTAR DATOS BASICOS EN LAS TABLAS
INSERT INTO Docentes (Usuario, Contrasena)
VALUES ('admin', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92'); --USUARIO: admin - CONTRASEÑA: 123456

INSERT INTO Cuatrimestre (Nombre) VALUES ('Cuatrimestre 1'), ('Cuatrimestre 2'), ('Cuatrimestre 3');

INSERT INTO Curso (Nombre, CuatrimestreId) VALUES
('Programación I', 1),
('Matemáticas I', 1),
('Programación II', 2),
('Bases de Datos', 2),
('Desarrollo Web', 3);
GO

