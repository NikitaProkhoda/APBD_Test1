using Microsoft.Data.SqlClient;
using VisitService.DTOs;

namespace VisitService.Services;

using System.Data.SqlClient;

public class VisitService : IVisitService
{
    private readonly IConfiguration _configuration;
    public VisitService(IConfiguration configuration) => _configuration = configuration;

    public async Task AddVisitAsync(VisitRequestDTO dto)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            var clientCmd = new SqlCommand("SELECT COUNT(*) FROM Client WHERE client_id = @ClientId", connection, transaction);
            clientCmd.Parameters.AddWithValue("@ClientId", dto.ClientId);
            var clientExists = (int)await clientCmd.ExecuteScalarAsync();
            if (clientExists == 0) throw new Exception("Client Not Found");
            
            var mechCmd = new SqlCommand("SELECT mechanic_id FROM Mechanic WHERE licence_number = @Licence", connection, transaction);
            mechCmd.Parameters.AddWithValue("@Licence", dto.MechanicLicenceNumber);
            var mechIdObj = await mechCmd.ExecuteScalarAsync();
            if (mechIdObj == null) throw new Exception("Mechanic Not Found");
            int mechanicId = Convert.ToInt32(mechIdObj);
            
            var visitCmd = new SqlCommand(@"INSERT INTO Visit (visit_id, client_id, mechanic_id, date)
                                           VALUES (@VisitId, @ClientId, @MechanicId, GETDATE())", connection, transaction);
            visitCmd.Parameters.AddWithValue("@VisitId", dto.VisitId);
            visitCmd.Parameters.AddWithValue("@ClientId", dto.ClientId);
            visitCmd.Parameters.AddWithValue("@MechanicId", mechanicId);
            await visitCmd.ExecuteNonQueryAsync();
            
            foreach (var service in dto.Services)
            {
                var serviceCmd = new SqlCommand("SELECT service_id FROM Service WHERE name = @Name", connection, transaction);
                serviceCmd.Parameters.AddWithValue("@Name", service.ServiceName);
                var serviceIdObj = await serviceCmd.ExecuteScalarAsync();
                if (serviceIdObj == null) throw new Exception("Service Not Found: " + service.ServiceName);
                int serviceId = Convert.ToInt32(serviceIdObj);

                var insert = new SqlCommand("INSERT INTO Visit_Service (visit_id, service_id, service_fee) VALUES (@VisitId, @ServiceId, @Fee)", connection, transaction);
                insert.Parameters.AddWithValue("@VisitId", dto.VisitId);
                insert.Parameters.AddWithValue("@ServiceId", serviceId);
                insert.Parameters.AddWithValue("@Fee", service.ServiceFee);
                await insert.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<VisitResponseDTO> GetVisitAsync(int id)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var cmd = new SqlCommand(@"
            SELECT v.date,
                   c.first_name, c.last_name, c.date_of_birth,
                   m.mechanic_id, m.licence_number,
                   s.name, vs.service_fee
            FROM Visit v
            JOIN Client c ON v.client_id = c.client_id
            JOIN Mechanic m ON v.mechanic_id = m.mechanic_id
            JOIN Visit_Service vs ON v.visit_id = vs.visit_id
            JOIN Service s ON vs.service_id = s.service_id
            WHERE v.visit_id = @VisitId", connection);

        cmd.Parameters.AddWithValue("@VisitId", id);

        var reader = await cmd.ExecuteReaderAsync();
        VisitResponseDTO? result = null;
        var services = new List<ServiceDTO>();

        while (await reader.ReadAsync())
        {
            if (result == null)
            {
                result = new VisitResponseDTO
                {
                    Date = reader.GetDateTime(0),
                    Client = new ClientDTO
                    {
                        FirstName = reader.GetString(1),
                        LastName = reader.GetString(2),
                        DateOfBirth = reader.GetDateTime(3)
                    },
                    Mechanic = new MechanicDTO
                    {
                        MechanicId = reader.GetInt32(4),
                        LicenceNumber = reader.GetString(5)
                    },
                    VisitServices = services
                };
            }

            services.Add(new ServiceDTO
            {
                ServiceName = reader.GetString(6),
                ServiceFee = reader.GetDecimal(7)
            });
        }

        if (result == null) throw new Exception("Not Found");
        return result;
    }
}