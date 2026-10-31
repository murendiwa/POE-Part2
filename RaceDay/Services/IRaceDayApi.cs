using RaceDay.Models;

namespace RaceDay.Services;

public interface IRaceDayApi
{
    // Authentication
    Task<ApiResult<UserDto>> LoginAsync(LoginViewModel model);
    Task<ApiResult<bool>> RegisterAsync(RegisterViewModel model);
    Task LogoutAsync();

    // Generic helpers used by every later controller (events, categories, enrolments, results...)
    Task<ApiResult<T>> GetAsync<T>(string url);
    Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(string url, TRequest body);
    Task<ApiResult<bool>> PutAsync<TRequest>(string url, TRequest body);
    Task<ApiResult<bool>> DeleteAsync(string url);

    // Sends an image to the API (which stores it in Azure Blob Storage). Returns the image URL.
    Task<ApiResult<string>> UploadImageAsync(string url, IFormFile file);
}