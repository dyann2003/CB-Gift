namespace CB_Gift.DTOs
{
    public class ServiceResult<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public static ServiceResult<T> Ok(T data, string? message = null)
    => new ServiceResult<T> { Success = true, Message = message, Data = data };

        public static ServiceResult<T> Fail(string message)
            => new ServiceResult<T> { Success = false, Message = message };
    }
}
