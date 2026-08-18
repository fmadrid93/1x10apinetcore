/*
    06. Completa los campos de control faltantes en Usuario, según el
    patron institucional (Manual_Estandares_CSharp_SQLServer_v8.md §5.3/§18):

        IdUsuarioCreate  -> ya agregado en el script 01.
        FechaCreate      -> ya existia.
        IdUsuarioUpdate  -> se agrega aqui.
        FechaUpdate      -> ya existia.
        IdUsuarioDelete  -> se agrega aqui.
        FechaDelete      -> se agrega aqui.
        Motivo           -> se agrega aqui (NVARCHAR(300), igual que los
                             ejemplos del manual, sin importar que el resto
                             de columnas de texto de esta tabla sean VARCHAR).

    Todos NULL por defecto: no hay forma de reconstruir este dato para
    filas historicas, asi que quedan sin valor hasta el primer
    update/baja/reactivacion real que ocurra desde ahora.

    Idempotente: no falla si ya se ejecuto antes.
*/

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Usuario') AND name = 'IdUsuarioUpdate')
BEGIN
    ALTER TABLE dbo.Usuario ADD IdUsuarioUpdate INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Usuario') AND name = 'IdUsuarioDelete')
BEGIN
    ALTER TABLE dbo.Usuario ADD IdUsuarioDelete INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Usuario') AND name = 'FechaDelete')
BEGIN
    ALTER TABLE dbo.Usuario ADD FechaDelete DATETIME NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Usuario') AND name = 'Motivo')
BEGIN
    ALTER TABLE dbo.Usuario ADD Motivo NVARCHAR(300) NULL;
END
GO
