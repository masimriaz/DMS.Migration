using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace DMS.Migration.Web.Extensions;

/// <summary>
/// Extension methods for TempData to support toast notifications.
/// </summary>
public static class TempDataExtensions
{
    private const string ToastKey = "_Toasts";

    /// <summary>
    /// Adds a toast notification to TempData.
    /// </summary>
    /// <param name="tempData">TempData dictionary</param>
    /// <param name="type">Toast type: success, error, warning, info</param>
    /// <param name="title">Toast title</param>
    /// <param name="message">Toast message</param>
    public static void AddToast(this ITempDataDictionary tempData, string type, string title, string message)
    {
        var toasts = GetToasts(tempData);
        toasts.Add(new ToastMessage
        {
            Type = type,
            Title = title,
            Message = message
        });
        tempData[ToastKey] = System.Text.Json.JsonSerializer.Serialize(toasts);
    }

    /// <summary>
    /// Gets all toast notifications from TempData.
    /// </summary>
    public static List<ToastMessage> GetToasts(this ITempDataDictionary tempData)
    {
        if (tempData[ToastKey] is string json && !string.IsNullOrEmpty(json))
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<ToastMessage>>(json)
                    ?? new List<ToastMessage>();
            }
            catch
            {
                return new List<ToastMessage>();
            }
        }
        return new List<ToastMessage>();
    }

    public class ToastMessage
    {
        public string Type { get; set; } = "info";
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
