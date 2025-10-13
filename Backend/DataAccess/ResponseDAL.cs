using Backend.Models.Sql;
using Backend.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.DataAccess;

public class ResponseDAL : IResponseDAL
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ResponseDAL> _logger;

    public ResponseDAL(ApplicationDbContext context, ILogger<ResponseDAL> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Response> CreateResponseAsync(Response response)
    {
        try
        {
            response.ResponseId = Guid.NewGuid();
            response.SubmittedAt = DateTime.UtcNow;

            await _context.Responses.AddAsync(response);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Response created: {response.ResponseId}");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating response");
            throw;
        }
    }

    public async Task<Answer> CreateAnswerAsync(Answer answer)
    {
        try
        {
            answer.AnswerId = Guid.NewGuid();
            answer.CreatedAt = DateTime.UtcNow;

            await _context.Answers.AddAsync(answer);
            await _context.SaveChangesAsync();

            return answer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating answer");
            throw;
        }
    }

    public async Task<FileUpload> CreateFileUploadAsync(FileUpload file)
    {
        try
        {
            file.FileId = Guid.NewGuid();
            file.CreatedAt = DateTime.UtcNow;

            await _context.Files.AddAsync(file);
            await _context.SaveChangesAsync();

            return file;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating file upload");
            throw;
        }
    }

    public async Task<Response?> GetResponseByIdAsync(Guid responseId)
    {
        try
        {
            return await _context.Responses
                .Include(r => r.User)
                .Include(r => r.Answers)
                .ThenInclude(a => a.Files)
                .FirstOrDefaultAsync(r => r.ResponseId == responseId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting response: {responseId}");
            throw;
        }
    }

    public async Task<List<Response>> GetResponsesByFormIdAsync(string formId, int page, int pageSize)
    {
        try
        {
            return await _context.Responses
                .Include(r => r.User)
                .Where(r => r.FormId == formId)
                .OrderByDescending(r => r.SubmittedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting responses for form: {formId}");
            throw;
        }
    }

    public async Task<List<Response>> GetResponsesByUserIdAsync(Guid userId, int page, int pageSize)
    {
        try
        {
            return await _context.Responses
                .Include(r => r.Answers)
                .Where(r => r.SubmittedBy == userId)
                .OrderByDescending(r => r.SubmittedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting responses for user: {userId}");
            throw;
        }
    }

    public async Task<List<Answer>> GetAnswersByResponseIdAsync(Guid responseId)
    {
        try
        {
            return await _context.Answers
                .Include(a => a.Files)
                .Where(a => a.ResponseId == responseId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting answers for response: {responseId}");
            throw;
        }
    }

    public async Task<FileUpload?> GetFileByIdAsync(Guid fileId)
    {
        try
        {
            return await _context.Files
                .FirstOrDefaultAsync(f => f.FileId == fileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting file: {fileId}");
            throw;
        }
    }

    public async Task<long> GetResponseCountByFormIdAsync(string formId)
    {
        try
        {
            return await _context.Responses
                .Where(r => r.FormId == formId)
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting response count for form: {formId}");
            throw;
        }
    }

    public async Task<bool> UserHasRespondedToFormAsync(Guid userId, string formId)
    {
        try
        {
            return await _context.Responses
                .AnyAsync(r => r.SubmittedBy == userId && r.FormId == formId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking user response for form: {formId}");
            throw;
        }
    }

    public async Task<Response> UpdateResponseAsync(Response response)
    {
        try
        {
            _context.Responses.Update(response);
            await _context.SaveChangesAsync();
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating response: {response.ResponseId}");
            throw;
        }
    }

    public async Task<bool> DeleteResponseAsync(Guid responseId)
    {
        try
        {
            var response = await GetResponseByIdAsync(responseId);
            if (response == null) return false;

            _context.Responses.Remove(response);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting response: {responseId}");
            throw;
        }
    }
}