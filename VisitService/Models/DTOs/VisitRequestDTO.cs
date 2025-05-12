namespace VisitService.DTOs;

public class VisitRequestDTO
{
    public int VisitId { get; set; }
    public int ClientId { get; set; }
    public string MechanicLicenceNumber { get; set; }
    public List<ServiceDTO> Services { get; set; }
}