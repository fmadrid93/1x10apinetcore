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
    }
}
