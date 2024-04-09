using System.Globalization;

namespace VisualizacionLoteria.Pages.Util
{
    public static class PremiosLoteria
    {
        public enum TipoPremio
        {
            PremioMayor,
            SegundoPremio,
            TercerPremio,
            IgualAMayorDiferenteSerie,
            IgualASegundoDiferenteSerie,
            IgualATerceroDiferenteSerie,
            InversaDelMayor,
            InversaDelSegundo,
            InversaDelTercero,
            NumeroDuplicador
        }

        public static IDictionary<TipoPremio, (int probabilidad, int premio)> InformacionPremios = new Dictionary<TipoPremio, (int probabilidad, int premio)>
        {
            { TipoPremio.PremioMayor, (98000, 24000000) },
            { TipoPremio.SegundoPremio, (98000, 7400000) },
            { TipoPremio.TercerPremio, (98000, 1800000) },
            { TipoPremio.IgualAMayorDiferenteSerie, (100, 40000) },
            { TipoPremio.IgualASegundoDiferenteSerie, (100, 8400) },
            { TipoPremio.IgualATerceroDiferenteSerie, (100, 6000) },
            { TipoPremio.InversaDelMayor, (100, 3000) },
            { TipoPremio.InversaDelSegundo, (100, 2400) },
            { TipoPremio.InversaDelTercero, (100, 1500) },
            { TipoPremio.NumeroDuplicador, (100, 3000) }
        };

        public static string TipoPremioAString(TipoPremio tipoPremio)
        {
            switch (tipoPremio)
            {
                case TipoPremio.PremioMayor:
                    return "Premio Mayor";
                case TipoPremio.SegundoPremio:
                    return "Segundo Premio";
                case TipoPremio.TercerPremio:
                    return "Tercer Premio";
                case TipoPremio.IgualAMayorDiferenteSerie:
                    return "Igual a mayor, diferente serie";
                case TipoPremio.IgualASegundoDiferenteSerie:
                    return "Igual a segundo, diferente serie";
                case TipoPremio.IgualATerceroDiferenteSerie:
                    return "Igual a tercero, diferente serie";
                case TipoPremio.InversaDelMayor:
                    return "Inversa del mayor";
                case TipoPremio.InversaDelSegundo:
                    return "Inversa del segundo";
                case TipoPremio.InversaDelTercero:
                    return "Inversa del tercero";
                case TipoPremio.NumeroDuplicador:
                    return "Número duplicador";
                default:
                    return "Tipo de premio desconocido";
            }
        }

        public static string PremioAString(int premio) => $"₡{premio:N0}";
    }
}