-- =========================================================================
-- Migración: 02_add_estado_apoyo_y_bot_config.sql
-- Descripción: Agrega el campo EstadoApoyo a PersonaMovilizada con 4 estados:
--              (PENDIENTE, CONSULTADO, APOYA, NO_APOYA) y crea la tabla
--              WhatsAppBotConfig para las respuestas diferenciadas.
-- =========================================================================

-- 1. Agregar columna EstadoApoyo a PersonaMovilizada
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.PersonaMovilizada') 
      AND name = 'EstadoApoyo'
)
BEGIN
    ALTER TABLE dbo.PersonaMovilizada
    ADD EstadoApoyo VARCHAR(30) NOT NULL CONSTRAINT DF_PersonaMovilizada_EstadoApoyo DEFAULT 'PENDIENTE';
    
    PRINT 'Columna EstadoApoyo agregada exitosamente a PersonaMovilizada.';
END
GO

-- 2. Crear tabla WhatsAppBotConfig para persistir la configuración del bot y sus respuestas diferenciadas
IF NOT EXISTS (
    SELECT 1 
    FROM sys.tables 
    WHERE name = 'WhatsAppBotConfig'
)
BEGIN
    CREATE TABLE dbo.WhatsAppBotConfig (
        IdBot INT IDENTITY(1,1) PRIMARY KEY,
        Titulo VARCHAR(150) NOT NULL DEFAULT 'Consulta de Intención de Voto',
        NombreCandidato VARCHAR(150) NOT NULL DEFAULT 'nuestro candidato',
        PlantillaPregunta VARCHAR(MAX) NOT NULL,
        Opcion1_Texto VARCHAR(200) NOT NULL DEFAULT 'Sí, totalmente',
        Opcion1_EstadoApoyo VARCHAR(30) NOT NULL DEFAULT 'APOYA',
        Opcion1_Respuesta VARCHAR(MAX) NOT NULL,
        Opcion2_Texto VARCHAR(200) NOT NULL DEFAULT 'Tal vez / Indeciso',
        Opcion2_EstadoApoyo VARCHAR(30) NOT NULL DEFAULT 'CONSULTADO',
        Opcion2_Respuesta VARCHAR(MAX) NOT NULL,
        Opcion3_Texto VARCHAR(200) NOT NULL DEFAULT 'No',
        Opcion3_EstadoApoyo VARCHAR(30) NOT NULL DEFAULT 'NO_APOYA',
        Opcion3_Respuesta VARCHAR(MAX) NOT NULL,
        Activo BIT NOT NULL DEFAULT 1,
        FechaUpdate DATETIME DEFAULT GETDATE()
    );

    -- Insertar configuración inicial por defecto
    INSERT INTO dbo.WhatsAppBotConfig (
        Titulo,
        NombreCandidato,
        PlantillaPregunta,
        Opcion1_Texto,
        Opcion1_EstadoApoyo,
        Opcion1_Respuesta,
        Opcion2_Texto,
        Opcion2_EstadoApoyo,
        Opcion2_Respuesta,
        Opcion3_Texto,
        Opcion3_EstadoApoyo,
        Opcion3_Respuesta,
        Activo
    ) VALUES (
        'Consulta de Intención de Voto',
        'nuestro candidato',
        'Hola {nombre}, ¿apoyarás a {candidato} en las próximas elecciones?\n\n1️⃣ Sí, totalmente\n2️⃣ Tal vez / Indeciso\n3️⃣ No\n\nPor favor responde con el número 1, 2 o 3.',
        'Sí, totalmente',
        'APOYA',
        '¡Excelente {nombre}! Muchísimas gracias por tu respaldo a {candidato}. ¡Juntos vamos a ganar!',
        'Tal vez / Indeciso',
        'CONSULTADO',
        'Gracias {nombre}. Te compartiremos nuestras principales propuestas para que conozcas a detalle el plan de trabajo de {candidato}.',
        'No',
        'NO_APOYA',
        'Comprendemos tu postura, {nombre}. Agradecemos mucho tu sinceridad y tiempo. ¡Que tengas un excelente día!',
        1
    );

    PRINT 'Tabla WhatsAppBotConfig creada con plantilla por defecto.';
END
GO
