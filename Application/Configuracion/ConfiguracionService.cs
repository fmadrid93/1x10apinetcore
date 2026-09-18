using System;
using System.Collections.Generic;
using System.Linq;
using Infrastructure;

namespace Application.Configuracion
{
    public class ConfiguracionService
    {
        private readonly DConfiguracion _data = new DConfiguracion();

        private const string ClavePermitirDuplicadosBase = "PERMITIR_VOTANTES_DUPLICADOS";

        private static string ClavePermitirDuplicados(int? idTerritorio)
        {
            // Mismo esquema que los campos obligatorios: sin territorio (SuperAdmin) es
            // el default global; con territorio, cada Admin tiene su propia clave.
            return idTerritorio.HasValue
                ? $"{ClavePermitirDuplicadosBase}_{idTerritorio.Value}"
                : ClavePermitirDuplicadosBase;
        }

        /// <param name="idTerritorio">Territorio del Admin que consulta (viene del JWT) o del votante/movilizador. Null = SuperAdmin.</param>
        /// <param name="idUsuario">IdUsuario del movilizador o consultante para heredar la config de su supervisor/Admin.</param>
        public bool ObtenerPermitirDuplicados(int? idTerritorio, int? idUsuario = null)
        {
            string valor = _data.ObtenerValorConHerencia(ClavePermitirDuplicadosBase, idTerritorio, idUsuario, "0");
            return valor == "1" || valor.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        /// <param name="idTerritorio">Territorio del Admin que guarda (viene del JWT). Null = SuperAdmin.</param>
        public bool GuardarPermitirDuplicados(int? idTerritorio, bool permitir)
        {
            string descripcion = idTerritorio.HasValue
                ? $"Permite o bloquea CI duplicado propio del territorio {idTerritorio.Value} (0=Bloquear, 1=Permitir)"
                : "Permite o bloquea CI duplicado: default global que heredan los territorios sin config propia (0=Bloquear, 1=Permitir)";

            return _data.GuardarValor(ClavePermitirDuplicados(idTerritorio), permitir ? "1" : "0", descripcion);
        }

        private const string ClaveCamposObligatoriosBase = "CAMPOS_OBLIGATORIOS_VOTANTE";

        private static string ClaveCamposObligatorios(int? idTerritorio)
        {
            // Sin territorio (SuperAdmin) = clave base = default global.
            // Con territorio = una clave propia por Admin Territorial, para que cada uno
            // configure "su estructura" sin afectar a los demás municipios/zonas.
            return idTerritorio.HasValue
                ? $"{ClaveCamposObligatoriosBase}_{idTerritorio.Value}"
                : ClaveCamposObligatoriosBase;
        }

        /// <param name="idTerritorio">
        /// Territorio del Admin que consulta (viene del JWT) o del votante/movilizador. Null = SuperAdmin.
        /// </param>
        /// <param name="idUsuario">IdUsuario para herencia por supervisor.</param>
        public List<string> ObtenerCamposObligatorios(int? idTerritorio, int? idUsuario = null)
        {
            string todosLosCampos = string.Join(
                ",",
                CamposVotanteCatalogo.CamposConfigurables.Select(c => c.Codigo)
            );

            string valor = _data.ObtenerValorConHerencia(ClaveCamposObligatoriosBase, idTerritorio, idUsuario, todosLosCampos);
            if (string.IsNullOrWhiteSpace(valor)) return new List<string>();

            return valor
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim().ToUpperInvariant())
                .Where(CamposVotanteCatalogo.EsCodigoValido)
                .Distinct()
                .ToList();
        }

        /// <param name="idTerritorio">Territorio del Admin que guarda (viene del JWT). Null = SuperAdmin.</param>
        public bool GuardarCamposObligatorios(int? idTerritorio, List<string> campos)
        {
            var validos = (campos ?? new List<string>())
                .Select(c => (c ?? "").Trim().ToUpperInvariant())
                .Where(CamposVotanteCatalogo.EsCodigoValido)
                .Distinct()
                .ToList();

            string descripcion = idTerritorio.HasValue
                ? $"Campos obligatorios del registro de votante propios del territorio {idTerritorio.Value} (lista separada por comas)"
                : "Campos obligatorios del registro de votante: default global que heredan los territorios sin config propia (lista separada por comas)";

            return _data.GuardarValor(ClaveCamposObligatorios(idTerritorio), string.Join(",", validos), descripcion);
        }
    }
}
