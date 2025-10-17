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
    Task<long> GetFormCountAsync(bool? isPublished = null);

    // Update
    Task<Form?> UpdateFormAsync(string formId, Form updatedForm);
    Task<bool> PublishFormAsync(string formId, Guid publishedBy);

    // Delete
    Task<bool> SoftDeleteFormAsync(string formId);
}