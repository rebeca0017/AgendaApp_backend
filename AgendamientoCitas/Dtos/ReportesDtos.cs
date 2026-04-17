namespace AgendamientoCitas.Dtos;

public class ResumenFinancieroDTO
{
    public decimal TotalIngresos { get; set; }
    public decimal TotalGastos { get; set; }
    public decimal GananciaNeta => TotalIngresos - TotalGastos;
    public int CitasCompletadas { get; set; }
    public int CitasCanceladas { get; set; }
    public int CitasNoAsistio { get; set; }
    public List<SerieMensualDTO> IngresosPorMes { get; set; } = new();
    public List<SerieMensualDTO> GastosPorMes { get; set; } = new();
}

public class SerieMensualDTO
{
    public string Mes { get; set; } = null!;
    public decimal Total { get; set; }
}

public class SaldoCitaDTO
{
    public int CitaId { get; set; }
    public int ClienteId { get; set; }
    public string Cliente { get; set; } = null!;
    public string Servicio { get; set; } = null!;
    public DateTime FechaInicio { get; set; }
    public string EstadoCita { get; set; } = null!;
    public decimal TotalServicio { get; set; }
    public decimal TotalAbonado { get; set; }
    public decimal SaldoPendiente => Math.Max(0, TotalServicio - TotalAbonado);
    public string EstadoPago => TotalAbonado <= 0 ? "Pendiente" : TotalAbonado < TotalServicio ? "Parcial" : "Pagado";
}

public class HistorialFinancieroClienteDTO
{
    public ClienteConsultarDTO Cliente { get; set; } = null!;
    public decimal TotalIngresos { get; set; }
    public decimal SaldoPendiente { get; set; }
    public List<IngresoConsultarDTO> Ingresos { get; set; } = new();
    public List<SaldoCitaDTO> Citas { get; set; } = new();
}
