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
    Usuario NVARCHAR(50) NOT NULL UNIQUE,
    Contrasena NVARCHAR(255) NOT NULL,
    Activo BIT DEFAULT 1
);
GO

CREATE TABLE Cuatrimestre (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(100) NOT NULL
);
GO

CREATE TABLE Curso (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Codigo NVARCHAR(50) NOT NULL UNIQUE,
    Nombre NVARCHAR(150) NOT NULL,
    Descripcion NVARCHAR(255) NULL,
    Creditos INT NOT NULL DEFAULT 4,
    CuatrimestreId INT NOT NULL,
    FechaCreacion DATETIME DEFAULT GETDATE(),
    CreadoPor NVARCHAR(100),
    FechaModificacion DATETIME NULL,
    ModificadoPor NVARCHAR(100) NULL,
    FOREIGN KEY (CuatrimestreId) REFERENCES Cuatrimestre(Id)
);
GO

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
    Usuario NVARCHAR(50) NOT NULL UNIQUE,
    Contrasena NVARCHAR(255) NOT NULL,
    FOREIGN KEY (CuatrimestreId) REFERENCES Cuatrimestre(Id)
);
GO

CREATE TABLE Matricula (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EstudianteId INT NOT NULL,
    CursoId INT NOT NULL,
    FechaMatricula DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (EstudianteId) REFERENCES Estudiante(Id) ON DELETE CASCADE,
    FOREIGN KEY (CursoId) REFERENCES Curso(Id) ON DELETE CASCADE
);
GO

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
    FOREIGN KEY (EstudianteId) REFERENCES Estudiante(Id),
    FOREIGN KEY (CursoId) REFERENCES Curso(Id),
    FOREIGN KEY (CuatrimestreId) REFERENCES Cuatrimestre(Id),
    CONSTRAINT UQ_Evaluacion UNIQUE (EstudianteId, CursoId, CuatrimestreId)
);
GO

CREATE TABLE Rol (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(100) UNIQUE NOT NULL,
    Descripcion NVARCHAR(255)
);
GO

CREATE TABLE UsuarioRol (
    DocenteId INT NOT NULL,
    RolId INT NOT NULL,
    PRIMARY KEY (DocenteId, RolId),
    FOREIGN KEY (DocenteId) REFERENCES Docentes(Id),
    FOREIGN KEY (RolId) REFERENCES Rol(Id)
);
GO

CREATE TABLE Bitacora (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Usuario NVARCHAR(100),
    Accion NVARCHAR(255),
    Modulo NVARCHAR(100),
    Fecha DATETIME DEFAULT GETDATE(),
    IP NVARCHAR(50)
);
GO

CREATE TABLE DocenteCurso (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    DocenteId INT NOT NULL,
    CursoId INT NOT NULL,
    FechaAsignacion DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (DocenteId) REFERENCES Docentes(Id),
    FOREIGN KEY (CursoId) REFERENCES Curso(Id),
    CONSTRAINT UQ_DocenteCurso UNIQUE (DocenteId, CursoId)
);
GO

-- INSERTAR DATOS BASICOS EN LAS TABLAS
INSERT INTO Docentes (Usuario, Contrasena)
VALUES ('admin', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92'); -- Contraseña: 123456

INSERT INTO Cuatrimestre (Nombre)
VALUES ('Cuatrimestre 1'), ('Cuatrimestre 2'), ('Cuatrimestre 3');

INSERT INTO Curso (Codigo, Nombre, Descripcion, Creditos, CuatrimestreId, CreadoPor)
VALUES 
('CUR1', 'Programación I', 'Introducción a la programación', 4, 1, 'admin'),
('CUR2', 'Matemáticas I', 'Fundamentos matemáticos básicos', 4, 1, 'admin'),
('CUR3', 'Programación II', 'Estructuras avanzadas y OOP', 4, 2, 'admin'),
('CUR4', 'Bases de Datos', 'Diseño y modelado de bases de datos', 4, 2, 'admin'),
('CUR5', 'Desarrollo Web', 'Aplicaciones web modernas con MVC', 4, 3, 'admin');

INSERT INTO Rol (Nombre, Descripcion)
VALUES 
('Administrador', 'Acceso total al sistema'),
('Coordinador', 'Gestión de cursos y reportes'),
('Docente', 'Registro y evaluación de estudiantes');

-- Asignar todos los roles al admin
INSERT INTO UsuarioRol (DocenteId, RolId)
SELECT 1, Id FROM Rol;

INSERT INTO Estudiante (
    Nombre, Apellidos, Identificacion, FechaNacimiento, Provincia, Canton, Distrito, Correo, CuatrimestreId, Usuario, Contrasena
) VALUES ('Brandon', 'Valverde Lobo', '901180346', '2003-02-17', 'Alajuela', 'Grecia', 'Bolivar', 'brandonvalobo@gmail.com', 1, 'Brandon', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92');

