using VisitService.DTOs;

namespace VisitService.Services;

public interface IVisitService
{
    Task<VisitResponseDTO> GetVisitAsync(int id);
    Task AddVisitAsync(VisitRequestDTO dto);
}