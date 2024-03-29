namespace VisualizacionLoteria.Pages.Models;

public class LoteriaComprada
{
    public int NumeroMayor { get; set; }
    public int NumeroDeFracciones { get; set; }

    public LoteriaComprada(int NumeroMayor, int NumeroDeFracciones)
    {
        this.NumeroMayor = NumeroMayor;
        this.NumeroDeFracciones = NumeroDeFracciones;
    }
}
