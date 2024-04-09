using Microsoft.AspNetCore.Components;
using MudBlazor;
using VisualizacionLoteria.Pages.Models;
using VisualizacionLoteria.Pages.Util;

namespace VisualizacionLoteria.Pages.Components;

public partial class Simulacion
{
    public enum VelocidadDeSimulacion
    {
        Lenta,
        Media,
        Rapida
    }

    public enum Premios
    {
        PrimerPremio,
        SegundoPremio,
        TercerPremio,
        IgualAMayorDiferenteSerie,
        IgualASegundoDiferenteSerie,
        IgualATerceroDiferenteSerie,
        InversaAlMayor,
        InversaAlSegundo,
        InversaAlTercero,
        NumeroDuplicador
    }

    [Parameter, EditorRequired]
    public List<LoteriaComprada> NumerosJugados { get; set; } = null!;

    [Parameter, EditorRequired]
    public int? LoteriasPorSegundo { get; set; } = null!;

    [Parameter, EditorRequired]
    public bool DeberiaEstarSimulando { get; set; } = false;

    [Parameter, EditorRequired]
    public bool DeberiaReiniciarSimulacion { get; set; } = false;

    [Parameter]
    public EventCallback<bool> DeberiaReiniciarSimulacionChanged { get; set; }

    // Variables internas para los parámetros.
    private List<LoteriaComprada> NumerosJugadosInterno { get; set; } = null!;
    private int? LoteriasPorSegundoInterno { get; set; } = null!;
    private bool DeberiaEstarSimulandoInterno { get; set; } = false;

    // Variables de la simulación.
    private bool HaSimulado { get; set; } = false;
    private System.Timers.Timer Timer { get; set; } = new System.Timers.Timer();
    private Random GeneradorDeNumerosAleatorios { get; set; } = new Random((int)DateTime.Now.Ticks);
    private TiqueteDeLoteria PrimerPremio { get; set; } = new TiqueteDeLoteria();
    private TiqueteDeLoteria SegundoPremio { get; set; } = new TiqueteDeLoteria();
    private TiqueteDeLoteria TercerPremio { get; set; } = new TiqueteDeLoteria();
    private List<TiqueteDeLoteria> LoteriaJugada { get; set; } = new List<TiqueteDeLoteria>();
    private int NumeroDuplicador { get; set; } = 0;

    //resultados
    private const int PrecioPorFraccion = 1500;
    private int FraccionesJugadas { get; set; } = 0;
    private int LoteriasJugadas { get; set; } = 0;
    private int DineroGastado { get; set; } = 0;
    private int DineroGanado { get; set; } = 0;
    private int BalanceTotal { get; set; } = 0;

    //tablas
    private bool TablaBalanceAbierta { get; set; } = true;
    private bool TablaTiempoAbierta { get; set; } = false;
    private bool TablaPremiosAbierta { get; set; } = false;

    //temp 
    private bool usarSimulacionLiteral = false;

    private Dictionary<PremiosLoteria.TipoPremio, int> PremiosGanados { get; set; } = new Dictionary<PremiosLoteria.TipoPremio, int>()
    {
        {PremiosLoteria.TipoPremio.PremioMayor, 0},
        {PremiosLoteria.TipoPremio.SegundoPremio, 0},
        {PremiosLoteria.TipoPremio.TercerPremio, 0},
        {PremiosLoteria.TipoPremio.IgualAMayorDiferenteSerie, 0},
        {PremiosLoteria.TipoPremio.IgualASegundoDiferenteSerie, 0},
        {PremiosLoteria.TipoPremio.IgualATerceroDiferenteSerie, 0},
        {PremiosLoteria.TipoPremio.InversaDelMayor, 0},
        {PremiosLoteria.TipoPremio.InversaDelSegundo, 0},
        {PremiosLoteria.TipoPremio.InversaDelTercero, 0},
        {PremiosLoteria.TipoPremio.NumeroDuplicador, 0}
    };

