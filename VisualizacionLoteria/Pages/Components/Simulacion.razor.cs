using Microsoft.AspNetCore.Components;
using MudBlazor;
using VisualizacionLoteria.Pages.Models;
using static MudBlazor.Colors;

namespace VisualizacionLoteria.Pages.Components;

public partial class Simulacion
{
    public enum VelocidadDeSimulacion
    {
        Lenta,
        Media,
        Rapida
    }

    [Parameter, EditorRequired]
    public List<LoteriaComprada> NumerosJugadosParam { get; set; } = null!;
    private List<LoteriaComprada> NumerosJugados { get; set; } = null!;
    private bool DebeActualizarNumerosJugados { get; set; } = false;
    private bool DeshabilitarBotonDeInicio { get; set; } = true;

    private VelocidadDeSimulacion VelocidadDeseada { get; set; } = VelocidadDeSimulacion.Media;
    private VelocidadDeSimulacion VelocidadActual { get; set; } = VelocidadDeSimulacion.Media;
    private TiqueteDeLoteria PrimerPremio { get; set; } = new TiqueteDeLoteria();
    private TiqueteDeLoteria SegundoPremio { get; set; } = new TiqueteDeLoteria();
    private TiqueteDeLoteria TercerPremio { get; set; } = new TiqueteDeLoteria();
    private List<TiqueteDeLoteria> LoteriaJugada { get; set; } = new List<TiqueteDeLoteria>();
    private bool SimulacionEnProgreso { get; set; } = false;
    private bool HaSimulado { get; set; } = false;
    private System.Timers.Timer Timer { get; set; } = new System.Timers.Timer();
    private Random GeneradorDeNumerosAleatorios { get; set; } = new Random((int)DateTime.Now.Ticks);

//resultados
    private const int PrecioPorFraccion = 1500;
    private int FraccionesJugadas { get; set; } = 0;
    private int LoteriasJugadas { get; set; } = 0;
    private int DineroGastado { get; set; } = 0;
    private int DineroGanado { get; set; } = 0;
    private int BalanceTotal { get; set; } = 0;

    protected override void OnParametersSet()
    {
        this.DeshabilitarBotonDeInicio = NumerosJugadosParam.Any() ? false : true;
        if(NumerosJugados != NumerosJugadosParam)
        {
            DebeActualizarNumerosJugados = true;
        }
    }

    private void SetearVelocidad(VelocidadDeSimulacion velocidad)
    {
        this.VelocidadDeseada = velocidad;
    }

    private bool DeshabilitarBoton(VelocidadDeSimulacion velocidad)
    {
        return this.VelocidadDeseada == velocidad;
    }

    private void IniciarSimulacion()
    {
        this.HaSimulado = true;
        this.SimulacionEnProgreso = true;
        Timer = new System.Timers.Timer(ConseguirTiempoDeEspera());
        Timer.Elapsed += OnTimerElapsed;
        Timer.AutoReset = true;
        Timer.Start();
    }

    private int ConseguirTiempoDeEspera()
    {
        return this.VelocidadDeseada switch
        {
            // 1000 ms = 1 segundo
            // 10 veces por segundo
            VelocidadDeSimulacion.Lenta => 100,
            // 40 veces por segundo
            VelocidadDeSimulacion.Media => 25,
            // 100 veces por segundo
            VelocidadDeSimulacion.Rapida => 10,
            _ => 100
        };
    }

    private void DetenerSimulacion()
    {
        Timer.Stop();
        this.SimulacionEnProgreso = false;
    }

    private void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
        if (DebeActualizarNumerosJugados)
        {
            this.NumerosJugados = this.NumerosJugadosParam;
            this.DebeActualizarNumerosJugados = false;
        }

        if (NumerosJugados != null && NumerosJugados.Any())
        {
            this.GenerarNumerosGanadoresYTiqueteComprado();
            this.EvaluarLoteria();
            StateHasChanged();
        }
        else
        {
            DetenerSimulacion();
            StateHasChanged();
            Snackbar.Add("Simulación detenida por falta de números.", Severity.Error);
        }

