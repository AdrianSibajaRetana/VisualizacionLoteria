namespace VisualizacionLoteria.Pages.Components;

public partial class Simulacion
{
    public enum VelocidadDeSimulacion
    {
        Lenta,
        Media,
        Rapida
    }

    private VelocidadDeSimulacion VelocidadDeseada { get; set; } = VelocidadDeSimulacion.Media;
    private VelocidadDeSimulacion VelocidadActual { get; set; } = VelocidadDeSimulacion.Media;
    private TiqueteDeLoteria PrimerPremio { get; set; } = new TiqueteDeLoteria();
    private TiqueteDeLoteria SegundoPremio { get; set; } = new TiqueteDeLoteria();
    private TiqueteDeLoteria TercerPremio { get; set; } = new TiqueteDeLoteria();
    private TiqueteDeLoteria TiqueteComprado { get; set; } = new TiqueteDeLoteria();
    private int NumeroMayorDeseado { get; set; } = 1;
    private bool SimulacionEnProgreso { get; set; } = false;
    private bool HaSimulado { get; set; } = false;
    private System.Timers.Timer Timer { get; set; } = new System.Timers.Timer();

    public Simulacion()
    {
        this.GenerarNumerosGanadoresYTiqueteComprado();
    }

    private void GenerarNumerosGanadoresYTiqueteComprado()
    {
        this.PrimerPremio.GenerarMayorYSerie();
        this.SegundoPremio.GenerarMayorYSerie();
        this.TercerPremio.GenerarMayorYSerie();
        this.TiqueteComprado.GenerarMayorYSerie(NumeroMayorDeseado);
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
        this.SimulacionEnProgreso = false;
        Timer.Stop();
    }

    private void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
        if(this.VelocidadDeseada != this.VelocidadActual)
        {
            this.Timer.Stop();
            this.Timer.Interval = ConseguirTiempoDeEspera();
            this.Timer.Start();
            this.VelocidadActual = this.VelocidadDeseada;
        }
        this.GenerarNumerosGanadoresYTiqueteComprado();
        StateHasChanged();
    }

    public class TiqueteDeLoteria
    {

        public int Mayor { get; set; }

        public int Serie { get; set; }

        public TiqueteDeLoteria()
        {
            this.GenerarMayorYSerie();
        }

        public void GenerarMayorYSerie(int? mayor = null)
        {
            if(mayor.HasValue)
            {
                this.Mayor = mayor.Value;
            }
            else
            {
                this.Mayor = new Random().Next(0, 99);
            }
            this.Serie = new Random().Next(0, 999);
        }
    }
}

