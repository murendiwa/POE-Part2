using System.Net;
using System.Text.Json;
using RaceDay.Models;

namespace RaceDay.Services;

internal record UploadResponse(string? Url);
internal record MessageBody(string? Message);

public class RaceDayApi(HttpClient http, IHttpContextAccessor accessor) : IRaceDayApi
{
    private ISession Session => accessor.HttpContext!.Session;

    // ---------- Authentication ----------
    // TODO-ADAPT: routes and field names below are placeholders. Match your Part 2 AuthController.

    public async Task<ApiResult<UserDto>> LoginAsync(LoginViewModel model)
    {
        var result = await ExecuteAsync<UserDto>(
            () => http.PostAsJsonAsync("api/auth/login", new { model.Email, model.Password }),
            res => res.Content.ReadFromJsonAsync<UserDto>(),
            clearSessionOn401: false,
            unauthorizedMessage: "Incorrect email or password.");

        if (!result.Success) return result;
        if (result.Data is null) return ApiResult<UserDto>.Fail("We couldn't log you in right now. Please try again.");

        // The handler should have stored the API cookie from the login response.
        if (string.IsNullOrEmpty(Session.GetString(ApiCookieHandler.SessionKey)))
            return ApiResult<UserDto>.Fail("We couldn't log you in right now. Please try again.");

        Session.SetString("UserId", result.Data.Id.ToString());
        Session.SetString("Role", result.Data.Role);
        Session.SetString("UserName", result.Data.FullName);
        return result;
    }

    public async Task<ApiResult<bool>> RegisterAsync(RegisterViewModel model)
    {
        return await ExecuteAsync<bool>(
            () => http.PostAsJsonAsync("api/auth/register",
                new { model.FullName, model.Email, model.Password, model.Role }),
            _ => Task.FromResult<bool>(true),
            clearSessionOn401: false);
    }

    public async Task LogoutAsync()
    {
        try { using var _ = await http.PostAsync("api/auth/logout", null); }
        catch { /* even if the API is unreachable we still end the MVC session */ }
        Session.Clear();
    }

    // ---------- Generic helpers ----------

    public Task<ApiResult<T>> GetAsync<T>(string url) =>
        ExecuteAsync<T>(() => http.GetAsync(url), res => res.Content.ReadFromJsonAsync<T>());

    public Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(string url, TRequest body) =>
        ExecuteAsync<TResponse>(() => http.PostAsJsonAsync(url, body), res => res.Content.ReadFromJsonAsync<TResponse>());

    public Task<ApiResult<bool>> PutAsync<TRequest>(string url, TRequest body) =>
        ExecuteAsync<bool>(() => http.PutAsJsonAsync(url, body), _ => Task.FromResult<bool>(true));

    public Task<ApiResult<bool>> DeleteAsync(string url) =>
        ExecuteAsync<bool>(() => http.DeleteAsync(url), _ => Task.FromResult<bool>(true));

    public async Task<ApiResult<string>> UploadImageAsync(string url, IFormFile file)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.FileName);   // field name must match the API parameter: "file"

            using var res = await http.PostAsync(url, content);
            if (!res.IsSuccessStatusCode)
                return ApiResult<string>.Fail(await ReadFriendlyMessageAsync(res) ?? FriendlyMessage(res.StatusCode));

            var body = await res.Content.ReadFromJsonAsync<UploadResponse>();
            return ApiResult<string>.Ok(body?.Url);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return ApiResult<string>.Fail(UnavailableMessage);
        }
    }

    // ---------- Internals ----------

    private const string UnavailableMessage = "The RaceDay service is unavailable right now. Please try again shortly.";

    private async Task<ApiResult<T>> ExecuteAsync<T>(
        Func<Task<HttpResponseMessage>> call,
        Func<HttpResponseMessage, Task<T?>> read,
        bool clearSessionOn401 = true,
        string? unauthorizedMessage = null)
    {
        try
        {
            using var res = await call();

            // With AllowAutoRedirect=false, a 3xx from the API means "go to the login page" = not logged in.
            var status = (int)res.StatusCode is >= 300 and < 400 ? HttpStatusCode.Unauthorized : res.StatusCode;

            if (status == HttpStatusCode.Unauthorized)
            {
                if (clearSessionOn401) Session.Clear();
                return ApiResult<T>.Fail(unauthorizedMessage ?? FriendlyMessage(status));
            }

            if (!res.IsSuccessStatusCode)
                return ApiResult<T>.Fail(await ReadFriendlyMessageAsync(res) ?? FriendlyMessage(status));

            return ApiResult<T>.Ok(await read(res));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or NotSupportedException)
        {
            return ApiResult<T>.Fail(UnavailableMessage);
        }
    }

    // If the API returns { "message": "..." } for a 400/409, show that sentence. It's written for users
    // (see Validate() in BlobStorageService). Anything else gets a generic message, never raw error text.
    private static async Task<string?> ReadFriendlyMessageAsync(HttpResponseMessage res)
    {
        if (res.StatusCode is not (HttpStatusCode.BadRequest or HttpStatusCode.Conflict)) return null;
        try
        {
            var body = await res.Content.ReadFromJsonAsync<MessageBody>();
            return string.IsNullOrWhiteSpace(body?.Message) || body.Message.Length > 200 ? null : body.Message;
        }
        catch { return null; }
    }

    private static string FriendlyMessage(HttpStatusCode status) => status switch
    {
        HttpStatusCode.BadRequest => "Some of the details you entered weren't accepted. Please check and try again.",
        HttpStatusCode.Unauthorized => "Your session has expired. Please log in again.",
        HttpStatusCode.Forbidden => "You don't have permission to do that.",
        HttpStatusCode.NotFound => "We couldn't find what you were looking for.",
        HttpStatusCode.Conflict => "That already exists. For example, this email may already be registered.",
        _ => "Something went wrong on our side. Please try again."
    };
}