using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Infrastructure
{
    public class DPersonaMovilizada : DbHelper
    {



            public DataTable Insertar(
                int idUsuarioMovilizador,
                int? idTerritorio,
                string nombres,
                string apellidos,
                string? ci,
                string? celular,
                string? direccionReferencia,
                string? sexo,
                string? rangoEdad,
                string? recintoVotacion,
                string? idRecinto,
                bool? requiereAyudaVotar,
                string? nivelCompromiso,
                string? observaciones,
                decimal? latitud,
                decimal? longitud
            )
            {
                return EjecutarPA(
                    "pa_persona_movilizada_insertar",
                    new SqlParameter("@IdUsuarioMovilizador", SqlDbType.Int) { Value = idUsuarioMovilizador },
                    new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = (object?)idTerritorio ?? DBNull.Value },
                    new SqlParameter("@Nombres", SqlDbType.VarChar, 150) { Value = nombres },
                    new SqlParameter("@Apellidos", SqlDbType.VarChar, 150) { Value = apellidos },
                    new SqlParameter("@CI", SqlDbType.VarChar, 30) { Value = (object?)ci ?? DBNull.Value },
                    new SqlParameter("@Celular", SqlDbType.VarChar, 30) { Value = (object?)celular ?? DBNull.Value },
                    new SqlParameter("@DireccionReferencia", SqlDbType.VarChar, 250) { Value = (object?)direccionReferencia ?? DBNull.Value },
                    new SqlParameter("@Sexo", SqlDbType.VarChar, 20) { Value = (object?)sexo ?? DBNull.Value },
                    new SqlParameter("@RangoEdad", SqlDbType.VarChar, 30) { Value = (object?)rangoEdad ?? DBNull.Value },
                    new SqlParameter("@RecintoVotacion", SqlDbType.VarChar, 150) { Value = (object?)recintoVotacion ?? DBNull.Value },
                     new SqlParameter("@IdRecinto", SqlDbType.VarChar, 150) { Value = (object?)idRecinto ?? DBNull.Value },
                    new SqlParameter("@RequiereAyudaVotar", SqlDbType.Bit) { Value = (object?)requiereAyudaVotar ?? DBNull.Value },
                    new SqlParameter("@NivelCompromiso", SqlDbType.VarChar, 30) { Value = (object?)nivelCompromiso ?? DBNull.Value },
                    new SqlParameter("@Observaciones", SqlDbType.VarChar, 500) { Value = (object?)observaciones ?? DBNull.Value },
                    new SqlParameter("@Latitud", SqlDbType.Decimal) { Value = (object?)latitud ?? DBNull.Value, Precision = 10, Scale = 8 },
                    new SqlParameter("@Longitud", SqlDbType.Decimal) { Value = (object?)longitud ?? DBNull.Value, Precision = 11, Scale = 8 }
                );
            }

            public DataTable Actualizar(
                int idPersonaMovilizada,
                int idUsuarioMovilizador,
                int? idTerritorio,
                string nombres,
                string apellidos,
                string? ci,
                string? celular,
                string? direccionReferencia,
                string? sexo,
                string? rangoEdad,
                string? recintoVotacion,
                  string? idRecinto,
                bool? requiereAyudaVotar,
                string? nivelCompromiso,
                string? observaciones,
                decimal? latitud,
                decimal? longitud
            )
            {
                return EjecutarPA(
                    "pa_persona_movilizada_actualizar",
                    new SqlParameter("@IdPersonaMovilizada", SqlDbType.Int) { Value = idPersonaMovilizada },
                    new SqlParameter("@IdUsuarioMovilizador", SqlDbType.Int) { Value = idUsuarioMovilizador },
                    new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = (object?)idTerritorio ?? DBNull.Value },
                    new SqlParameter("@Nombres", SqlDbType.VarChar, 150) { Value = nombres },
                    new SqlParameter("@Apellidos", SqlDbType.VarChar, 150) { Value = apellidos },
                    new SqlParameter("@CI", SqlDbType.VarChar, 30) { Value = (object?)ci ?? DBNull.Value },
                    new SqlParameter("@Celular", SqlDbType.VarChar, 30) { Value = (object?)celular ?? DBNull.Value },
                    new SqlParameter("@DireccionReferencia", SqlDbType.VarChar, 250) { Value = (object?)direccionReferencia ?? DBNull.Value },
                    new SqlParameter("@Sexo", SqlDbType.VarChar, 20) { Value = (object?)sexo ?? DBNull.Value },
                    new SqlParameter("@RangoEdad", SqlDbType.VarChar, 30) { Value = (object?)rangoEdad ?? DBNull.Value },
                    new SqlParameter("@RecintoVotacion", SqlDbType.VarChar, 150) { Value = (object?)recintoVotacion ?? DBNull.Value },

                     new SqlParameter("@IdRecinto", SqlDbType.VarChar, 150) { Value = (object?)idRecinto ?? DBNull.Value },
                    new SqlParameter("@RequiereAyudaVotar", SqlDbType.Bit) { Value = (object?)requiereAyudaVotar ?? DBNull.Value },
                    new SqlParameter("@NivelCompromiso", SqlDbType.VarChar, 30) { Value = (object?)nivelCompromiso ?? DBNull.Value },
                    new SqlParameter("@Observaciones", SqlDbType.VarChar, 500) { Value = (object?)observaciones ?? DBNull.Value },
                    new SqlParameter("@Latitud", SqlDbType.Decimal) { Value = (object?)latitud ?? DBNull.Value, Precision = 10, Scale = 8 },
                    new SqlParameter("@Longitud", SqlDbType.Decimal) { Value = (object?)longitud ?? DBNull.Value, Precision = 11, Scale = 8 }
                );
            }



        public DataTable EliminarLogico(int idPersonaMovilizada, int idUsuarioMovilizador)
        {
            return EjecutarPA(
                "pa_persona_movilizada_eliminar_logico",
                new SqlParameter("@IdPersonaMovilizada", SqlDbType.Int) { Value = idPersonaMovilizada },
                new SqlParameter("@IdUsuarioMovilizador", SqlDbType.Int) { Value = idUsuarioMovilizador }
            );
        }

        public DataTable ObtenerPorId(int idPersonaMovilizada)
        {
            return EjecutarPA(
                "pa_persona_movilizada_obtener_por_id",
                new SqlParameter("@IdPersonaMovilizada", SqlDbType.Int) { Value = idPersonaMovilizada }
            );
        }

        public DataTable ListarPorMovilizador(int idUsuarioMovilizador, string? texto, string? estadoDiaD)
        {
            return EjecutarPA(
                "pa_persona_movilizada_listar_por_movilizador",
                new SqlParameter("@IdUsuarioMovilizador", SqlDbType.Int) { Value = idUsuarioMovilizador },
                new SqlParameter("@Texto", SqlDbType.VarChar, 150) { Value = (object?)texto ?? DBNull.Value },
                new SqlParameter("@EstadoDiaD", SqlDbType.VarChar, 30) { Value = (object?)estadoDiaD ?? DBNull.Value }
            );
        }

        public DataTable BuscarGeneral(int? idTerritorio, int? idUsuarioMovilizador, string? texto, string? estadoDiaD)
        {
            return EjecutarPA(
                "pa_persona_movilizada_buscar_general",
                new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = (object?)idTerritorio ?? DBNull.Value },
                new SqlParameter("@IdUsuarioMovilizador", SqlDbType.Int) { Value = (object?)idUsuarioMovilizador ?? DBNull.Value },
                new SqlParameter("@Texto", SqlDbType.VarChar, 150) { Value = (object?)texto ?? DBNull.Value },
                new SqlParameter("@EstadoDiaD", SqlDbType.VarChar, 30) { Value = (object?)estadoDiaD ?? DBNull.Value }
            );
        }

        public DataTable ResumenMovilizador(int idUsuarioMovilizador)
        {
            return EjecutarPA(
                "pa_persona_movilizada_resumen_movilizador",
                new SqlParameter("@IdUsuarioMovilizador", SqlDbType.Int) { Value = idUsuarioMovilizador }
            );
        }
    }
}