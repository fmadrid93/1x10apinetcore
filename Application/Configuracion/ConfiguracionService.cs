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

        public List<string> ObtenerCamposObligatorios()
        {
            string valor = _data.ObtenerValor("CAMPOS_OBLIGATORIOS_VOTANTE", "");
            if (string.IsNullOrWhiteSpace(valor)) return new List<string>();

            return valor
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim().ToUpperInvariant())
                .Where(CamposVotanteCatalogo.EsCodigoValido)
                .Distinct()
                .ToList();
        }

        public bool GuardarCamposObligatorios(List<string> campos)
        {
            var validos = (campos ?? new List<string>())
                .Select(c => (c ?? "").Trim().ToUpperInvariant())
                .Where(CamposVotanteCatalogo.EsCodigoValido)
                .Distinct()
                .ToList();

            return _data.GuardarValor(
                "CAMPOS_OBLIGATORIOS_VOTANTE",
                string.Join(",", validos),
                "Campos del registro de votante que son obligatorios además de Nombre y CI (lista separada por comas)"
            );
        }
    }
}