    protected override async Task OnParametersSetAsync()
    {
        // Solo actualizamos los parámetros si se cambia el estado de la simulación.
        if (this.DeberiaEstarSimulandoInterno != this.DeberiaEstarSimulando)
        {
            this.NumerosJugadosInterno = this.NumerosJugados;
            this.LoteriasPorSegundoInterno = this.LoteriasPorSegundo;
            this.DeberiaEstarSimulandoInterno = this.DeberiaEstarSimulando;        
            if (NumerosJugadosInterno.Any() && LoteriasPorSegundoInterno.HasValue && DeberiaEstarSimulandoInterno)
            {
                this.IniciarSimulacion();
            }
            else if (!DeberiaEstarSimulandoInterno && HaSimulado)
            {
                this.DetenerSimulacion();
            }
        }

        if(this.DeberiaReiniciarSimulacion)
        {
            await this.Reiniciar();
        }        
    }

    private  async Task Reiniciar()
    {
        this.FraccionesJugadas = 0;
        this.LoteriasJugadas = 0;
        this.DineroGastado = 0;
        this.DineroGanado = 0;
        this.BalanceTotal = 0;

        foreach (var kvp in PremiosGanados)
        {
            PremiosGanados[kvp.Key] = 0;
        }

        this.DeberiaReiniciarSimulacion = false;
        await DeberiaReiniciarSimulacionChanged.InvokeAsync(this.DeberiaReiniciarSimulacion);
    }

    private double ConseguirTiempoDeEspera()
    {
        //Asume que los parametros no son nulos.
        return (double)1000 / (double)this.LoteriasPorSegundoInterno!.Value;
    }

    private void IniciarSimulacion()
    {
        this.HaSimulado = true;
        Timer = new System.Timers.Timer(ConseguirTiempoDeEspera());
        Timer.Elapsed += OnTimerElapsed;
        Timer.AutoReset = true;
        Timer.Start();
    }


    private void DetenerSimulacion()
    {
        Timer.Stop();
    }

