using System;
using System.Collections.Generic;
using System.Linq;
using Infrastructure;

namespace Application.Configuracion
{
    public class ConfiguracionService
    {
        private readonly DConfiguracion _data = new DConfiguracion();

        public bool ObtenerPermitirDuplicados()
        {
            return _data.ObtenerPermitirDuplicados();
        }

        public bool GuardarPermitirDuplicados(bool permitir)
        {
            return _data.GuardarPermitirDuplicados(permitir);
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
        /// Territorio del Admin que consulta (viene del JWT). Null = SuperAdmin.
        /// </param>
        public List<string> ObtenerCamposObligatorios(int? idTerritorio)
        {
            string todosLosCampos = string.Join(
                ",",
                CamposVotanteCatalogo.CamposConfigurables.Select(c => c.Codigo)
            );

            // El default de "todos obligatorios" aplica siempre que no exista una config
            // explícita. Un territorio que nunca configuró nada propio hereda el default
            // global del SuperAdmin (que a su vez, si tampoco fue tocado, es "todos").
            string valorPorDefecto = idTerritorio.HasValue
                ? _data.ObtenerValor(ClaveCamposObligatoriosBase, todosLosCampos)
                : todosLosCampos;

            string valor = _data.ObtenerValor(ClaveCamposObligatorios(idTerritorio), valorPorDefecto);
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
