namespace Dtos.Votante
{
    public class SuperAdminLimpiarMarcasRequest
    {
        public bool LimpiarVoto { get; set; } = true;
        public bool LimpiarGasolina { get; set; } = true;
        public bool LimpiarPasoPC { get; set; } = true;
        public int? IdTerritorio { get; set; }
    }
}
