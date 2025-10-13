using Backend.Models.Nosql;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace Backend.DataAccess;

public interface IFormDAL
{
    // Create
    Task<Form> CreateFormAsync(Form form);

    // Read
    Task<Form?> GetFormByIdAsync(string formId);
    Task<List<Form>> GetAllFormsAsync(int page, int pageSize, bool? isPublished = null);
    Task<List<Form>> GetFormsByCreatorAsync(Guid creatorId, int page, int pageSize);
    Task<long> GetFormCountAsync(bool? isPublished = null);
    Task<bool> FormExistsAsync(string formId);

    // Update
    Task<Form?> UpdateFormAsync(string formId, Form updatedForm);
    Task<bool> PublishFormAsync(string formId, Guid publishedBy);
    Task<bool> UnpublishFormAsync(string formId);

    // Delete
    Task<bool> SoftDeleteFormAsync(string formId);
    Task<bool> HardDeleteFormAsync(string formId);

    // Utility
    Task<bool> FormHasResponsesAsync(string formId);
}