    private void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
        this.GenerarNumerosGanadoresYTiqueteComprado();
        this.EvaluarLoteria();
        StateHasChanged();
    }

    private void GenerarNumerosGanadoresYTiqueteComprado()
    {
        var NumerosMayoresGanadores = this.GenerarListaConNumerosNoRepetidos(4, 1, 99);
        var SeriesGanadoras = this.GenerarListaConNumerosNoRepetidos(3, 1, 999);
        PrimerPremio.SetNumeros(NumerosMayoresGanadores[0], SeriesGanadoras[0]);
        SegundoPremio.SetNumeros(NumerosMayoresGanadores[1], SeriesGanadoras[1]);
        TercerPremio.SetNumeros(NumerosMayoresGanadores[2], SeriesGanadoras[2]);
        NumeroDuplicador = NumerosMayoresGanadores[3];
        this.GenerarNumerosJugados();        
    }

    private List<int> GenerarListaConNumerosNoRepetidos(int count, int min, int max)
    {
        HashSet<int> set = new HashSet<int>();
        List<int> result = new List<int>();

        while (set.Count < count)
        {
            int num = this.GeneradorDeNumerosAleatorios.Next(min, max + 1);
            if (!set.Contains(num))
            {
                set.Add(num);
                result.Add(num);
            }
        }

        return result;
    }

    private void GenerarNumerosJugados()
    {
        LoteriaJugada = new List<TiqueteDeLoteria>();
        foreach (LoteriaComprada loteria in NumerosJugadosInterno)
        {
            for (int i = 0; i < loteria.NumeroDeFracciones; i++)
            {
                TiqueteDeLoteria tiqueteAIngresar = new TiqueteDeLoteria();
                int Serie = this.GeneradorDeNumerosAleatorios.Next(1, 999 + 1);
                tiqueteAIngresar.SetNumeros(loteria.NumeroMayor, Serie);
                LoteriaJugada.Add(tiqueteAIngresar);
            }
        }
    }

    private void EvaluarLoteria()
    {        
        this.LoteriasJugadas++;
        int dineroGanadoEnRonda = 0;
        foreach (TiqueteDeLoteria tiqueteDeLoteriaJugado in LoteriaJugada)
        {
            this.FraccionesJugadas++;
            if(usarSimulacionLiteral)
            {
                dineroGanadoEnRonda += this.EvaluarTiqueteDeLoteria(tiqueteDeLoteriaJugado);
            }
            else
            {
                dineroGanadoEnRonda += this.EvaluarTiqueteDeLoteria();
            }
        }
        this.DineroGanado += dineroGanadoEnRonda;
        this.DineroGastado = FraccionesJugadas * PrecioPorFraccion;
        this.BalanceTotal = DineroGanado - DineroGastado;
    }

    private int EvaluarTiqueteDeLoteria(TiqueteDeLoteria tiqueteDeLoteriaJugado)
    {
        int dineroGanado = 0;
        dineroGanado += EvaluarPremioMayor(tiqueteDeLoteriaJugado);
        dineroGanado += EvaluarSegundoPremio(tiqueteDeLoteriaJugado);
        dineroGanado += EvaluarTercerPremio(tiqueteDeLoteriaJugado);
        dineroGanado += EvaluarIgualAMayorDiferenteSerie(tiqueteDeLoteriaJugado);
        dineroGanado += EvaluarIgualASegundoDiferenteSerie(tiqueteDeLoteriaJugado);
        dineroGanado += EvaluarIgualATerceroDiferenteSerie(tiqueteDeLoteriaJugado);
        int numeroRevertido = RevertirNumero(tiqueteDeLoteriaJugado.Mayor);
        dineroGanado += EvaluarInversaAlMayor(numeroRevertido);
        dineroGanado += EvaluarInversaAlSegundo(numeroRevertido);
        dineroGanado += EvaluaraInversaAlTercero(numeroRevertido);
        dineroGanado += EvaluarNumeroDuplicador(tiqueteDeLoteriaJugado.Mayor);
        return dineroGanado;
    }

    private int EvaluarTiqueteDeLoteria()
    {
        int premioTotal = 0;
        foreach (var kvp in PremiosLoteria.InformacionPremios)
        {
            // Se genera un número entre 0 y la probabilidad del premio.
            // Por ejemplo, 0 a 98000 para el premio mayor.
            // Si el número es 0, se gana el premio.
            int numero = this.GeneradorDeNumerosAleatorios.Next(kvp.Value.probabilidad);
            if (numero == 0)
            {
                PremiosGanados[kvp.Key]++;
                premioTotal += kvp.Value.premio;                
            }
        }
        return premioTotal;
    }

    private int EvaluarPremioMayor(TiqueteDeLoteria tiqueteDeLoteriaJugado)
    {
        if (tiqueteDeLoteriaJugado.Mayor == PrimerPremio.Mayor && tiqueteDeLoteriaJugado.Serie == PrimerPremio.Serie)
        {
            PremiosGanados[PremiosLoteria.TipoPremio.PremioMayor]++;
            return PremiosLoteria.InformacionPremios[PremiosLoteria.TipoPremio.PremioMayor].premio;
        }
        return 0;
    }

    private int EvaluarSegundoPremio(TiqueteDeLoteria tiqueteDeLoteriaJugado)
    {
        if (tiqueteDeLoteriaJugado.Mayor == SegundoPremio.Mayor && tiqueteDeLoteriaJugado.Serie == SegundoPremio.Serie)
        {
            PremiosGanados[PremiosLoteria.TipoPremio.SegundoPremio]++;
            return PremiosLoteria.InformacionPremios[PremiosLoteria.TipoPremio.SegundoPremio].premio;
        }
        return 0;
    }

    private int EvaluarTercerPremio(TiqueteDeLoteria tiqueteDeLoteriaJugado)
    {
        if (tiqueteDeLoteriaJugado.Mayor == TercerPremio.Mayor && tiqueteDeLoteriaJugado.Serie == TercerPremio.Serie)
        {
            PremiosGanados[PremiosLoteria.TipoPremio.TercerPremio]++;
            return PremiosLoteria.InformacionPremios[PremiosLoteria.TipoPremio.TercerPremio].premio;
        }
        return 0;
    }

    private int EvaluarIgualAMayorDiferenteSerie(TiqueteDeLoteria tiqueteDeLoteriaJugado)
    {
        if(tiqueteDeLoteriaJugado.Mayor == PrimerPremio.Mayor && tiqueteDeLoteriaJugado.Serie != PrimerPremio.Serie)
        {
            PremiosGanados[PremiosLoteria.TipoPremio.IgualAMayorDiferenteSerie]++;
            return PremiosLoteria.InformacionPremios[PremiosLoteria.TipoPremio.IgualAMayorDiferenteSerie].premio;
        }
        return 0;
    }

    private int EvaluarIgualASegundoDiferenteSerie(TiqueteDeLoteria tiqueteDeLoteriaJugado)
    {
        if(tiqueteDeLoteriaJugado.Mayor == SegundoPremio.Mayor && tiqueteDeLoteriaJugado.Serie != SegundoPremio.Serie)
        {
            PremiosGanados[PremiosLoteria.TipoPremio.IgualASegundoDiferenteSerie]++;
            return PremiosLoteria.InformacionPremios[PremiosLoteria.TipoPremio.IgualASegundoDiferenteSerie].premio;
        }
        return 0;
    }

    private int EvaluarIgualATerceroDiferenteSerie(TiqueteDeLoteria tiqueteDeLoteriaJugado)
    {
        if(tiqueteDeLoteriaJugado.Mayor == TercerPremio.Mayor && tiqueteDeLoteriaJugado.Serie != TercerPremio.Serie)
        {
            PremiosGanados[PremiosLoteria.TipoPremio.IgualATerceroDiferenteSerie]++;
            return PremiosLoteria.InformacionPremios[PremiosLoteria.TipoPremio.IgualATerceroDiferenteSerie].premio;
        }
        return 0;
    }

    private int EvaluarInversaAlMayor(int inversaJugada)
    {
        if(inversaJugada == PrimerPremio.Mayor)
        {
            PremiosGanados[PremiosLoteria.TipoPremio.InversaDelMayor]++;
            return PremiosLoteria.InformacionPremios[PremiosLoteria.TipoPremio.InversaDelMayor].premio;
        }
        return 0;
    }

    private int EvaluarInversaAlSegundo(int inversaJugada)
    {
        if(inversaJugada == SegundoPremio.Mayor)
        {
            PremiosGanados[PremiosLoteria.TipoPremio.InversaDelSegundo]++;
            return PremiosLoteria.InformacionPremios[PremiosLoteria.TipoPremio.InversaDelSegundo].premio;
        }
        return 0;
    }

    private int EvaluaraInversaAlTercero(int inversaJugada)
    {
        if(inversaJugada == TercerPremio.Mayor)
        {
            PremiosGanados[PremiosLoteria.TipoPremio.InversaDelTercero]++;
            return PremiosLoteria.InformacionPremios[PremiosLoteria.TipoPremio.InversaDelTercero].premio;
        }
        return 0;
    }

    private int EvaluarNumeroDuplicador(int mayorJugado)
    {
        if (NumeroDuplicador == mayorJugado)
        {
            PremiosGanados[PremiosLoteria.TipoPremio.NumeroDuplicador]++;
            return PremiosLoteria.InformacionPremios[PremiosLoteria.TipoPremio.NumeroDuplicador].premio;
        }
        return 0;
    }

    private int RevertirNumero(int numero)
    {
        // Extraer los dígitos
        int ultimoDigito = numero % 10;
        int primerDigito = numero / 10;

        // Revertir los dígitos y formar el número revertido
        int numeroRevertido = ultimoDigito * 10 + primerDigito;

        // Devolver el número revertido
        return numeroRevertido;
    }

    public class TiqueteDeLoteria
    {

        public int Mayor { get; set; }

        public int Serie { get; set; }

        public void SetNumeros(int mayor, int serie)
        {
            Mayor = mayor;
            Serie = serie;
        }
    }
}

