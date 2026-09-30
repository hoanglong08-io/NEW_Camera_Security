using System.Net.Http.Headers;

namespace Camera_Client;

public partial class MainPage : ContentPage
{
    private string _currentLocalVideo = string.Empty;

    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnRecordClicked(object sender, EventArgs e)
    {
        if (MediaPicker.Default.IsCaptureSupported)
        {
            var video = await MediaPicker.Default.CaptureVideoAsync();
            if (video != null)
            {
                // Lưu tạm vào bộ nhớ đệm của App
                _currentLocalVideo = Path.Combine(FileSystem.CacheDirectory, video.FileName);
                using Stream sourceStream = await video.OpenReadAsync();
                using FileStream localFileStream = File.OpenWrite(_currentLocalVideo);
                await sourceStream.CopyToAsync(localFileStream);

                StatusLabel.Text = "Đã quay và lưu cục bộ.";
            }
        }
    }

    // ĐÃ SỬA: Dùng Launcher gốc của iPhone thay vì biến VideoPlayer
    private async void OnPlayClicked(object sender, EventArgs e)
    {
        if (File.Exists(_currentLocalVideo))
        {
            StatusLabel.Text = "Đang mở trình phát video...";
            await Launcher.Default.OpenAsync(new OpenFileRequest("Xem video", new ReadOnlyFile(_currentLocalVideo)));
        }
        else
        {
            StatusLabel.Text = "Chưa có video nào được quay!";
        }
    }

    private async void OnSyncClicked(object sender, EventArgs e)
    {
        if (!File.Exists(_currentLocalVideo))
        {
            StatusLabel.Text = "Không có file để gửi!";
            return;
        }

        StatusLabel.Text = "Đang đồng bộ về Laptop...";
        string url = $"http://{ServerIpEntry.Text}:5000/upload";

        using var client = new HttpClient();
        using var content = new MultipartFormDataContent();
        using var fileStream = File.OpenRead(_currentLocalVideo);

        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("video/mp4");
        content.Add(fileContent, "video", Path.GetFileName(_currentLocalVideo));

        try
        {
            var response = await client.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                StatusLabel.Text = "Đã sao lưu thành công vào File Explorer!";
                File.Delete(_currentLocalVideo); // Xóa trên iPhone sau khi gửi để giải phóng 
                _currentLocalVideo = string.Empty;
            }
        }
        catch (Exception)
        {
            StatusLabel.Text = "Lỗi mạng: Không tìm thấy Laptop.";
        }
    }
}