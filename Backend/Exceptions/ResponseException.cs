using System;

namespace Backend.Exceptions
{
    /// <summary>
    /// Base exception for response-related errors
    /// </summary>
    public abstract class ResponseException : Exception
    {
        public string ErrorCode { get; }
        
        protected ResponseException(string message, string errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        protected ResponseException(string message, string errorCode, Exception innerException) 
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// Exception thrown when response validation fails
    /// </summary>
    public class ResponseValidationException : ResponseException
    {
        public ResponseValidationException(string message) 
            : base(message, "RESPONSE_VALIDATION_ERROR") { }
    }

    /// <summary>
    /// Exception thrown when response is not found
    /// </summary>
    public class ResponseNotFoundException : ResponseException
    {
        public ResponseNotFoundException(Guid responseId) 
            : base($"Response not found: {responseId}", "RESPONSE_NOT_FOUND") { }
    }

    /// <summary>
    /// Exception thrown when form is not found for response submission
    /// </summary>
    public class FormNotFoundForResponseException : ResponseException
    {
        public FormNotFoundForResponseException(string formId) 
            : base($"Form not found: {formId}", "FORM_NOT_FOUND") { }
    }

    /// <summary>
    /// Exception thrown when file is not found
    /// </summary>
    public class FileNotFoundException : ResponseException
    {
        public FileNotFoundException(Guid fileId) 
            : base($"File not found: {fileId}", "FILE_NOT_FOUND") { }
    }

    /// <summary>
    /// Exception thrown when user lacks permission for response operation
    /// </summary>
    public class ResponseUnauthorizedException : ResponseException
    {
        public ResponseUnauthorizedException(string message) 
            : base(message, "RESPONSE_UNAUTHORIZED") { }
    }

    /// <summary>
    /// Exception thrown when response operation violates business rules
    /// </summary>
    public class ResponseOperationException : ResponseException
    {
        public ResponseOperationException(string message) 
            : base(message, "RESPONSE_OPERATION_ERROR") { }
    }

    /// <summary>
    /// Exception thrown when user has already responded to a form
    /// </summary>
    public class DuplicateResponseException : ResponseException
    {
        public DuplicateResponseException(string message = "You have already responded to this form") 
            : base(message, "DUPLICATE_RESPONSE") { }
    }

    /// <summary>
    /// Exception thrown when trying to submit response to unpublished form
    /// </summary>
    public class UnpublishedFormException : ResponseException
    {
        public UnpublishedFormException(string message = "Cannot submit response to unpublished form") 
            : base(message, "UNPUBLISHED_FORM") { }
    }

    /// <summary>
    /// Exception thrown when required questions are not answered
    /// </summary>
    public class RequiredQuestionException : ResponseException
    {
        public RequiredQuestionException(string questionLabel) 
            : base($"Required question not answered: {questionLabel}", "REQUIRED_QUESTION") { }
    }

    /// <summary>
    /// Exception thrown when response data access fails
    /// </summary>
    public class ResponseDataAccessException : ResponseException
    {
        public ResponseDataAccessException(string message, Exception innerException) 
            : base(message, "RESPONSE_DATA_ACCESS_ERROR", innerException) { }
    }
}
