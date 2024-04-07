using Microsoft.AspNetCore.Components;
using MudBlazor;
using VisualizacionLoteria.Pages.Models;

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

    private Dictionary<Premios, int> PremiosGanados { get; set; } = new Dictionary<Premios, int>()
    {
        {Premios.PrimerPremio, 0},
        {Premios.SegundoPremio, 0},
        {Premios.TercerPremio, 0},
        {Premios.IgualAMayorDiferenteSerie, 0},
        {Premios.IgualASegundoDiferenteSerie, 0},
        {Premios.IgualATerceroDiferenteSerie, 0},
        {Premios.InversaAlMayor, 0},
        {Premios.InversaAlSegundo, 0},
        {Premios.InversaAlTercero, 0},
        {Premios.NumeroDuplicador, 0}
    };

    private Dictionary<Premios, int> MontoPorPremio { get; set; } = new Dictionary<Premios, int>()
    {
        {Premios.PrimerPremio, 24000000},
        {Premios.SegundoPremio, 7400000},
        {Premios.TercerPremio, 1800000},
        {Premios.IgualAMayorDiferenteSerie, 40000},
        {Premios.IgualASegundoDiferenteSerie, 8400},
        {Premios.IgualATerceroDiferenteSerie, 6000},
        {Premios.InversaAlMayor, 3000},
        {Premios.InversaAlSegundo, 2400},
        {Premios.InversaAlTercero, 1500}
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

        foreach (KeyValuePair<Premios, int> premio in PremiosGanados)
        {
            PremiosGanados[premio.Key] = 0;
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
            dineroGanadoEnRonda += this.EvaluarTiqueteDeLoteria(tiqueteDeLoteriaJugado);
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
      
        return EvaluarNumeroDuplicador(tiqueteDeLoteriaJugado.Mayor) ? dineroGanado *= 2 : dineroGanado;        
    }

    private int EvaluarPremioMayor(TiqueteDeLoteria tiqueteDeLoteriaJugado)
    {
        if (tiqueteDeLoteriaJugado.Mayor == PrimerPremio.Mayor && tiqueteDeLoteriaJugado.Serie == PrimerPremio.Serie)
        {
            PremiosGanados[Premios.PrimerPremio]++;
            return MontoPorPremio[Premios.PrimerPremio];
        }
        return 0;
    }

    private int EvaluarSegundoPremio(TiqueteDeLoteria tiqueteDeLoteriaJugado)
    {
        if (tiqueteDeLoteriaJugado.Mayor == SegundoPremio.Mayor && tiqueteDeLoteriaJugado.Serie == SegundoPremio.Serie)
        {
            PremiosGanados[Premios.SegundoPremio]++;
            return MontoPorPremio[Premios.SegundoPremio];
        }
        return 0;
    }

    private int EvaluarTercerPremio(TiqueteDeLoteria tiqueteDeLoteriaJugado)
    {
        if (tiqueteDeLoteriaJugado.Mayor == TercerPremio.Mayor && tiqueteDeLoteriaJugado.Serie == TercerPremio.Serie)
        {
            PremiosGanados[Premios.TercerPremio]++;
            return MontoPorPremio[Premios.TercerPremio];
        }
        return 0;
    }

    private int EvaluarIgualAMayorDiferenteSerie(TiqueteDeLoteria tiqueteDeLoteriaJugado)
    {
        if(tiqueteDeLoteriaJugado.Mayor == PrimerPremio.Mayor && tiqueteDeLoteriaJugado.Serie != PrimerPremio.Serie)
        {
            PremiosGanados[Premios.IgualAMayorDiferenteSerie]++;
            return MontoPorPremio[Premios.IgualAMayorDiferenteSerie];
        }
        return 0;
    }

    private int EvaluarIgualASegundoDiferenteSerie(TiqueteDeLoteria tiqueteDeLoteriaJugado)
    {
        if(tiqueteDeLoteriaJugado.Mayor == SegundoPremio.Mayor && tiqueteDeLoteriaJugado.Serie != SegundoPremio.Serie)
        {
            PremiosGanados[Premios.IgualASegundoDiferenteSerie]++;
            return MontoPorPremio[Premios.IgualASegundoDiferenteSerie];
        }
        return 0;
    }

    private int EvaluarIgualATerceroDiferenteSerie(TiqueteDeLoteria tiqueteDeLoteriaJugado)
    {
        if(tiqueteDeLoteriaJugado.Mayor == TercerPremio.Mayor && tiqueteDeLoteriaJugado.Serie != TercerPremio.Serie)
        {
            PremiosGanados[Premios.IgualATerceroDiferenteSerie]++;
            return MontoPorPremio[Premios.IgualATerceroDiferenteSerie];
        }
        return 0;
    }

    private int EvaluarInversaAlMayor(int inversaJugada)
    {
        if(inversaJugada == PrimerPremio.Mayor)
        {
            PremiosGanados[Premios.InversaAlMayor]++;
            return MontoPorPremio[Premios.InversaAlMayor];
        }
        return 0;
    }

    private int EvaluarInversaAlSegundo(int inversaJugada)
    {
        if(inversaJugada == SegundoPremio.Mayor)
        {
            PremiosGanados[Premios.InversaAlSegundo]++;
            return MontoPorPremio[Premios.InversaAlSegundo];
        }
        return 0;
    }

    private int EvaluaraInversaAlTercero(int inversaJugada)
    {
        if(inversaJugada == TercerPremio.Mayor)
        {
            PremiosGanados[Premios.InversaAlTercero]++;
            return MontoPorPremio[Premios.InversaAlTercero];
        }
        return 0;
    }

    private bool EvaluarNumeroDuplicador(int mayorJugado)
    {
        if (NumeroDuplicador == mayorJugado)
        {
            PremiosGanados[Premios.NumeroDuplicador]++;
            return true;
        }
        return false;
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

