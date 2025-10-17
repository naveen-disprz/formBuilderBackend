using System;
using System.Diagnostics.CodeAnalysis;

namespace Backend.Exceptions
{
    /// <summary>
    /// Base exception for form-related errors
    /// </summary>
    [ExcludeFromCodeCoverage]
    public abstract class FormException : Exception
    {
        public string ErrorCode { get; }
        
        protected FormException(string message, string errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        protected FormException(string message, string errorCode, Exception innerException) 
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// Exception thrown when form validation fails
    /// </summary>
    public class FormValidationException : FormException
    {
        public FormValidationException(string message) 
            : base(message, "FORM_VALIDATION_ERROR") { }
    }

    /// <summary>
    /// Exception thrown when form is not found
    /// </summary>
    public class FormNotFoundException : FormException
    {
        public FormNotFoundException(string formId) 
            : base($"Form not found: {formId}", "FORM_NOT_FOUND") { }
    }

    /// <summary>
    /// Exception thrown when user lacks permission for form operation
    /// </summary>
    public class FormUnauthorizedException : FormException
    {
        public FormUnauthorizedException(string message) 
            : base(message, "FORM_UNAUTHORIZED") { }
    }

    /// <summary>
    /// Exception thrown when form operation violates business rules
    /// </summary>
    public class FormOperationException : FormException
    {
        public FormOperationException(string message) 
            : base(message, "FORM_OPERATION_ERROR") { }
    }

    /// <summary>
    /// Exception thrown when trying to modify published form with responses
    /// </summary>
    public class FormLockedException : FormException
    {
        public FormLockedException(string message = "Cannot update published form with responses") 
            : base(message, "FORM_LOCKED") { }
    }

    /// <summary>
    /// Exception thrown for question-related validation errors
    /// </summary>
    public class QuestionValidationException : FormException
    {
        public QuestionValidationException(string message) 
            : base(message, "QUESTION_VALIDATION_ERROR") { }
    }

    /// <summary>
    /// Exception thrown when form data access fails
    /// </summary>
    public class FormDataAccessException : FormException
    {
        public FormDataAccessException(string message, Exception innerException) 
            : base(message, "FORM_DATA_ACCESS_ERROR", innerException) { }
    }
}
