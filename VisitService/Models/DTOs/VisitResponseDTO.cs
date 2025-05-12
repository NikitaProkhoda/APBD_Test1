namespace VisitService.DTOs;

public class VisitResponseDTO
{
    public DateTime Date { get; set; }
    public ClientDTO Client { get; set; }
    public MechanicDTO Mechanic { get; set; }
    public List<ServiceDTO> VisitServices { get; set; }
}