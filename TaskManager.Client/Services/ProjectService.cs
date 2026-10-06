using System.Net.Http.Json;
using TaskManager.Client.Models;

namespace TaskManager.Client.Services
{
    // Un "wrapper" sottile attorno a HttpClient: ogni pagina Razor che lavora
    // sui progetti chiama questi metodi invece di scrivere ogni volta
    // GetFromJsonAsync/PostAsJsonAsync — più leggibile, e se l'URL dell'API
    // cambia lo si corregge in un solo posto.
    public class ProjectService
    {
        private readonly HttpClient _http;

        public ProjectService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<ProjectModel>> GetAllAsync()
        {
            return await _http.GetFromJsonAsync<List<ProjectModel>>("api/projects") ?? new();
        }

        public async Task<bool> CreateAsync(ProjectModel project)
        {
            var response = await _http.PostAsJsonAsync("api/projects", project);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateAsync(int id, ProjectModel project)
        {
            var response = await _http.PutAsJsonAsync($"api/projects/{id}", project);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var response = await _http.DeleteAsync($"api/projects/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}
