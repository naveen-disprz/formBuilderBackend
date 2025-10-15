using System;

namespace Backend.Exceptions
{
    /// <summary>
    /// Base exception for authentication-related errors
    /// </summary>
    public abstract class AuthException : Exception
    {
        public string ErrorCode { get; }
        
        protected AuthException(string message, string errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        protected AuthException(string message, string errorCode, Exception innerException) 
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// Exception thrown when user input validation fails
    /// </summary>
    public class ValidationException : AuthException
    {
        public ValidationException(string message) 
            : base(message, "VALIDATION_ERROR") { }
    }

    /// <summary>
    /// Exception thrown when user already exists
    /// </summary>
    public class UserAlreadyExistsException : AuthException
    {
        public UserAlreadyExistsException(string message) 
            : base(message, "USER_EXISTS") { }
    }

    /// <summary>
    /// Exception thrown when authentication fails
    /// </summary>
    public class AuthenticationFailedException : AuthException
    {
        public AuthenticationFailedException(string message = "Invalid credentials") 
            : base(message, "AUTH_FAILED") { }
    }

    /// <summary>
    /// Exception thrown when user is not found
    /// </summary>
    public class UserNotFoundException : AuthException
    {
        public UserNotFoundException(string message = "User not found") 
            : base(message, "USER_NOT_FOUND") { }
    }

    /// <summary>
    /// Exception thrown for configuration errors
    /// </summary>
    public class ConfigurationException : AuthException
    {
        public ConfigurationException(string message) 
            : base(message, "CONFIG_ERROR") { }
    }

    /// <summary>
    /// Exception thrown when password requirements are not met
    /// </summary>
    public class PasswordPolicyException : AuthException
    {
        public PasswordPolicyException(string message) 
            : base(message, "PASSWORD_POLICY") { }
    }

    /// <summary>
    /// Exception thrown for token-related errors
    /// </summary>
    public class TokenException : AuthException
    {
        public TokenException(string message) 
            : base(message, "TOKEN_ERROR") { }
    }
    
    /// <summary>
    /// Exception thrown when response data access fails
    /// </summary>
    public class AuthDataAccessException : AuthException
    {
        public AuthDataAccessException(string message, Exception innerException) 
            : base(message, "AUTH_DATA_ACCESS_ERROR", innerException) { }
    }
}