        if(this.VelocidadDeseada != this.VelocidadActual)
        {
            this.Timer.Stop();
            this.Timer.Interval = ConseguirTiempoDeEspera();
            this.Timer.Start();
            this.VelocidadActual = this.VelocidadDeseada;
        }
    }

    private void GenerarNumerosGanadoresYTiqueteComprado()
    {
        var NumerosMayoresGanadores = this.GenerarListaConNumerosNoRepetidos(3, 1, 99);
        var SeriesGanadoras = this.GenerarListaConNumerosNoRepetidos(3, 1, 999);
        PrimerPremio.SetNumeros(NumerosMayoresGanadores[0], SeriesGanadoras[0]);
        SegundoPremio.SetNumeros(NumerosMayoresGanadores[1], SeriesGanadoras[1]);
        TercerPremio.SetNumeros(NumerosMayoresGanadores[2], SeriesGanadoras[2]);
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
        foreach (LoteriaComprada loteria in NumerosJugados)
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
        dineroGanado += EvaluarNumeroDuplicador();
        return dineroGanado;
        
    }

    private int EvaluarPremioMayor(TiqueteDeLoteria tiqueteDeLoteriaJugado)
    {
        if (tiqueteDeLoteriaJugado.Mayor == PrimerPremio.Mayor && tiqueteDeLoteriaJugado.Serie == PrimerPremio.Serie)
        {
            return 24000000;
        }
        return 0;
    }

    private int EvaluarSegundoPremio(TiqueteDeLoteria tiqueteDeLoteriaJugado)
    {
        if (tiqueteDeLoteriaJugado.Mayor == SegundoPremio.Mayor && tiqueteDeLoteriaJugado.Serie == SegundoPremio.Serie)
        {
            return 7400000;
        }
        return 0;
    }

    private int EvaluarTercerPremio(TiqueteDeLoteria tiqueteDeLoteriaJugado)
    {
        if (tiqueteDeLoteriaJugado.Mayor == TercerPremio.Mayor && tiqueteDeLoteriaJugado.Serie == TercerPremio.Serie)
        {
            return 1800000;
        }
        return 0;
    }

    private int EvaluarIgualAMayorDiferenteSerie(TiqueteDeLoteria tiqueteDeLoteriaJugado)
    {
        if(tiqueteDeLoteriaJugado.Mayor == PrimerPremio.Mayor && tiqueteDeLoteriaJugado.Serie != PrimerPremio.Serie)
        {
            return 40000;
        }
        return 0;
    }

    private int EvaluarIgualASegundoDiferenteSerie(TiqueteDeLoteria tiqueteDeLoteriaJugado)
    {
        if(tiqueteDeLoteriaJugado.Mayor == SegundoPremio.Mayor && tiqueteDeLoteriaJugado.Serie != SegundoPremio.Serie)
        {
            return 8400;
        }
        return 0;
    }

    private int EvaluarIgualATerceroDiferenteSerie(TiqueteDeLoteria tiqueteDeLoteriaJugado)
    {
        if(tiqueteDeLoteriaJugado.Mayor == TercerPremio.Mayor && tiqueteDeLoteriaJugado.Serie != TercerPremio.Serie)
        {
            return 6000;
        }
        return 0;
    }

    private int EvaluarInversaAlMayor(int inversaJugada)
    {
        if(inversaJugada == PrimerPremio.Mayor)
        {
            return 3000;
        }
        return 0;
    }

    private int EvaluarInversaAlSegundo(int inversaJugada)
    {
        if(inversaJugada == SegundoPremio.Mayor)
        {
            return 2400;
        }
        return 0;
    }

    private int EvaluaraInversaAlTercero(int inversaJugada)
    {
        if(inversaJugada == TercerPremio.Mayor)
        {
            return 1500;
        }
        return 0;
    }

    //TODO: implementar numero duplicador y evitar repetidos en numeros sorteados.
    private int EvaluarNumeroDuplicador()
    {
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

