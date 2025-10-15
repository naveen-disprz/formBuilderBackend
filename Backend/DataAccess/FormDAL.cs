using Backend.Models.Nosql;
using Backend.Data;
using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Backend.Exceptions;

namespace Backend.DataAccess
{
    public class FormDAL : IFormDAL
    {
        private readonly IMongoCollection<Form> _forms;
        private readonly ILogger<FormDAL> _logger;

        public FormDAL(MongoDbContext mongoContext, ILogger<FormDAL> logger)
        {
            _forms = mongoContext.Forms;
            _logger = logger;
        }

        public async Task<Form> CreateFormAsync(Form form)
        {
            try
            {
                form.Id = ObjectId.GenerateNewId().ToString();
                form.CreatedAt = DateTime.UtcNow;
                form.UpdatedAt = DateTime.UtcNow;
                form.IsPublished = false;
                form.IsDeleted = false;

                await _forms.InsertOneAsync(form);
                _logger.LogInformation($"Form created successfully: {form.Id}");

                return form;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating form");
                throw new FormDataAccessException("Database error while creating form", ex);
            }
        }

        public async Task<Form?> GetFormByIdAsync(string formId)
        {
            try
            {
                var filter = Builders<Form>.Filter.And(
                    Builders<Form>.Filter.Eq(f => f.Id, formId),
                    Builders<Form>.Filter.Eq(f => f.IsDeleted, false)
                );

                return await _forms.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting form by ID: {formId}");
                throw new FormDataAccessException($"Database error while retrieving form: {formId}", ex);
            }
        }

        public async Task<List<Form>> GetAllFormsAsync(int page, int pageSize, bool? isPublished = null)
        {
            try
            {
                var filterBuilder = Builders<Form>.Filter;
                var filter = filterBuilder.Eq(f => f.IsDeleted, false);

                if (isPublished.HasValue)
                {
                    filter = filterBuilder.And(filter,
                        filterBuilder.Eq(f => f.IsPublished, isPublished.Value));
                }

                return await _forms.Find(filter)
                    .SortByDescending(f => f.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting forms");
                throw new FormDataAccessException("Database error while retrieving forms", ex);
            }
        }

        public async Task<List<Form>> GetFormsByCreatorAsync(Guid creatorId, int page, int pageSize)
        {
            try
            {
                var filter = Builders<Form>.Filter.And(
                    Builders<Form>.Filter.Eq(f => f.CreatedBy, creatorId),
                    Builders<Form>.Filter.Eq(f => f.IsDeleted, false)
                );

                return await _forms.Find(filter)
                    .SortByDescending(f => f.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting forms by creator: {creatorId}");
                throw new FormDataAccessException($"Database error while retrieving forms for creator: {creatorId}", ex);
            }
        }

        public async Task<long> GetFormCountAsync(bool? isPublished = null)
        {
            try
            {
                var filterBuilder = Builders<Form>.Filter;
                var filter = filterBuilder.Eq(f => f.IsDeleted, false);

                if (isPublished.HasValue)
                {
                    filter = filterBuilder.And(filter,
                        filterBuilder.Eq(f => f.IsPublished, isPublished.Value));
                }

                return await _forms.CountDocumentsAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting form count");
                throw new FormDataAccessException("Database error while counting forms", ex);
            }
        }

        public async Task<bool> FormExistsAsync(string formId)
        {
            try
            {
                var filter = Builders<Form>.Filter.And(
                    Builders<Form>.Filter.Eq(f => f.Id, formId),
                    Builders<Form>.Filter.Eq(f => f.IsDeleted, false)
                );

                var count = await _forms.CountDocumentsAsync(filter);
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking form existence: {formId}");
                throw new FormDataAccessException($"Database error while checking form existence: {formId}", ex);
            }
        }

        public async Task<Form?> UpdateFormAsync(string formId, Form updatedForm)
        {
            try
            {
                updatedForm.UpdatedAt = DateTime.UtcNow;

                var filter = Builders<Form>.Filter.And(
                    Builders<Form>.Filter.Eq(f => f.Id, formId),
                    Builders<Form>.Filter.Eq(f => f.IsDeleted, false)
                );

                var options = new FindOneAndReplaceOptions<Form>
                {
                    ReturnDocument = ReturnDocument.After
                };

                return await _forms.FindOneAndReplaceAsync(filter, updatedForm, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating form: {formId}");
                throw new FormDataAccessException($"Database error while updating form: {formId}", ex);
            }
        }

        public async Task<bool> PublishFormAsync(string formId, Guid publishedBy)
        {
            try
            {
                var filter = Builders<Form>.Filter.Eq(f => f.Id, formId);
                var update = Builders<Form>.Update
                    .Set(f => f.IsPublished, true)
                    .Set(f => f.PublishedBy, publishedBy)
                    .Set(f => f.UpdatedAt, DateTime.UtcNow);

                var result = await _forms.UpdateOneAsync(filter, update);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error publishing form: {formId}");
                throw new FormDataAccessException($"Database error while publishing form: {formId}", ex);
            }
        }

        public async Task<bool> UnpublishFormAsync(string formId)
        {
            try
            {
                var filter = Builders<Form>.Filter.Eq(f => f.Id, formId);
                var update = Builders<Form>.Update
                    .Set(f => f.IsPublished, false)
                    .Set(f => f.UpdatedAt, DateTime.UtcNow);

                var result = await _forms.UpdateOneAsync(filter, update);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error unpublishing form: {formId}");
                throw new FormDataAccessException($"Database error while unpublishing form: {formId}", ex);
            }
        }

        public async Task<bool> SoftDeleteFormAsync(string formId)
        {
            try
            {
                var filter = Builders<Form>.Filter.Eq(f => f.Id, formId);
                var update = Builders<Form>.Update
                    .Set(f => f.IsDeleted, true)
                    .Set(f => f.UpdatedAt, DateTime.UtcNow);

                var result = await _forms.UpdateOneAsync(filter, update);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error soft deleting form: {formId}");
                throw new FormDataAccessException($"Database error while deleting form: {formId}", ex);
            }
        }

        public async Task<bool> HardDeleteFormAsync(string formId)
        {
            try
            {
                var filter = Builders<Form>.Filter.Eq(f => f.Id, formId);
                var result = await _forms.DeleteOneAsync(filter);
                return result.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error hard deleting form: {formId}");
                throw new FormDataAccessException($"Database error while permanently deleting form: {formId}", ex);
            }
        }

        public async Task<bool> FormHasResponsesAsync(string formId)
        {
            try
            {
                // This would typically check the SQL Server Responses table
                // For now, returning false as placeholder
                return await Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking form responses: {formId}");
                throw new FormDataAccessException($"Database error while checking form responses: {formId}", ex);
            }
        }
    }
}
