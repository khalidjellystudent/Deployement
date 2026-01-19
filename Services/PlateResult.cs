using System.Collections.Generic;



namespace TicketSystem.Services;
public class PlateResult
{
    public List<ResultItem> Results { get; set; } = new();
    public double? ProcessingTime { get; set; }
}

public class ResultItem
{
    public string Plate { get; set; } = string.Empty;
    public double Score { get; set; } // 0..1
    public Region? Region { get; set; }
    public VehicleBox? Box { get; set; }
    public List<Candidate> Candidates { get; set; } = new();
}

public class Region
{
    public string Code { get; set; } = string.Empty;
}

public class VehicleBox
{
    public int Xmin { get; set; }
    public int Ymin { get; set; }
    public int Xmax { get; set; }
    public int Ymax { get; set; }
}

public class Candidate
{
    public string Plate { get; set; } = string.Empty;
    public double Score { get; set; }
}
