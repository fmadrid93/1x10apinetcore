namespace Application.Configuracion
{
    public static class CamposVotanteCatalogo
    {
        public static readonly (string Codigo, string Etiqueta)[] CamposConfigurables = new[]
        {
            ("CELULAR", "Celular"),
            ("DIRECCION", "Dirección de referencia"),
            ("SEXO", "Sexo"),
            ("RANGO_EDAD", "Rango de edad"),
            ("RECINTO", "Recinto de votación"),
            ("NIVEL_COMPROMISO", "Nivel de compromiso"),
            ("OBSERVACIONES", "Observaciones"),
            ("UBICACION", "Ubicación (mapa)"),
        };

        public static bool EsCodigoValido(string codigo)
        {
            foreach (var campo in CamposConfigurables)
            {
                if (campo.Codigo == codigo) return true;
            }
            return false;
        }
    }
}
